using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MplotLib;

namespace MplotCli.Lsp;

/// <summary>
/// Minimal, dependency free Language Server Protocol implementation over stdin/stdout.
/// Supports full text sync, diagnostics, keyword completion, and hover.
/// </summary>
internal sealed class LspServer
{
    private const int ParseError = -32700;
    private const int MethodNotFound = -32601;
    private const int InvalidRequest = -32600;
    private const int ServerNotInitialized = -32002;

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal);

    private bool _initialized;
    private bool _shutdownRequested;

    private LspServer(Stream input, Stream output)
    {
        _input = input;
        _output = output;
    }

    public static int Run()
    {
        // stdout carries the protocol, so it must never be written to directly from here on.
        using Stream input = Console.OpenStandardInput();
        using Stream output = Console.OpenStandardOutput();
        return new LspServer(input, output).Loop();
    }

    private int Loop()
    {
        while (true)
        {
            string? body = ReadMessage();
            if (body is null) return _shutdownRequested ? 0 : 1;

            JsonObject? message;
            try
            {
                message = JsonNode.Parse(body) as JsonObject;
            }
            catch (JsonException)
            {
                SendError(null, ParseError, "Invalid JSON");
                continue;
            }

            if (message is null)
            {
                SendError(null, InvalidRequest, "Expected a JSON object");
                continue;
            }

            string? method = message["method"]?.GetValue<string>();
            JsonNode? id = message["id"];
            JsonNode? @params = message["params"];

            // A message with no method is a response to a server->client request. None are sent, so ignore.
            if (method is null) continue;

            if (method == "exit") return _shutdownRequested ? 0 : 1;

            try
            {
                if (id is null) HandleNotification(method, @params);
                else HandleRequest(id, method, @params);
            }
            catch (Exception e)
            {
                Log($"Error handling '{method}': {e}");
                if (id is not null) SendError(id, -32603, e.Message);
            }
        }
    }

    private void HandleRequest(JsonNode id, string method, JsonNode? @params)
    {
        if (!_initialized && method != "initialize")
        {
            SendError(id, ServerNotInitialized, "Server not initialized");
            return;
        }

        switch (method)
        {
            case "initialize":
                _initialized = true;
                SendResult(id, new JsonObject
                {
                    ["capabilities"] = new JsonObject
                    {
                        ["positionEncoding"] = "utf-16",
                        ["textDocumentSync"] = new JsonObject
                        {
                            ["openClose"] = true,
                            ["change"] = 1, // Full
                        },
                        ["completionProvider"] = new JsonObject
                        {
                            // Typing the quote in `y "` brings up column names.
                            ["triggerCharacters"] = new JsonArray("\""),
                        },
                        ["hoverProvider"] = true,
                    },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "mplot",
                        ["version"] = Program.Version,
                    },
                });
                break;
            case "shutdown":
                _shutdownRequested = true;
                SendResult(id, null);
                break;
            case "textDocument/completion":
                SendResult(id, Completion(@params!));
                break;
            case "textDocument/hover":
                SendResult(id, Hover(@params!));
                break;
            default:
                SendError(id, MethodNotFound, $"Method not found: {method}");
                break;
        }
    }

    private void HandleNotification(string method, JsonNode? @params)
    {
        switch (method)
        {
            case "textDocument/didOpen":
            {
                JsonNode doc = @params!["textDocument"]!;
                string uri = doc["uri"]!.GetValue<string>();
                _documents[uri] = doc["text"]!.GetValue<string>();
                PublishDiagnostics(uri);
                break;
            }
            case "textDocument/didChange":
            {
                string uri = @params!["textDocument"]!["uri"]!.GetValue<string>();
                string text = _documents.GetValueOrDefault(uri, "");
                foreach (JsonNode? change in @params["contentChanges"]!.AsArray())
                {
                    text = ApplyChange(text, change!);
                }

                _documents[uri] = text;
                PublishDiagnostics(uri);
                break;
            }
            case "textDocument/didClose":
            {
                string uri = @params!["textDocument"]!["uri"]!.GetValue<string>();
                _documents.Remove(uri);
                SendNotification("textDocument/publishDiagnostics", new JsonObject
                {
                    ["uri"] = uri,
                    ["diagnostics"] = new JsonArray(),
                });
                break;
            }
            // initialized, $/cancelRequest, $/setTrace, didSave, etc. need no action.
        }
    }

    /// <summary>
    /// Full sync is advertised, but apply ranged changes too in case a client sends them anyway.
    /// </summary>
    private static string ApplyChange(string text, JsonNode change)
    {
        string newText = change["text"]!.GetValue<string>();
        JsonNode? range = change["range"];
        if (range is null) return newText;

        int start = OffsetOf(text, ReadPosition(range["start"]!));
        int end = OffsetOf(text, ReadPosition(range["end"]!));
        return string.Concat(text.AsSpan(0, start), newText, text.AsSpan(end));
    }

    private void PublishDiagnostics(string uri)
    {
        JsonArray diagnostics = new();
        foreach (var d in MplotAnalysis.Diagnose(_documents[uri], FileInfoFromUri(uri)))
        {
            diagnostics.Add(new JsonObject
            {
                ["range"] = RangeJson(d.Range),
                ["severity"] = (int)d.Severity,
                ["source"] = "mplot",
                ["message"] = d.Message,
            });
        }

        SendNotification("textDocument/publishDiagnostics", new JsonObject
        {
            ["uri"] = uri,
            ["diagnostics"] = diagnostics,
        });
    }

    private JsonNode Completion(JsonNode @params)
    {
        string uri = @params["textDocument"]!["uri"]!.GetValue<string>();
        LspPosition position = ReadPosition(@params["position"]!);

        JsonArray items = new();
        if (_documents.TryGetValue(uri, out string? text))
        {
            var context = MplotAnalysis.Completion(text, position, FileInfoFromUri(uri));
            for (int i = 0; i < context.Columns.Count; i++)
            {
                items.Add(ColumnItem(context.Columns[i], i, context.StringContents));
            }

            if (context.StringContents is not null) return items;
        }

        foreach (var (keyword, doc) in MplotDocs.CompletionKeywords())
        {
            JsonObject item = new()
            {
                ["label"] = keyword,
                ["kind"] = (int)(doc?.Kind ?? KeywordKind.Option),
            };

            if (doc is not null)
            {
                item["detail"] = doc.Signature;
                item["documentation"] = new JsonObject
                {
                    ["kind"] = "markdown",
                    ["value"] = doc.Markdown(),
                };
            }

            items.Add(item);
        }

        return items;
    }

    private static JsonObject ColumnItem(ColumnName column, int index, LspRange? stringContents)
    {
        string escaped = column.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
        JsonObject item = new()
        {
            ["label"] = column.Name,
            ["kind"] = 5, // Field
            ["detail"] = column.Source,
            // Leading space sorts columns ahead of keywords, in header order.
            ["sortText"] = $" {index:D4}",
        };

        if (stringContents is { } range)
        {
            item["textEdit"] = new JsonObject { ["range"] = RangeJson(range), ["newText"] = escaped };
        }
        else
        {
            item["insertText"] = $"\"{escaped}\"";
            item["filterText"] = column.Name;
        }

        return item;
    }

    private JsonNode? Hover(JsonNode @params)
    {
        string uri = @params["textDocument"]!["uri"]!.GetValue<string>();
        if (!_documents.TryGetValue(uri, out string? text)) return null;

        var hover = MplotAnalysis.Hover(text, ReadPosition(@params["position"]!));
        if (hover is null) return null;

        return new JsonObject
        {
            ["contents"] = new JsonObject
            {
                ["kind"] = "markdown",
                ["value"] = hover.Value.Markdown,
            },
            ["range"] = RangeJson(hover.Value.Range),
        };
    }

    private static Maybe<FileInfo> FileInfoFromUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed) && parsed.IsFile)
        {
            return new FileInfo(parsed.LocalPath);
        }

        return NoneMarker.None;
    }

    private static LspPosition ReadPosition(JsonNode node) =>
        new(node["line"]!.GetValue<int>(), node["character"]!.GetValue<int>());

    private static JsonObject RangeJson(LspRange range) => new()
    {
        ["start"] = new JsonObject { ["line"] = range.Start.Line, ["character"] = range.Start.Character },
        ["end"] = new JsonObject { ["line"] = range.End.Line, ["character"] = range.End.Character },
    };

    private static int OffsetOf(string text, LspPosition position)
    {
        int line = 0;
        int offset = 0;
        while (line < position.Line && offset < text.Length)
        {
            int newline = text.IndexOf('\n', offset);
            if (newline < 0) return text.Length;
            offset = newline + 1;
            line++;
        }

        return Math.Min(offset + position.Character, text.Length);
    }

    // ---- Transport: JSON-RPC with Content-Length framing ----

    private string? ReadMessage()
    {
        int contentLength = -1;

        while (true)
        {
            string? header = ReadHeaderLine();
            if (header is null) return null;
            if (header.Length == 0) break;

            int colon = header.IndexOf(':');
            if (colon > 0 && header[..colon].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                contentLength = int.Parse(header[(colon + 1)..].Trim());
            }
        }

        if (contentLength < 0) return ReadMessage();

        byte[] buffer = new byte[contentLength];
        int read = 0;
        while (read < contentLength)
        {
            int n = _input.Read(buffer, read, contentLength - read);
            if (n == 0) return null;
            read += n;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>Reads one ASCII header line, without the trailing \r\n. Null at end of stream.</summary>
    private string? ReadHeaderLine()
    {
        StringBuilder b = new();
        while (true)
        {
            int c = _input.ReadByte();
            if (c < 0) return null;
            if (c == '\n')
            {
                if (b.Length > 0 && b[^1] == '\r') b.Length--;
                return b.ToString();
            }

            b.Append((char)c);
        }
    }

    private void SendResult(JsonNode id, JsonNode? result) =>
        Send(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone(), ["result"] = result });

    private void SendError(JsonNode? id, int code, string message) =>
        Send(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
        });

    private void SendNotification(string method, JsonNode @params) =>
        Send(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = @params });

    private void Send(JsonObject message)
    {
        byte[] body = Encoding.UTF8.GetBytes(message.ToJsonString());
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
        _output.Write(header);
        _output.Write(body);
        _output.Flush();
    }

    private static void Log(string message) => Console.Error.WriteLine($"[mplot lsp] {message}");
}

internal readonly record struct LspPosition(int Line, int Character);

internal readonly record struct LspRange(LspPosition Start, LspPosition End);
