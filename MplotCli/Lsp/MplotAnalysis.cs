using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MplotLib;

namespace MplotCli.Lsp;

internal enum DiagnosticSeverity
{
    Error = 1,
    Warning = 2,
}

internal sealed record LspDiagnostic(LspRange Range, DiagnosticSeverity Severity, string Message);

internal readonly record struct HoverResult(string Markdown, LspRange Range);

internal sealed record ColumnName(string Name, string Source);

/// <param name="StringContents">When the cursor is inside a string, the range of its contents up to the closing quote.</param>
internal sealed record CompletionContext(LspRange? StringContents, List<ColumnName> Columns);

/// <summary>
/// Parses mplot source and answers editor questions about it. Everything is recomputed from the
/// text on each request; plot files are small enough that caching isn't worth it.
/// </summary>
internal static class MplotAnalysis
{
    private sealed record Parsed(CommonTokenStream Tokens, MplotParser.MplotContext Tree, List<LspDiagnostic> SyntaxErrors);

    private static Parsed Parse(string text)
    {
        SyntaxErrorCollector errors = new();

        MplotLexer lexer = new(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errors);

        CommonTokenStream tokens = new(lexer);
        MplotParser parser = new(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errors);

        var tree = parser.mplot();
        tokens.Fill();
        return new Parsed(tokens, tree, errors.Diagnostics);
    }

    public static List<LspDiagnostic> Diagnose(string text, Maybe<FileInfo> fileInfo)
    {
        Parsed parsed = Parse(text);

        // The listener assumes a valid tree, so only run the semantic checks on clean parses.
        if (parsed.SyntaxErrors.Count > 0) return parsed.SyntaxErrors;

        List<LspDiagnostic> diagnostics = new();
        foreach (var plot in parsed.Tree.plot())
        {
            diagnostics.AddRange(CheckPlot(plot, fileInfo));
        }

        return diagnostics;
    }

    /// <summary>
    /// Walks a plot with the same listener the compiler uses. Does not call WriteGnuplot or GenSeries,
    /// since those can read data files or run commands.
    /// </summary>
    private static List<LspDiagnostic> CheckPlot(MplotParser.PlotContext plot, Maybe<FileInfo> fileInfo)
    {
        List<LspDiagnostic> diagnostics = new();
        TrackingListener listener = new(fileInfo);
        LspRange plotStart = TokenRange(plot.Start);

        try
        {
            new ParseTreeWalker().Walk(listener, plot);
        }
        catch (Exception e)
        {
            ParserRuleContext at = Element(listener.Current) ?? plot;

            // sargN values come from the command line, so they are unknown while editing.
            bool usesStringArg = Trees.FindAllTokenNodes(at, MplotLexer.STRING_ARG).Count > 0;
            if (!usesStringArg)
            {
                diagnostics.Add(new LspDiagnostic(ContextRange(at), DiagnosticSeverity.Error, e.Message.TrimEnd()));
            }
        }

        foreach (var warning in listener.Warnings)
        {
            diagnostics.Add(new LspDiagnostic(plotStart, DiagnosticSeverity.Warning, warning.TrimEnd()));
        }

        foreach (var check in listener.GnuplotCheck())
        {
            diagnostics.Add(new LspDiagnostic(plotStart, DiagnosticSeverity.Error, check.TrimEnd()));
        }

        return diagnostics;
    }

    private static ParserRuleContext? Element(ParserRuleContext? ctx)
    {
        for (var c = ctx; c is not null; c = c.Parent as ParserRuleContext)
        {
            if (c is MplotParser.ElementContext) return c;
        }

        return ctx;
    }

    public static HoverResult? Hover(string text, LspPosition position)
    {
        Parsed parsed = Parse(text);
        IToken? token = TokenAt(parsed.Tokens, position);
        if (token is null) return null;

        KeywordDoc? doc = DocFor(token, parsed.Tree);
        return doc is null ? null : new HoverResult(doc.Markdown(), TokenRange(token));
    }

    private static KeywordDoc? DocFor(IToken token, IParseTree tree)
    {
        if (token.Type == MplotLexer.STRING_ARG) return MplotDocs.StringArg;

        ITerminalNode? node = FindTerminal(tree, token.TokenIndex);
        if (node is null) return MplotDocs.ByKeyword(token.Text);

        if (token.Type is MplotLexer.ASTERISK or MplotLexer.CYCLE
            && !HasAncestor(node, MplotParser.RULE_rangeItem, MplotParser.RULE_expressionItem))
        {
            return MplotDocs.ArrayModifier(token.Text);
        }

        // Walk up to the nearest rule that has docs. Resolving by rule rather than by token text
        // keeps short keywords apart, e.g. 's' as series vs. 's' as a text anchor.
        for (IParseTree? t = node.Parent; t is ParserRuleContext ctx; t = ctx.Parent)
        {
            var doc = MplotDocs.ForRule(MplotParser.ruleNames[ctx.RuleIndex], ctx.Start.Text);
            if (doc is not null) return doc;
        }

        return MplotDocs.ByKeyword(token.Text);
    }

    private static readonly HashSet<string> ColumnKeywords = new(StringComparer.Ordinal) { "x", "xcol", "y", "ycol", "xexp", "yexp", "ignore" };

    private static readonly int[] ColumnRules =
    {
        MplotParser.RULE_xcol, MplotParser.RULE_ycol, MplotParser.RULE_xexp, MplotParser.RULE_yexp, MplotParser.RULE_ignore,
    };

    /// <summary>
    /// Works out what to complete at a position: whether the cursor is in a string, and if it is in a
    /// column reference (x, y, xexp, yexp, ignore), the header names of the plot's data sources.
    /// </summary>
    public static CompletionContext Completion(string text, LspPosition position, Maybe<FileInfo> fileInfo)
    {
        Parsed parsed = Parse(text);
        IList<IToken> tokens = parsed.Tokens.GetTokens();

        LspRange? stringContents = null;
        LspPosition anchor = position;

        IToken? stringToken = TokenAt(parsed.Tokens, position);
        if (stringToken is not null && stringToken.Type == MplotLexer.STRING && Compare(position, TokenRange(stringToken).Start) > 0)
        {
            LspRange range = TokenRange(stringToken);
            // A string spanning lines is usually an unclosed quote swallowing what follows, so only replace up to the cursor.
            LspPosition end = range.Start.Line == range.End.Line ? range.End with { Character = range.End.Character - 1 } : position;
            stringContents = new LspRange(range.Start with { Character = range.Start.Character + 1 }, end);
            anchor = range.Start;
        }
        else if (UnclosedQuoteBefore(text, position, tokens) is { } quote)
        {
            stringToken = null;
            stringContents = new LspRange(quote with { Character = quote.Character + 1 }, position);
            anchor = quote;
        }
        else
        {
            stringToken = null;
        }

        IToken? previous = tokens.LastOrDefault(t => t.Type != TokenConstants.EOF && Compare(TokenRange(t).End, anchor) <= 0);

        bool inColumnReference =
            (stringToken is not null && InColumnRule(parsed.Tree, stringToken))
            || (previous is not null && (ColumnKeywords.Contains(previous.Text) || InColumnRule(parsed.Tree, previous)));

        List<ColumnName> columns = inColumnReference ? PlotColumns(tokens, previous, fileInfo) : new List<ColumnName>();
        return new CompletionContext(stringContents, columns);
    }

    private static bool InColumnRule(IParseTree tree, IToken token)
    {
        ITerminalNode? node = FindTerminal(tree, token.TokenIndex);
        return node is not null && HasAncestor(node, ColumnRules);
    }

    /// <summary>
    /// The lexer can't produce a STRING token for a quote that is never closed, so look for an odd quote
    /// earlier on the line that isn't part of any STRING token.
    /// </summary>
    private static LspPosition? UnclosedQuoteBefore(string text, LspPosition position, IList<IToken> tokens)
    {
        string[] lines = text.Split('\n');
        if (position.Line >= lines.Length) return null;

        string prefix = lines[position.Line][..Math.Min(position.Character, lines[position.Line].Length)];
        int lastQuote = -1;
        int quotes = 0;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (prefix[i] == '\\') i++;
            else if (prefix[i] == '"')
            {
                quotes++;
                lastQuote = i;
            }
        }

        if (quotes % 2 == 0) return null;

        LspPosition quote = new(position.Line, lastQuote);
        bool inStringToken = tokens.Any(t => t.Type == MplotLexer.STRING
                                             && Compare(TokenRange(t).Start, quote) <= 0
                                             && Compare(quote, TokenRange(t).End) < 0);
        return inStringToken ? null : quote;
    }

    /// <summary>
    /// Collects header names from the data sources in the plot containing <paramref name="near"/>.
    /// Scans tokens rather than the parse tree, since the tree is often broken while typing.
    /// </summary>
    private static List<ColumnName> PlotColumns(IList<IToken> tokens, IToken? near, Maybe<FileInfo> fileInfo)
    {
        int index = near?.TokenIndex ?? 0;
        int start = index;
        while (start > 0 && tokens[start - 1].Type != MplotLexer.PLOTSEP) start--;
        int end = index;
        while (end < tokens.Count && tokens[end].Type is not (MplotLexer.PLOTSEP or TokenConstants.EOF)) end++;

        List<string> files = new();
        List<string> inlineData = new();
        Dictionary<string, Separator> separators = new(StringComparer.Ordinal);

        for (int i = start; i < end; i++)
        {
            IToken t = tokens[i];
            switch (t.Type)
            {
                case MplotLexer.INLINEDATA:
                    inlineData.Add(t.Text[(t.Text.IndexOf('\n') + 1)..]);
                    break;
                case MplotLexer.TAB_CHAR:
                    AddSeparator(new TabSeparator());
                    break;
                case MplotLexer.WHITESPACESEP:
                    AddSeparator(new WhiteSpaceSeparator());
                    break;
                case MplotLexer.COMMA_WORD or MplotLexer.CSV:
                    AddSeparator(new CommaSeparator());
                    break;
                default:
                    if (t.Text is "f" or "file")
                    {
                        // Includes the default in (sargN "default"); bare sargN values are unknown.
                        foreach (var s in StringsAfter(i)) files.Add(new FileSource(s, fileInfo).Filepath);
                    }
                    else if (t.Text is "sep" or "separator")
                    {
                        foreach (var s in StringsAfter(i)) AddSeparator(new CharactersSeparator(s));
                    }

                    break;
            }
        }

        // Separators are per source and order dependent. Only use an explicit one when it's unambiguous.
        Separator? separator = separators.Count == 1 ? separators.Values.First() : null;

        List<ColumnName> columns = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase); // mplot matches column names case-insensitively.
        foreach (var file in files)
        {
            foreach (var header in DataHeaders.ForFile(file, separator))
            {
                if (seen.Add(header)) columns.Add(new ColumnName(header, Path.GetFileName(file)));
            }
        }

        foreach (var data in inlineData)
        {
            foreach (var header in DataHeaders.ForInline(data, separator))
            {
                if (seen.Add(header)) columns.Add(new ColumnName(header, "inline data"));
            }
        }

        return columns;

        void AddSeparator(Separator s) => separators.TryAdd(s.ToGnuplot(), s);

        IEnumerable<string> StringsAfter(int keyword)
        {
            for (int j = keyword + 1; j < end; j++)
            {
                switch (tokens[j].Type)
                {
                    case MplotLexer.STRING:
                        yield return tokens[j].Text.UnescapeMplotString();
                        break;
                    case MplotLexer.POUND or MplotLexer.INT or MplotLexer.ASTERISK or MplotLexer.CYCLE
                        or MplotLexer.LPAREN or MplotLexer.RPAREN or MplotLexer.STRING_ARG:
                        break;
                    default:
                        yield break;
                }
            }
        }
    }

    private static IToken? TokenAt(CommonTokenStream tokens, LspPosition position)
    {
        foreach (IToken token in tokens.GetTokens())
        {
            if (token.Type == TokenConstants.EOF) break;

            LspRange range = TokenRange(token);
            if (Compare(range.Start, position) <= 0 && Compare(position, range.End) < 0) return token;
        }

        return null;
    }

    private static ITerminalNode? FindTerminal(IParseTree tree, int tokenIndex)
    {
        if (tree is ITerminalNode terminal) return terminal.Symbol.TokenIndex == tokenIndex ? terminal : null;

        for (int i = 0; i < tree.ChildCount; i++)
        {
            var found = FindTerminal(tree.GetChild(i), tokenIndex);
            if (found is not null) return found;
        }

        return null;
    }

    private static bool HasAncestor(IParseTree node, params int[] ruleIndices)
    {
        for (IParseTree? t = node.Parent; t is ParserRuleContext ctx; t = ctx.Parent)
        {
            if (ruleIndices.Contains(ctx.RuleIndex)) return true;
        }

        return false;
    }

    private static int Compare(LspPosition a, LspPosition b) =>
        a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Character.CompareTo(b.Character);

    private static LspRange ContextRange(ParserRuleContext ctx) =>
        new(TokenRange(ctx.Start).Start, TokenRange(ctx.Stop ?? ctx.Start).End);

    /// <summary>
    /// ANTLR lines are 1 based, LSP lines are 0 based. Columns are UTF-16 code units in both.
    /// </summary>
    internal static LspRange TokenRange(IToken token)
    {
        LspPosition start = new(token.Line - 1, token.Column);
        if (token.Type == TokenConstants.EOF) return new LspRange(start, start);

        string text = token.Text ?? "";
        int lastNewline = text.LastIndexOf('\n');
        LspPosition end = lastNewline < 0
            ? start with { Character = start.Character + text.Length }
            : new LspPosition(start.Line + text.Count(c => c == '\n'), text.Length - lastNewline - 1);

        return new LspRange(start, end);
    }

    private sealed class SyntaxErrorCollector : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
    {
        public List<LspDiagnostic> Diagnostics { get; } = new();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            LspRange range = offendingSymbol is null
                ? new LspRange(new(line - 1, charPositionInLine), new(line - 1, charPositionInLine + 1))
                : TokenRange(offendingSymbol);
            Diagnostics.Add(new LspDiagnostic(range, DiagnosticSeverity.Error, msg));
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            LspPosition start = new(line - 1, charPositionInLine);
            Diagnostics.Add(new LspDiagnostic(new LspRange(start, start with { Character = charPositionInLine + 1 }), DiagnosticSeverity.Error, msg));
        }
    }

    /// <summary>Tracks the rule being processed so a listener exception can be located in the source.</summary>
    private sealed class TrackingListener(Maybe<FileInfo> fileInfo) : MplotListener(fileInfo, new List<string>())
    {
        private readonly Stack<ParserRuleContext> _stack = new();

        public ParserRuleContext? Current => _stack.Count > 0 ? _stack.Peek() : null;

        public override void EnterEveryRule(ParserRuleContext context)
        {
            _stack.Push(context);
            base.EnterEveryRule(context);
        }

        public override void ExitEveryRule(ParserRuleContext context)
        {
            base.ExitEveryRule(context);
            _stack.Pop();
        }
    }
}
