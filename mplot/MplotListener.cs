using System.Drawing;
using System.Text;
using Antlr4.Runtime.Tree;
using OneOf;

namespace mplot;

public class MplotListener : MplotBaseListener
{
    MplotQueue<Color> ColorStack = new(Color.Black);
    public MplotArray<Color> LineColorArray = new(Color.Black, MplotArrayMode.Repeat, "Line color");

    public MplotQueue<int> LineWidthQueue = new(1);
    public MplotArray<int> LineWidthArray = new(1, MplotArrayMode.Repeat, "Line width");

    private int _currentVar = 1;

    public string? XLabel { get; set; }
    public string? YLabel { get; set; }

    public MplotQueue<ColumnSpecifier> Ycols { get; set; } = new (new(2));

    public MplotArray<ColumnSpecifier> YcolsArray { get; } = new MplotArray<ColumnSpecifier>( new ColumnSpecifier(2), MplotArrayMode.Repeat, "Y col specifier");
    public MplotQueue<ColumnSpecifier> Xcols { get; set; } = new (new(1));

    public MplotArray<ColumnSpecifier> XcolsArray { get; } = new( new ColumnSpecifier(1), MplotArrayMode.Repeat, "X col specifier");

    public bool Grid = false;

    public List<string> GnuplotLiterals = new();
    public string? OutputFile { get; set; }

    public string? Title { get; set; }

    public int DefaultDpi = 96;
    public readonly int PlotDpi; // 96 * 4
    public int TextDpi = 96;

    public SpecifiedBool Legend = SpecifiedBool.Unset;
    public SeriesStyle PlotSeriesStyle { get; private set; } = SeriesStyle.Scatter;

    public MplotArray<SeriesStyle> PlotSeriesStyleArray = new(SeriesStyle.Scatter, MplotArrayMode.Repeat, "Series style");

    public double XMin { get; set; } = double.NaN;
    public double XMax { get; set; } = double.NaN;

    public Rational? XTicMin = null;
    public Rational? XTicMax = null;
    public Rational? XTicIncrement = null;

    public double YMin { get; set; } = double.NaN;
    public double YMax { get; set; } = double.NaN;

    public Rational? YTicMin = null;
    public Rational? YTicMax = null;
    public Rational? YTicIncrement = null;
    public Rational Width { get; set; } = new Rational(6);

    public Rational Height { get; set; } = new Rational(4);

    public int Skip = 0;

    public MplotArray<int> SkipArray = new(0, MplotArrayMode.Repeat, "Skip");

    public MplotArray<Separator2> SeparatorArray = new(new List<Separator2>() {  new AutoSeparator() } , MplotArrayMode.Repeat, "Separator");

    public MplotQueue<Marker> MarkerStack = new(new CircleMarker());

    public MplotArray<Marker> MarkerArray = new(new CircleMarker(), MplotArrayMode.Repeat, "Marker");

    public MplotQueue<string> FilePathStack = new("data.tsv");

    public MplotArray<DataSource> FilePathArray = new(new FileSource("data.tsv"), MplotArrayMode.Repeat, "File path");

    public LegendPosition LegendPosition = new Empty();

    public string? FontName { get; set; } = null;
    public int FontSize { get; set; } = 10;

    public string? LastFilePath { get; set; }

    public List<Series> Series = new();

    public MplotArray<string> SeriesNameArray = new MplotArray<string>("Series", MplotArrayMode.Repeat, "Series name");
    public bool TimeAxis { get; set; } = false;
    public string TimeFormat { get; set; } = "";

    public string XFormat { get; set; } = "";


    public List<Series> GenSeries()
    {
        List<IMplotArray> requiredArrays = new List<IMplotArray>()
        {
            XcolsArray,
            YcolsArray,
            SeriesNameArray,
            FilePathArray,
            MarkerArray,
            SkipArray,
            LineColorArray,
            LineWidthArray,
            PlotSeriesStyleArray,
            SeparatorArray,
        };

        List<IMplotArray> normalLengthArrays = requiredArrays.Where(array => array.Mode() == MplotArrayMode.Normal).ToList();

        if (!normalLengthArrays.Any())
        {
            StringBuilder b = new();
            b.Append("At least one array for the series needs to be a countable array.\n");
            throw new InvalidDataException(b.ToString());
        }

        IEnumerable<int> lengths = normalLengthArrays.Select(array => array.Length()).Distinct().ToList();

        if (lengths.Count() != 1)
        {
            StringBuilder b = new();
            b.Append("All series array lengths must be equal. Found:\n");
            foreach (var a in normalLengthArrays) b.Append($"{a.Name()}\t{a.Length()}\n");
            throw new InvalidDataException(b.ToString());
        }

        List<Series> series = new();
        for (int i = 0; i < lengths.First(); i++)
        {
            Series s = new(
            XcolsArray[i],
            YcolsArray[i],
                SeriesNameArray[i],
                FilePathArray[i],
                MarkerArray[i],
                SkipArray[i],
                LineColorArray[i],
                LineWidthArray[i],
                PlotSeriesStyleArray[i],
                SeparatorArray[i]
            );

            series.Add(s);
        }

        return series;
    }


    public MplotListener()
    {
        PlotDpi = DefaultDpi * 4;
        OutputFile = null;
    }

    public string ParseXLabelContext(MplotParser.XlabelContext context) => context.STRING().GetText().RemoveQuotes();
    public string ParseYLabelContext(MplotParser.YlabelContext context) => context.STRING().GetText().RemoveQuotes();

    public override void EnterXlabel(MplotParser.XlabelContext context) => XLabel = ParseXLabelContext(context).Trim();
    public override void EnterYlabel(MplotParser.YlabelContext context) => YLabel = ParseYLabelContext(context).Trim();

    public override void EnterTitles(MplotParser.TitlesContext context)
    {
        Title =  context.STRING()[0].GetText().RemoveQuotes().Trim();
        XLabel = context.STRING()[1].GetText().RemoveQuotes().Trim();
        YLabel = context.STRING()[2].GetText().RemoveQuotes().Trim();
    }

    public override void EnterFile(MplotParser.FileContext context)
    {
        FilePathStack.Queue(context.stringArray().STRING().Select(node => node.GetText().RemoveQuotes()));

        bool repeat = context.stringArray().ASTERISK() is not null;
        bool cycle =  context.stringArray().CYCLE() is not null;

        FilePathArray.Update(context.stringArray().STRING().Select(node => (DataSource)new FileSource(node.GetText().RemoveQuotes())).ToList(), cycle, repeat);
    }

    public override void EnterTitle(MplotParser.TitleContext context) => Title = context.STRING().GetText().RemoveQuotes();

    public override void EnterGnuplotLiteral(MplotParser.GnuplotLiteralContext context) => GnuplotLiterals.AddRange(context.STRING().Select(s => s.GetText()[1..^1]));

    public override void EnterXtics(MplotParser.XticsContext context)
    {
        XTicMin = new Rational(context.rational(0));
        XTicIncrement = new Rational(context.rational(1));
        XTicMax = new Rational(context.rational(2));
    }

    public override void EnterYtics(MplotParser.YticsContext context)
    {
        YTicMin = new Rational(context.rational(0));
        YTicIncrement = new Rational(context.rational(1));
        YTicMax = new Rational(context.rational(2));
    }

    public override void EnterHeight(MplotParser.HeightContext context)
    {
        if (context.INT() != null)
        {
            Height = new Rational(int.Parse(context.INT().GetText()));
        }
        else if (context.rational() is MplotParser.FractionContext fractionContext)
        {
            Height = new Rational(int.Parse(fractionContext.numerator.Text),
                int.Parse(fractionContext.denominator.Text));
        }
        else if (context.rational() is MplotParser.MixedFractionContext mixedFractionContext)
        {
            int denominator = int.Parse(mixedFractionContext.denominator.Text);
            int integerPortion = int.Parse(mixedFractionContext.integer.Text);
            int numerator = int.Parse(mixedFractionContext.numerator.Text);

            Height = new Rational(integerPortion * denominator, denominator) + new Rational(numerator, denominator);
        }
    }

    public override void EnterXrange(MplotParser.XrangeContext context)
    {
        if (context.range().lowLimit != null)  XMin = new Rational(context.range().lowLimit);
        if (context.range().highLimit != null) XMax = new Rational(context.range().highLimit);
    }
    public override void EnterYrange(MplotParser.YrangeContext context)
    {
        if (context.range().lowLimit != null)  YMin = new Rational(context.range().lowLimit);
        if (context.range().highLimit != null) YMax = new Rational(context.range().highLimit);
    }

    public override void EnterNoLegend(MplotParser.NoLegendContext context) => Legend = SpecifiedBool.False;


    public override void EnterWidth(MplotParser.WidthContext context)
    {
        if (context.INT() != null)
        {
            Width = new Rational(int.Parse(context.INT().GetText()));
        }
        else if (context.rational() is MplotParser.FractionContext fractionContext)
        {
            Width = new Rational(int.Parse(fractionContext.numerator.Text),
                int.Parse(fractionContext.denominator.Text));
        }
        else if (context.rational() is MplotParser.MixedFractionContext mixedFractionContext)
        {
            int denominator = int.Parse(mixedFractionContext.denominator.Text);
            int integerPortion = int.Parse(mixedFractionContext.integer.Text);
            int numerator = int.Parse(mixedFractionContext.numerator.Text);

            Width = new Rational(integerPortion * denominator, denominator) + new Rational(numerator, denominator);
        }
    }

    public override void EnterTimeFormat(MplotParser.TimeFormatContext context)
    {
        TimeFormat = context.STRING().GetText().RemoveQuotes();
        TimeAxis = true;
    }

    public override void EnterIsoDate(MplotParser.IsoDateContext context)
    {
        TimeFormat = "%Y-%m-%d";
        TimeAxis = true;
    }

    public override void EnterTimeAxis(MplotParser.TimeAxisContext context) => TimeAxis = true;

    public override void EnterCommaSep(MplotParser.CommaSepContext context)
    {
        CommaSeparator commaSeparator = new CommaSeparator();
        SeparatorArray.Add(commaSeparator);
        if (context.ASTERISK() is not null) SeparatorArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterTabSep(MplotParser.TabSepContext context)
    {
        TabSeparator tabSeparator = new();
        SeparatorArray.Add(tabSeparator);
        if (context.ASTERISK() is not null) SeparatorArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterWhitespaceSep(MplotParser.WhitespaceSepContext context)
    {
        WhiteSpaceSeparator tabSeparator = new();
        SeparatorArray.Add(tabSeparator);
        if (context.ASTERISK() is not null) SeparatorArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterSeparator(MplotParser.SeparatorContext context)
    {
        var separatorText = context.stringArray().STRING().Select(s => s.GetText().RemoveQuotes()).ToList();
        CharactersSeparator charactersSeparator = new(separatorText[0]);

        SeparatorArray.Update(separatorText.Select(s => (Separator2)new CharactersSeparator(s)).ToList(), context.stringArray().CYCLE().Any(), context.stringArray().ASTERISK().Any());
    }

    public override void EnterSeriesStyle(MplotParser.SeriesStyleContext context)
    {
        if (context.CYCLE() is not null) PlotSeriesStyleArray.SetMode(MplotArrayMode.Cycle);
        if (context.ASTERISK() is not null) PlotSeriesStyleArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterPointStyle(MplotParser.PointStyleContext context)
    {
        PlotSeriesStyle = SeriesStyle.Scatter;
        PlotSeriesStyleArray.Add(SeriesStyle.Scatter);
    }

    public override void EnterLineStyle(MplotParser.LineStyleContext context)
    {
        PlotSeriesStyle = SeriesStyle.Line;
        PlotSeriesStyleArray.Add(SeriesStyle.Line);
    }

    public override void EnterPlusMarker(MplotParser.PlusMarkerContext context)
    {
        PlusMarker plusMarker = new();
        MarkerStack.Queue(plusMarker);
        MarkerArray.Add(plusMarker);
    }

    public override void EnterCircleMarker(MplotParser.CircleMarkerContext context)
    {
        CircleMarker circleMarker = new();
        MarkerStack.Queue(circleMarker);
        MarkerArray.Add(circleMarker);
    }

    public override void EnterSquareMarker(MplotParser.SquareMarkerContext context)
    {

    }

    public override void EnterLabels(MplotParser.LabelsContext context)
    {
        XLabel = context.STRING()[0].GetText().RemoveQuotes();
        YLabel = context.STRING()[1].GetText().RemoveQuotes();
    }

    public override void EnterRgbColor(MplotParser.RgbColorContext context)
    {
        var r = int.Parse(context.INT()[0].GetText());
        var g = int.Parse(context.INT()[1].GetText());
        var b = int.Parse(context.INT()[2].GetText());

        // Get Color from RGB
        Color color = Color.FromArgb(r, g, b);
        ColorStack.Queue(color);
        LineColorArray.Add(color);
        if (context.ASTERISK() is not null)  LineColorArray.SetMode(MplotArrayMode.Repeat);
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
    }

    public override void EnterHexColor(MplotParser.HexColorContext context)
    {
        string hex = context.HEXCOLOR().GetText();

        // Get Color from Hex in form #RRGGBB
        Color color = ColorTranslator.FromHtml(hex);
        ColorStack.Queue(color);
        LineColorArray.Add(color);

        if (context.ASTERISK() is not null)  LineColorArray.SetMode(MplotArrayMode.Repeat);
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
    }

    public override void EnterYcol(MplotParser.YcolContext context)
    {
        foreach (var child in context.children)
        {
            if (child is not ITerminalNode node) continue;

            if (node.Symbol.Type == MplotParser.INT)
            {
                var integer = int.Parse(node.GetText());
                ColumnSpecifier columnSpecifier = new(integer);
                Ycols.Queue(columnSpecifier);
                YcolsArray.Add(columnSpecifier);
            }
            else if (node.Symbol.Type == MplotParser.STRING)
            {
                var text = node.GetText().RemoveQuotes();
                ColumnSpecifier columnSpecifier = new ColumnSpecifier(text);
                Ycols.Queue(columnSpecifier);
                YcolsArray.Add(columnSpecifier);
            }
            else if (node.Symbol.Type == MplotParser.ASTERISK)
            {
                YcolsArray.SetMode(MplotArrayMode.Repeat);
            }
            else if (node.Symbol.Type == MplotParser.CYCLE)
            {
                YcolsArray.SetMode(MplotArrayMode.Cycle);
            }
        }
    }

    public override void EnterXcol(MplotParser.XcolContext context)
    {
        foreach (IParseTree? child in context.children)
        {
            if (child is not ITerminalNode node) continue;

            if (node.Symbol.Type == MplotParser.INT)
            {
                var integer = int.Parse(node.GetText());
                ColumnSpecifier columnSpecifier = new(integer);
                Xcols.Queue(columnSpecifier);
                XcolsArray.Add(columnSpecifier);
            }
            else if (node.Symbol.Type == MplotParser.STRING)
            {
                var text = node.GetText().RemoveQuotes();
                ColumnSpecifier columnSpecifier = new ColumnSpecifier(text);
                Xcols.Queue(columnSpecifier);
                XcolsArray.Add(columnSpecifier);
            }
            else if (node.Symbol.Type == MplotParser.ASTERISK)
            {
                XcolsArray.SetMode(MplotArrayMode.Repeat);
            }
            else if (node.Symbol.Type == MplotParser.CYCLE)
            {
                XcolsArray.SetMode(MplotArrayMode.Cycle);
            }
        }
    }

    public override void EnterOutput(MplotParser.OutputContext context)
    {
        OutputFile = context.STRING().GetText().RemoveQuotes();
    }

    public override void EnterXformat(MplotParser.XformatContext context)
    {
        XFormat = context.STRING().GetText().RemoveQuotes();
    }

    public override void EnterSkip(MplotParser.SkipContext context)
    {
        var ints = context.intArray().INT().Select(node => int.Parse(node.GetText())).ToList();
        if (ints.Any())
        {
            Skip = ints[0];
        }
        SkipArray.Update(context.intArray());
    }

    public override void EnterLineWidth(MplotParser.LineWidthContext context)
    {
        foreach (var integer in context.intArray().INT())
        {
            var integerValue = int.Parse(integer.GetText());
            LineWidthQueue.Queue(integerValue);
        }

        LineWidthArray.Update(context.intArray());
    }

    public override void EnterKnownColorCycleAsterisk(MplotParser.KnownColorCycleAsteriskContext context)
    {
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
        if (context.ASTERISK() is not null) LineColorArray.SetMode(MplotArrayMode.Repeat);
    }

    // TODO: Add to arrays.
    public override void EnterRedColor(MplotParser.RedColorContext context)
    {
        ColorStack.Queue(Color.Red);
        LineColorArray.Add(Color.Red);
    }

    public override void EnterGreenColor(MplotParser.GreenColorContext context)
    {
        ColorStack.Queue(Color.Green);
        LineColorArray.Add(Color.Green);
    }

    public override void EnterBlueColor(MplotParser.BlueColorContext context)
    {
        ColorStack.Queue(Color.Blue);
        LineColorArray.Add(Color.Blue);
    }

    public override void EnterCcllcBlueColor(MplotParser.CcllcBlueColorContext context)
    {
        ColorStack.Queue(Color.FromArgb(0, 73, 135));
        LineColorArray.Add(Color.FromArgb(0, 73, 135));
    }

    public override void EnterCcllcGray(MplotParser.CcllcGrayContext context)
    {
        ColorStack.Queue(Color.FromArgb(84, 88, 90));
        LineColorArray.Add(Color.FromArgb(84, 88, 90));
    }

    public override void EnterCcllcMaroon(MplotParser.CcllcMaroonContext context)
    {
        ColorStack.Queue(Color.FromArgb(80, 0, 0));
        LineColorArray.Add(Color.FromArgb(80, 0, 0));
    }

    public override void EnterCcllcGreen(MplotParser.CcllcGreenContext context)
    {
        ColorStack.Queue(Color.FromArgb(25, 145, 58));
        LineColorArray.Add(Color.FromArgb(25, 145, 58));
    }

    public override void EnterCcllcYellow(MplotParser.CcllcYellowContext context)
    {
        ColorStack.Queue(Color.FromArgb(253, 196, 0));
        LineColorArray.Add(Color.FromArgb(253, 196, 0));
    }

    public override void EnterTopLegend(MplotParser.TopLegendContext context) => LegendPosition = new Top();
    public override void EnterRightLegend(MplotParser.RightLegendContext context) => LegendPosition = new Right();
    public override void EnterUpperRightLegend(MplotParser.UpperRightLegendContext context) => LegendPosition = new InsideUpperRight();
    public override void EnterUpperLeftLegend(MplotParser.UpperLeftLegendContext context) => LegendPosition = new InsideUpperLeft();

    public override void EnterGrid(MplotParser.GridContext context) => Grid = true;

    public override void EnterSeries(MplotParser.SeriesContext context)
    {
        var xCol = Xcols.Pop();
        var yCol = Ycols.Pop();

        var names = context.stringArray().STRING().Select(node => node.GetText().RemoveQuotes().Trim()).ToList();
        SeriesNameArray.Update(names, context.stringArray().CYCLE().Any(), context.stringArray().ASTERISK().Any());

        foreach (var name in names)
        {
            string filePath = FilePathStack.Pop();

            var marker = MarkerStack.Pop();
            Color lineColor = ColorStack.Pop();
            var lineWidth = LineWidthQueue.Pop();

            Series series = new Series(xCol, yCol, name, new FileSource(filePath), marker, Skip, lineColor, lineWidth, PlotSeriesStyle, new TabSeparator());

            Series.Add(series);
        }
    }

    public override void EnterInlineData(MplotParser.InlineDataContext context)
    {
        string content = context.INLINEDATA().GetText();

        int indexOfFirstNewline = content.IndexOf('\n');
        int finalIndex = content.Length - 1;

        for (int i = finalIndex; i > -1; i--)
        {
            if (content[i] != ' ' && content[i] != '\t')
            {
                finalIndex = i - 3;
                break;
            }
        }

        string data = content[(indexOfFirstNewline+1)..finalIndex];

        FilePathArray.Add(new InlineDataSource(data, "var" + _currentVar));
        _currentVar++;
    }

    public override void EnterCommandSource(MplotParser.CommandSourceContext context)
    {
        var arguments = context.STRING().Select(s => s.GetText().RemoveQuotes()).ToList();
        var cmdDataSource = new CommandDataSource(arguments, _currentVar);
        _currentVar++;
        FilePathArray.Add(cmdDataSource);
    }

    public override void EnterFont(MplotParser.FontContext context)
    {
        FontName = context.STRING().GetText().RemoveQuotes();
    }

    public override void EnterFontSize(MplotParser.FontSizeContext context)
    {
        FontSize = int.Parse(context.INT().GetText());
    }

    public string WriteGnuplot()
    {
        List<string> lines = new();

        int widthPixels  =  (Width * PlotDpi).Round();
        int heightPixels = (Height * PlotDpi).Round();

        string fontScale = PlotDpi % TextDpi == 0 ? (PlotDpi / TextDpi).ToString() : ((double)PlotDpi / TextDpi).ToString("f3");

        string fontName = FontName ?? "";

        lines.Add($"set terminal pngcairo size {widthPixels},{heightPixels} font \"{fontName},{FontSize}\" fontscale {fontScale} pointscale {fontScale}");

        lines.AddRange(GnuplotLiterals);

        // TODO: escape characters
        if (!string.IsNullOrWhiteSpace(OutputFile)) lines.Add($"set output \"{OutputFile}\"");

        if (!string.IsNullOrWhiteSpace(Title)) lines.Add($"set title \"{Title}\"");
        if (!string.IsNullOrWhiteSpace(XLabel)) lines.Add($"set xlabel \"{XLabel}\"");
        if (!string.IsNullOrWhiteSpace(YLabel)) lines.Add($"set ylabel \"{YLabel}\"");

        if (Grid) lines.Add("set grid");

        if (Legend == SpecifiedBool.False) lines.Add("set key off");
        else if (Legend == SpecifiedBool.True) lines.Add($"set key {LegendPosition.GnuPlotLegendPosition}");
        else lines.Add(Series.Count > 1 ? $"set key {LegendPosition.GnuPlotLegendPosition}" : "set key off");

        if (!double.IsNaN(XMin) && !double.IsNaN(XMax)) lines.Add($"set xrange [{XMin}:{XMax}]");
        else if (!double.IsNaN(XMin))                   lines.Add($"set xrange [{XMin}:*]");
        else if (!double.IsNaN(XMax))                   lines.Add($"set xrange [*:{XMax}]");

        if (!double.IsNaN(YMin) && !double.IsNaN(YMax)) lines.Add($"set yrange [{YMin}:{YMax}]");
        else if (!double.IsNaN(YMin))                   lines.Add($"set yrange [{YMin}:*]");
        else if (!double.IsNaN(YMax))                   lines.Add($"set yrange [*:{YMax}]");

        if (XTicMin is not null && XTicMax is not null && XTicIncrement is not null)
        {
            lines.Add( $"set xtics {XTicMin.ToDecimalString()}, {XTicIncrement.ToDecimalString()}, {XTicMax.ToDecimalString()}");
        }
        if (YTicMin is not null && YTicMax is not null && YTicIncrement is not null)
        {
            lines.Add( $"set ytics {YTicMin.ToDecimalString()}, {YTicIncrement.ToDecimalString()}, {YTicMax.ToDecimalString()}");
        }

        if (TimeAxis)
        {
            lines.Add("set xdata time");
            lines.Add($"set timefmt \"{TimeFormat}\"");
        }

        if (!string.IsNullOrWhiteSpace(XFormat))
        {
            lines.Add($"set format x \"{XFormat}\"");
        }

        List<Series> seriesList = GenSeries();

        foreach (var source in seriesList.Where(series => series.FilePath.IsInlindeDataSource).Select(series => series.FilePath.AsT1))
        {
            lines.Add($"${source.VariableName}<<EOF");
            lines.AddRange(source.Contents.SplitLines());
            lines.Add("EOF");
        }

        foreach (var source in seriesList.Where(s => s.FilePath.IsCmdDataSource).Select(s => s.FilePath.AsT2))
        {
            var cmdOutput = source.Execute();
            if (!cmdOutput.Success)
            {
                throw new InvalidOperationException(cmdOutput.ErrorMsg);
            }

            var enc = new UTF8Encoding(false);
            File.WriteAllText(source.TmpFilePath(), cmdOutput.Output, enc);
        }

        if (seriesList.Any())
        {
            // We still don't probably handle the case when series from different sources have different separators.
            var knownSep = SeparatorArray[0].Determine(seriesList[0].FilePath);
            var sepLine = knownSep.ToGnuplot();
            
            lines.Add(sepLine);

            if (seriesList.Count == 1)
            {
                Series series = seriesList[0];
                lines.Add($"plot {series.ToGnuplot()}");
            }
            else
            {
                lines.Add("plot \\");
                foreach (IndexedItem<Series> series in seriesList.WithIndex(1))
                {
                    string end = series.Index == seriesList.Count ? "" : ", \\";
                    lines.Add( "  " + series.Item.ToGnuplot() + end);
                }
            }
        }

        StringBuilder builder = new();
        foreach (var line in lines) builder.Append(line + "\n");
        return builder.ToString();
    }
}

public class AutoSeriesEnum : OneOfBase<AutoSeries, SeriesStyle>
{
    protected AutoSeriesEnum(OneOf<AutoSeries, SeriesStyle> input) : base(input)
    {
    }
}

public class AutoSeries
{


}

public class SeriesStyle
{
    public int Id { get; }
    public List<string> Keywords { get; }
    public string GnuplotStyle { get; }

    public bool IsScatter { get; }

    public static Dictionary<string, SeriesStyle> Map = new();

    public static SeriesStyle Scatter = new(0, new List<string> {"scatter", "point"}, "points", true);
    public static SeriesStyle Line = new(1, new List<string> {"line", "-"}, "lines", false);

    public SeriesStyle(int id, List<string> keywords, string gnuplotStyle, bool isScatter)
    {
        Id = id;
        Keywords = keywords;
        GnuplotStyle = gnuplotStyle;
        IsScatter = isScatter;

        foreach (var s in keywords) Map[s] = this;
    }
}

public abstract class LegendPosition
{
    public abstract string GnuPlotLegendPosition { get; }
}

public class Top : LegendPosition
{
    public override string GnuPlotLegendPosition => "outside center top";
}

public class Right : LegendPosition
{
    public override string GnuPlotLegendPosition => "outside right center";
}

public class InsideUpperRight : LegendPosition
{
    public override string GnuPlotLegendPosition => "inside right top";
}

public class InsideUpperLeft : LegendPosition
{
    public override string GnuPlotLegendPosition => "inside left top";
}

public class Empty : LegendPosition
{
    public override string GnuPlotLegendPosition => "";
}

public class IndexedItem<T>
{
    public int Index { get; }
    public T Item { get; }

    public IndexedItem(int index, T item)
    {
        Index = index;
        Item = item;
    }
}

public abstract class Marker
{
    public abstract string ToGnuplot();
}


public class CircleMarker : Marker
{
    public override string ToGnuplot() => "pt 6";
}

public class PlusMarker : Marker
{
    public override string ToGnuplot() => "pt 1";
}

public class LineMarker : Marker
{
    public override string ToGnuplot()
    {
        return "";
    }
}

public class XMarker : Marker
{
    public override string ToGnuplot()
    {
        return "";
    }
}

public class Series
{
    private readonly ColumnSpecifier _xCol;
    private readonly ColumnSpecifier _yCol;
    private readonly string _name;
    public DataSource FilePath { get; }
    private readonly Marker _marker;
    private readonly int _skip;
    private readonly Color _lineColor;
    private readonly int _lineWidth;
    private readonly SeriesStyle _style;
    public Separator2 Separator { get; }

    public Series(ColumnSpecifier xCol, ColumnSpecifier yCol, string name, DataSource filePath, Marker marker, int skip, Color lineColor, int lineWidth, SeriesStyle style, Separator2 separator)
    {
        _xCol = xCol;
        _yCol = yCol;
        _name = name;
        FilePath = filePath;
        _marker = marker;
        _skip = skip;
        _lineColor = lineColor;
        _lineWidth = lineWidth;
        _style = style;
        Separator = separator;
    }

    public string ToGnuplot()
    {
        string skipText = _skip == 0 ? "" : $" skip {_skip}";
        string lineWidth = _lineWidth == 1 ? "" : $"lw {_lineWidth}";
        string title = string.IsNullOrWhiteSpace(_name) ? "" : $" title \"{_name}\"";

        string source = FilePath.Match(
            fileSource => $"\"{fileSource.Filepath}\"",
            inlineDataSource => "$" + inlineDataSource.VariableName,
            cmdDataSource => $"\"{cmdDataSource.TmpFilePath()}\""
        );

        string markerText = _style.IsScatter ? " " + _marker.ToGnuplot() + " " : " ";

        return $"{source} using {_xCol}:{_yCol}{skipText} with {_style.GnuplotStyle}{markerText}{lineWidth} linecolor {_lineColor.ToGnuplot()}{title}";
    }
}

public class AutoSeparator
{
    public Separator Determine(DataSource source)
    {
        if (source.TryPickT0(out FileSource file, out var rest))
        {
            var lower = file.Filepath.ToLower();
            if (lower.EndsWith(".csv")) return new CommaSeparator();
            if (lower.EndsWith(".tsv")) return new TabSeparator();
        }

        using Stream stream = source.Stream();
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

        var firstLine = reader.ReadLine();
        if (firstLine is null) return new TabSeparator();
        if (firstLine.Any(c => c == '\t')) return new TabSeparator();

        var secondLine = reader.ReadLine();
        if (secondLine is null) return new TabSeparator();

        // Check if the number of commas in the first line is the same as the second.
        int firstLineCommas = firstLine.Count(c => c == ',');
        int secondLineCommas = secondLine.Count(c => c == ',');
        if (firstLineCommas == secondLineCommas) return new CommaSeparator();

        return new TabSeparator();
    }
}

public abstract class Separator
{
    public abstract string ToGnuplot();
    public abstract List<string> Split(string s);
}

public class TabSeparator : Separator 
{
    public override string ToGnuplot()
    {
        return "set datafile separator tab";
    }

    public override List<string> Split(string s)
    {
        return s.Split('\t').ToList();
    }
}

public class CommaSeparator : Separator
{
    public override string ToGnuplot() => "set datafile separator comma";
    public override List<string> Split(string s) => s.Split(',').ToList();
}

public class CharactersSeparator : Separator
{
    public string Sep { get; }

    public CharactersSeparator(string sep)
    {
        Sep = sep;
    }
    public override string ToGnuplot() => $"set datafile separator \"{Sep}\"";
    public override List<string> Split(string s) => s.Split(Sep).ToList();
}

public class WhiteSpaceSeparator : Separator
{
    public override string ToGnuplot() => $"set datafile separator whitespace";

    public override List<string> Split(string s) => s.Split(null).ToList();
}

public class Rational
{

    public int Numerator { get; }
    public int Denominator { get; }

    public Rational(MplotParser.RationalContext rationalContext)
    {
        if (rationalContext is MplotParser.FractionContext fraction)
        {
            Numerator = int.Parse(fraction.GetText());
            Denominator = int.Parse(fraction.GetText());
        }
        else if (rationalContext is MplotParser.MixedFractionContext mixedFraction)
        {
            Numerator = int.Parse(mixedFraction.denominator.Text) * int.Parse(mixedFraction.integer.Text) + int.Parse(mixedFraction.numerator.Text);
            Denominator = int.Parse(mixedFraction.denominator.Text);
        }
        else if (rationalContext is MplotParser.IntegerContext integerContext)
        {
            Numerator = int.Parse(integerContext.GetText());
            Denominator = 1;
        }
        else
        {
            throw new NotSupportedException("Unhandled rational context");
        }
    }

    public Rational(int numerator, int denominator)
    {
        // Store the rational in canonical form where denominator is positive
        // TODO: Use GCD.
        if (denominator < 0)
        {
            Numerator = -numerator;
            Denominator = -denominator;
        }
        else
        {
            Numerator = numerator;
            Denominator = denominator;
        }
    }

    public Rational(int numerator)
    {
        Numerator = numerator;
        Denominator = 1;
    }

    public override string ToString()
    {
        return $"{Numerator}/{Denominator}";
    }

    // Implement addition
    public static Rational operator +(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator + r2.Numerator * r1.Denominator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator+(Rational r1, int r2) => r1 + new Rational(r2);

    public static Rational operator+(int r1, Rational r2) => new Rational(r1) + r2;

    // Implement subtraction
    public static Rational operator -(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator - r2.Numerator * r1.Denominator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator-(Rational r1, int r2) => r1 - new Rational(r2);

    public static Rational operator-(int r1, Rational r2) => new Rational(r1) - r2;

    // Implement multiplication
    public static Rational operator *(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Numerator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator*(Rational r1, int r2) => r1 * new Rational(r2);

    public static Rational operator*(int r1, Rational r2) => new Rational(r1) * r2;

    // Implement division
    public static Rational operator /(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator;
        int denominator = r1.Denominator * r2.Numerator;
        // If denominator is zero, throw DivideByZeroException
        if (denominator == 0) throw new DivideByZeroException();
        return new Rational(numerator, denominator);
    }

    public static Rational operator/(Rational r1, int r2)
    {
        return r1 / new Rational(r2);
    }

    public static Rational operator/(int r1, Rational r2)
    {
        return new Rational(r1) / r2;
    }

    // Implement equality and inequality
    public static bool operator ==(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator == r2.Numerator * r1.Denominator;
    }

    public static bool operator !=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator != r2.Numerator * r1.Denominator;
    }

    public static bool operator ==(Rational r1, int r2)
    {
        return r1 == new Rational(r2);
    }

    public static bool operator !=(Rational r1, int r2)
    {
        return r1 != new Rational(r2);
    }

    public static bool operator ==(int r1, Rational r2)
    {
        return new Rational(r1) == r2;
    }

    public static bool operator !=(int r1, Rational r2)
    {
        return new Rational(r1) != r2;
    }

    protected bool Equals(Rational other) => Numerator * other.Denominator == other.Numerator * Denominator;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Rational) obj);
    }

    // Implement Object.GetHashCode
    public override int GetHashCode()
    {
        return Numerator.GetHashCode() ^ Denominator.GetHashCode();
    }

    // Implement greater than and less than
    public static bool operator >(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator > r2.Numerator * r1.Denominator;
    }

    public static bool operator <(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator < r2.Numerator * r1.Denominator;
    }

    public static bool operator >(Rational r1, int r2)
    {
        return r1 > new Rational(r2);
    }

    public static bool operator <(Rational r1, int r2)
    {
        return r1 < new Rational(r2);
    }

    public static bool operator >(int r1, Rational r2)
    {
        return new Rational(r1) > r2;
    }

    public static bool operator <(int r1, Rational r2)
    {
        return new Rational(r1) < r2;
    }

    // Implement greater than or equal to and less than or equal to
    public static bool operator >=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator >= r2.Numerator * r1.Denominator;
    }

    public static bool operator <=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator <= r2.Numerator * r1.Denominator;
    }

    public static bool operator >=(Rational r1, int r2)
    {
        return r1 >= new Rational(r2);
    }

    public static bool operator <=(Rational r1, int r2)
    {
        return r1 <= new Rational(r2);
    }

    public static bool operator >=(int r1, Rational r2)
    {
        return new Rational(r1) >= r2;
    }

    public static bool operator <=(int r1, Rational r2)
    {
        return new Rational(r1) <= r2;
    }

    // Implement implicit conversion from int to Rational
    public static implicit operator Rational(int i)
    {
        return new Rational(i);
    }

    // Implement implicit conversion from Rational to double
    public static implicit operator double(Rational r)
    {
        return (double)r.Numerator / r.Denominator;
    }

    public bool IsPositive()
    {

        if (Numerator >= 0 && Denominator > 0)
        {
            return true;
        }

        if (Numerator <= 0 && Denominator < 0)
        {
            return true;
        }

        return false;
    }

    public int Floor()
    {
        return IsPositive() ? Numerator / Denominator : (Numerator / Denominator) - 1;
    }

    public int Round() => (this + new Rational(1, 2)).Floor();

    public string ToDecimalString()
    {
        if (Numerator % Denominator == 0) return (Numerator / Denominator).ToString();
        double value = (double)Numerator / Denominator;
        return value.ToString("f3");
    }
}

public class ColumnSpecifier
{
    public bool UseString { get; }
    public string ColumnName { get; }
    public int OneBasedColumnIndex { get; }

    public ColumnSpecifier(string columnName)
    {
        UseString = true;
        ColumnName = columnName;
    }

    public ColumnSpecifier(int oneBasedColumnIndex)
    {
        UseString = false;
        OneBasedColumnIndex = oneBasedColumnIndex;
        ColumnName = "";
    }

    public override string ToString() => UseString ? $"\"{ColumnName}\"" : OneBasedColumnIndex.ToString();

    public int ColMatchZeroBased(List<string> line)
    {
        if (!UseString) return OneBasedColumnIndex - 1;

        int col = 0;
        foreach (var field in line)
        {
            if (string.Equals(field, ColumnName, StringComparison.OrdinalIgnoreCase))
            {
                return col;
            }
            col++;
        }

        return -1;
    }
}

public enum SpecifiedBool
{
    Unset = -1,
    False = 0,
    True = 1,
}
