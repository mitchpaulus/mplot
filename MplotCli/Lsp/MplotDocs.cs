using Antlr4.Runtime;
using System.Text;

namespace MplotCli.Lsp;

/// <summary>Values are the LSP CompletionItemKind for each kind of keyword.</summary>
internal enum KeywordKind
{
    Hidden = 0, // Documented for hover, but not offered as a completion.
    Command = 14, // Keyword
    Color = 16,
    Option = 20, // EnumMember
}

/// <param name="Rule">Grammar rule (see Mplot.g4) the keywords belong to. Used to resolve hover from the parse tree.</param>
internal sealed record KeywordDoc(string Rule, string[] Keywords, string Signature, string Description, KeywordKind Kind = KeywordKind.Command)
{
    public string Markdown()
    {
        StringBuilder b = new();
        b.Append("```mplot\n").Append(Signature).Append("\n```\n\n").Append(Description);
        if (Keywords.Length > 1)
        {
            b.Append("\n\nAliases: ").Append(string.Join(", ", Keywords.Select(k => $"`{k}`")));
        }

        return b.ToString();
    }
}

/// <summary>
/// Hover and completion docs for mplot keywords. Keep in sync with Mplot.g4 and docs/doc.md.
/// Plain single-literal tokens missing from here are still offered as completions, without docs.
/// </summary>
internal static class MplotDocs
{
    private const string ArrayNote = "Accepts `#N` counts, `*` (repeat last), and `cycle`.";

    private static readonly KeywordDoc[] Docs =
    {
        // Data source and series
        new("file", ["f", "file"], "f <file> ...", $"Data file(s) to read, one per series. Paths are relative to the .mplot file. {ArrayNote}"),
        new("inlineData", ["start", "DATA"], "start\n...\nend", "Inline data block embedded in the plot file. Starts with `start`/`DATA` and ends with `end`, each on its own line."),
        new("commandSource", ["cmd", "command"], "cmd [*] \"<program>\" \"<arg>\" ...", "Run a command and read its standard output as the data source."),
        new("separator", ["sep", "separator"], "sep \"<separator>\" ...", $"Custom field separator(s), one per data source. {ArrayNote}"),
        new("separator_type", [",", "comma", "csv", "CSV"], "comma [*]", "Data is comma separated. Add `*` to apply to all remaining sources.", KeywordKind.Option),
        new("separator_type", ["tab"], "tab [*]", "Data is tab separated. Add `*` to apply to all remaining sources.", KeywordKind.Option),
        new("separator_type", ["ws", "whitespace"], "ws [*]", "Data is whitespace separated. Add `*` to apply to all remaining sources.", KeywordKind.Option),
        new("series", ["s", "series"], "s \"<name>\" ...", $"Series names, shown in the legend. {ArrayNote}"),
        new("skip", ["skip"], "skip <lines> ...", "Number of lines to skip at the top of each data source."),
        new("xcol", ["x", "xcol"], "x <column> ...", $"X column(s), by 1-based index or header name. {ArrayNote}"),
        new("ycol", ["y", "ycol"], "y <column> ...", $"Y column(s), by 1-based index or header name. {ArrayNote}"),
        new("xexp", ["xexp"], "xexp [*] <expression>", "Compute the x value with an RPN expression, e.g. `xexp $1 60 /`. Columns are `$1`, `$2`, ..., `$x`, `$y`."),
        new("yexp", ["yexp"], "yexp [*] <expression>", "Compute the y value with an RPN expression, e.g. `yexp $2 $3 +`. Columns are `$1`, `$2`, ..., `$x`, `$y`."),
        new("colorData", ["colordata"], "colordata", "Enable coloring by data source."),

        // Labels and titles
        new("title", ["ct", "charttitle", "title", "t"], "ct \"<title>\"", "Chart title."),
        new("xlabel", ["xl", "xlabel", "xt", "xtitle"], "xl \"<label>\"", "X axis label."),
        new("x2label", ["xl2", "x2l", "xlabel2", "x2label"], "xl2 \"<label>\"", "Secondary (top) x axis label."),
        new("ylabel", ["yl", "ylabel", "yt", "ytitle"], "yl \"<label>\"", "Y axis label."),
        new("y2label", ["yl2", "y2l", "ylabel2", "y2label"], "yl2 \"<label>\"", "Secondary (right) y axis label."),
        new("labels", ["l", "labels"], "l \"<x label>\" \"<y label>\"", "Set the x and y axis labels together."),

        // Figure size, output, font
        new("width", ["w", "width"], "w <inches>", "Figure width in inches. Accepts fractions, e.g. `6-1/2`. Default 6."),
        new("height", ["h", "height"], "h <inches>", "Figure height in inches. Accepts fractions, e.g. `3-1/2`."),
        new("output", ["out", "output"], "out \"<file>\"", "Output file name."),
        new("font", ["font", "fo"], "font \"<name>\"", "Font family."),
        new("fontSize", ["fs", "fontSize"], "fs <points>", "Font size in points. Default 10."),
        new("dpi", ["dpi", "d"], "dpi <dpi>", "Output resolution in dots per inch."),
        new("svg", ["svg"], "svg", "Use the SVG terminal instead of PNG. With `-c`, writes a `.svg` file."),

        // Axis, time, range, format
        new("timeAxis", ["time"], "time", "Treat the x axis as time."),
        new("timeFormat", ["tf", "timeformat"], "tf \"<format>\"", "Format of the times in the data, e.g. `tf \"yyyy-mm-dd hh:nn\"`. A format containing `%` is passed to gnuplot unchanged. Implies `time`."),
        new("isoDate", ["isodate"], "isodate", "Shorthand for `tf \"%Y-%m-%d\"`. Implies `time`."),
        new("xformat", ["xf", "xticformat", "xformat"], "xf \"<format>\"", "Format of the x tic labels. A time format (no `%`) implies `time`."),
        new("yformat", ["yf", "yticformat", "yformat"], "yf \"<format>\"", "Format of the y tic labels."),
        new("xrange", ["xr", "xrange", "xlim", "xlimit"], "xr <low> <high>", "X axis range. Separate with `:` or `to`. Each bound is a number, date/time, `*`, or `auto`."),
        new("yrange", ["yr", "yrange", "ylim", "ylimit"], "yr <low> <high>", "Y axis range. Separate with `:` or `to`. Each bound is a number, date/time, `*`, or `auto`."),
        new("x2range", ["xr2", "xrange2", "xlim2", "xlimit2", "x2r", "x2range", "x2lim", "x2limit"], "xr2 <low> <high>", "Secondary (top) x axis range."),
        new("y2range", ["yr2", "yrange2", "ylim2", "ylimit2", "y2r", "y2range", "y2lim", "y2limit"], "yr2 <low> <high>", "Secondary (right) y axis range."),
        new("xmin", ["xmin", "xlow"], "xmin <value>", "X axis lower bound only."),
        new("xmax", ["xmax", "xhigh"], "xmax <value>", "X axis upper bound only."),
        new("ymin", ["ymin", "ylow"], "ymin <value>", "Y axis lower bound only."),
        new("ymax", ["ymax", "yhigh"], "ymax <value>", "Y axis upper bound only."),
        new("axisSide", ["y1", "y2"], "y2 [#N | * | cycle]", "Plot the series against the left (`y1`) or right (`y2`) y axis.", KeywordKind.Option),
        new("xStep", ["xstep", "xstepwidth"], "xstep <step>", "Spacing between x tics."),

        // Tics
        new("xtics", ["xtics"], "xtics <min> <step> <max>", "X tic positions. Values may be separated by `,` or `:`."),
        new("ytics", ["ytics"], "ytics <min> <step> <max>", "Y tic positions. Values may be separated by `,` or `:`."),
        new("x2tics", ["x2tics", "xtics2"], "x2tics <min> <step> <max>", "Secondary (top) x tic positions."),
        new("y2tics", ["y2tics", "ytics2"], "y2tics <min> <step> <max>", "Secondary (right) y tic positions."),
        new("xticAdd", ["xticadd", "xtickadd", "addxtic", "addxtick"], "xticadd <value> [\"<label>\"]", "Add a single x tic, optionally labeled. Label may come first."),
        new("yticAdd", ["yticadd", "ytickadd", "addytic", "addytick"], "yticadd <value> [\"<label>\"]", "Add a single y tic, optionally labeled. Label may come first."),
        new("xticAdd2", ["xticadd2", "xtickadd2", "addxtic2", "addxtick2"], "xticadd2 <value> [\"<label>\"]", "Add a single tic to the secondary x axis."),
        new("yticAdd2", ["yticadd2", "ytickadd2", "addytic2", "addytick2"], "yticadd2 <value> [\"<label>\"]", "Add a single tic to the secondary y axis."),
        new("xticlabels", ["xticlabels"], "xticlabels \"<label>\" ...", "Labels for the tics defined by `xtics`."),
        new("yticlabels", ["yticlabels"], "yticlabels \"<label>\" ...", "Labels for the tics defined by `ytics`."),
        new("x2ticlabels", ["x2ticlabels"], "x2ticlabels \"<label>\" ...", "Labels for the tics defined by `x2tics`."),
        new("y2ticlabels", ["y2ticlabels"], "y2ticlabels \"<label>\" ...", "Labels for the tics defined by `y2tics`."),
        new("ticsOut", ["ticsout", "outtics", "ticksout", "outticks"], "ticsout", "Draw tic marks pointing outward."),

        // Legend
        new("legend", ["key", "legend"], "key", "Force the legend to be shown."),
        new("noLegend", ["nokey", "nolegend"], "nokey", "Hide the legend."),
        new("legendOption", ["top", "above"], "top", "Place the legend above the plot.", KeywordKind.Option),
        new("legendOption", ["right"], "right", "Place the legend to the right of the plot.", KeywordKind.Option),
        new("legendOption", ["ur", "upperright"], "ur", "Place the legend in the upper right corner.", KeywordKind.Option),
        new("legendOption", ["ul", "upperleft"], "ul", "Place the legend in the upper left corner.", KeywordKind.Option),
        new("legendOption", ["br", "bottomright"], "br", "Place the legend in the bottom right corner.", KeywordKind.Option),
        new("legendOption", ["bl", "bottomleft"], "bl", "Place the legend in the bottom left corner.", KeywordKind.Option),
        new("legendOption", ["aboveright"], "aboveright", "Place the legend above the plot, aligned right.", KeywordKind.Option),
        new("legendCols", ["legendcols", "keycols", "cols", "numcols", "maxcols"], "legendcols <n>", "Maximum number of legend columns."),
        new("legendRows", ["legendrows", "keyrows", "rows", "numrows", "maxrows"], "legendrows <n>", "Maximum number of legend rows."),

        // Style, lines, markers, colors
        new("pointStyle", ["point", "scatter", "xy"], "point [#N | * | cycle]", "Draw the series as points only.", KeywordKind.Option),
        new("lineStyle", ["line", "lines"], "line [#N | * | cycle]", "Draw the series as lines.", KeywordKind.Option),
        new("hist", ["hist", "histogram"], "hist [#N | * | cycle]", "Draw the series as a histogram. See `binsize`.", KeywordKind.Option),
        new("fstep", ["fstep"], "fstep [#N | * | cycle]", "Draw the series as a forward step plot.", KeywordKind.Option),
        new("lineWidth", ["lw", "linewidth", "lt", "linethickness"], "lw <width> ...", $"Line width(s), one per series. {ArrayNote}"),
        new("dash", ["dashed", "dash", "nodash", "solid"], "dashed [#N | * | cycle]", "Named dash type for the next series: `dashed`/`dash` or `solid`/`nodash`.", KeywordKind.Option),
        new("dash", ["dashtype", "dt"], "dt \"<pattern>\" ...", $"Custom dash pattern(s), e.g. `dt \".-\" \"--\"`. {ArrayNote}"),
        new("marker", ["marker", "m", "o", "circle", "plus", "square", "filledcircle", "fcircle", "fillcircle", "filled", "filledsquare", "fsquare", "fillsquare", "nomarker", "nomark"],
            "[marker] <type> [#N | * | cycle]",
            "Point marker for the next series. Types: `o`/`circle`, `plus`, `square`, `filledcircle`, `filledsquare`, `nomarker`.", KeywordKind.Option),
        new("knownColor", ["red", "green", "blue", "black", "orange", "ccblue", "ccllcblue", "ccgray", "ccllcgray", "ccmaroon", "ccllcmaroon", "ccgreen", "ccllcgreen", "ccyellow", "ccllcyellow"],
            "red [* | cycle]", "Named line color for the next series.", KeywordKind.Color),
        new("hexColor", [], "#RRGGBB [* | cycle]", "Hex line color for the next series."),
        new("rgbColor", ["rgb", "RGB"], "rgb <r> <g> <b> [* | cycle]", "RGB line color for the next series. Components are 0-255."),
        new("grid", ["grid", "g"], "grid", "Show grid lines."),
        new("binsize", ["binsize", "bs", "binwidth", "barwidth", "boxwidth"], "binsize [*] <width>", "Histogram bin width."),

        // Margins and annotations
        new("lmargin", ["lmargin", "lm"], "lmargin <size>", "Left margin, in character widths."),
        new("rmargin", ["rmargin", "rm"], "rmargin <size>", "Right margin, in character widths."),
        new("textLabel", ["text", "note", "annotation"], "text \"<label>\" <x> <y> [anchor]",
            "Place a text annotation at `<x> <y>`. The string may come before or after the coordinates. Coordinates may be dates/times on a time axis. Optional anchor: `c n s e w ne nw se sw` (`l`/`r` = `w`/`e`)."),
        new("textAnchor", ["c", "n", "s", "e", "w", "ne", "nw", "se", "sw", "l", "r"], "c | n | s | e | w | ne | nw | se | sw | l | r",
            "Which point of the text sits on the coordinate. `l`/`r` are aliases for `w`/`e`. At most one per `text`.", KeywordKind.Hidden),
        new("arrow", ["arrow"], "arrow <x1> <y1> <x2> <y2>", "Draw an arrow from `(x1, y1)` to `(x2, y2)`."),
        new("xhighlight", ["xhighlight", "highlightx", "xrect", "rectx"], "xhighlight <x1> <x2> [color]",
            "Shade a full-height background band between `x1` and `x2`. Bounds may be dates/times. Color defaults to light gray."),
        new("yhighlight", ["yhighlight", "highlighty", "yrect", "recty"], "yhighlight <y1> <y2> [color]",
            "Shade a full-width background band between `y1` and `y2`. Color defaults to light gray."),

        // Other
        new("ignore", ["ignore"], "ignore [* | cycle] <expression>", "Skip points where the RPN expression is true, e.g. `ignore $2 0 <`. Operators: `+ - * / < > <= >= = != and or`."),
        new("gnuplotLiteral", ["gnuplot"], "gnuplot \"<command>\" ...", "Insert raw gnuplot commands."),
        new("count", [], "<value>#N", "Repeat the preceding value N times.", KeywordKind.Hidden),
    };

    public static readonly KeywordDoc StringArg = new("argString", [], "sargN | (sargN \"<default>\")",
        "String argument N from the command line (`mplot -s <value>`). In parentheses, gives a default used when the argument is missing.", KeywordKind.Hidden);

    private static readonly KeywordDoc Cycle = new("", ["cycle"], "<values> cycle",
        "Array modifier: wrap around, so series i uses value `i % length`. Exempts the array from the equal-length check.", KeywordKind.Option);

    private static readonly KeywordDoc Repeat = new("", ["*"], "<values> *",
        "Array modifier: repeat the last value for all remaining series. Exempts the array from the equal-length check.", KeywordKind.Hidden);

    private static readonly Dictionary<string, KeywordDoc[]> ByRule = Docs
        .GroupBy(d => d.Rule)
        .ToDictionary(g => g.Key, g => g.ToArray());

    private static readonly Lazy<List<(string Keyword, KeywordDoc? Doc)>> Completions = new(BuildCompletions);

    /// <summary>Docs for a rule. When several entries share a rule, pick the one matching the rule's first token.</summary>
    public static KeywordDoc? ForRule(string rule, string startText)
    {
        if (!ByRule.TryGetValue(rule, out var docs)) return null;
        return docs.FirstOrDefault(d => d.Keywords.Contains(startText)) ?? docs[0];
    }

    /// <summary>Fallback when the token isn't in the parse tree, e.g. after a syntax error.</summary>
    public static KeywordDoc? ByKeyword(string text) =>
        Docs.Append(Cycle).Where(d => d.Keywords.Contains(text)).OrderBy(d => d.Kind == KeywordKind.Hidden).FirstOrDefault();

    public static KeywordDoc ArrayModifier(string text) => text == "cycle" ? Cycle : Repeat;

    public static IReadOnlyList<(string Keyword, KeywordDoc? Doc)> CompletionKeywords() => Completions.Value;

    private static List<(string, KeywordDoc?)> BuildCompletions()
    {
        List<(string, KeywordDoc?)> items = new();
        // 'stdin' is a grammar token, but no element accepts it yet.
        HashSet<string> seen = new(StringComparer.Ordinal) { "stdin" };

        foreach (var doc in Docs.Append(Cycle))
        {
            foreach (var keyword in doc.Keywords)
            {
                // Hidden keywords are still marked seen so the vocabulary pass below doesn't re-add them.
                if (seen.Add(keyword) && doc.Kind != KeywordKind.Hidden) items.Add((keyword, doc));
            }
        }

        // Catch keywords added to the grammar but not documented here yet.
        var vocabulary = (Vocabulary)MplotLexer.DefaultVocabulary;
        for (int i = 1; i <= vocabulary.getMaxTokenType(); i++)
        {
            string? literal = vocabulary.GetLiteralName(i);
            if (literal is null || literal.Length < 3) continue;

            string keyword = literal[1..^1];
            if (keyword.All(char.IsLetterOrDigit) && char.IsLetter(keyword[0]) && seen.Add(keyword)) items.Add((keyword, null));
        }

        return items;
    }
}
