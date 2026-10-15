using System.Text;
using MplotLib;

namespace MplotCli.Lsp;

/// <summary>
/// Reads column names from the header line of delimited data, for completing column references.
/// </summary>
internal static class DataHeaders
{
    // Guards against reading a whole large file that has no newlines.
    private const int MaxHeaderChars = 64 * 1024;

    private static readonly Dictionary<string, (DateTime Modified, long Length, List<string> Headers)> Cache = new(StringComparer.Ordinal);

    /// <param name="separator">Explicit separator from the plot, or null to detect it the same way mplot does.</param>
    public static List<string> ForFile(string path, Separator? separator)
    {
        try
        {
            FileInfo info = new(path);
            if (!info.Exists) return new List<string>();

            string key = $"{info.FullName}\0{separator?.ToGnuplot()}";
            if (Cache.TryGetValue(key, out var cached) && cached.Modified == info.LastWriteTimeUtc && cached.Length == info.Length)
            {
                return cached.Headers;
            }

            using StreamReader reader = new(info.FullName, MplotEncoding.Utf8, detectEncodingFromByteOrderMarks: true);
            string? firstLine = ReadFirstLine(reader);

            List<string> headers = new();
            // A NUL means binary (e.g. an .xlsx), not delimited text.
            if (firstLine is not null && !firstLine.Contains('\0'))
            {
                separator ??= new AutoSeparator().Determine(new FileSource(info.FullName, NoneMarker.None));
                headers = Split(firstLine, separator);
            }

            Cache[key] = (info.LastWriteTimeUtc, info.Length, headers);
            return headers;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    /// <param name="data">Inline data contents, without the start line.</param>
    public static List<string> ForInline(string data, Separator? separator)
    {
        using StringReader reader = new(data);
        string? firstLine = ReadFirstLine(reader);
        if (firstLine is null) return new List<string>();

        separator ??= new AutoSeparator().Determine(new InlineDataSource(data, ""));
        return Split(firstLine, separator);
    }

    private static List<string> Split(string line, Separator separator)
    {
        // Quoted CSV headers are unquoted, matching `mplot --headers`.
        var fields = separator is CommaSeparator ? new CsvReader().ParseCsvLine(line) : separator.Split(line);
        return fields.Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
    }

    private static string? ReadFirstLine(TextReader reader)
    {
        StringBuilder b = new();
        int c;
        while (b.Length < MaxHeaderChars && (c = reader.Read()) >= 0)
        {
            if (c == '\n') return b.ToString().TrimEnd('\r');
            b.Append((char)c);
        }

        return b.Length == 0 ? null : b.ToString().TrimEnd('\r');
    }
}
