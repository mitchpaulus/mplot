using System.Drawing;
using System.Text;
using Antlr4.Runtime.Tree;
using OneOf;

namespace mplot;

public class MplotListener : MplotBaseListener
{
    private readonly Maybe<FileInfo> _fileInfo;
    public readonly MplotArray<Color> LineColorArray = new(Color.Black, MplotArrayMode.Repeat, "Line color");

    public readonly MplotArray<int> LineWidthArray = new(1, MplotArrayMode.Repeat, "Line width");

    private int _currentVar = 1;

    public string? XLabel { get; set; }
    public string? YLabel { get; set; }
    public MplotArray<ColumnSelector> YcolsArray { get; } = new(new ColumnSpecifier(2), MplotArrayMode.Repeat, "Y col specifier");
    public MplotArray<ColumnSelector> XcolsArray { get; } = new(new ColumnSpecifier(1), MplotArrayMode.Repeat, "X col specifier");
    private MplotArray<Maybe<List<MplotToken>>> Ignores { get; } = new(NoneMarker.None, MplotArrayMode.Repeat, "Ignore expression");

    private readonly List<AdditionalTic> _additionalXTics = new();
    private readonly List<AdditionalTic> _additionalYTics = new();

    private readonly List<AdditionalTic> _additionalXTics2 = new();
    private readonly List<AdditionalTic> _additionalYTics2 = new();

    public bool Grid = false;

    public List<string> GnuplotLiterals = new();
    public string? OutputFile { get; set; }

    public string? Title { get; set; }

    public int DefaultDpi = 96;
    public readonly int PlotDpi; // 96 * 4 = 384
    public int TextDpi = 96;

    public SpecifiedBool Legend = SpecifiedBool.Unset;
    public SeriesStyle PlotSeriesStyle { get; private set; } = SeriesStyle.Scatter;

    public readonly MplotArray<SeriesStyle> PlotSeriesStyleArray = new(SeriesStyle.Scatter, MplotArrayMode.Repeat, "Series style");

    public RangeSpec XMin { get; set; } = AutoRange.Auto();
    public RangeSpec XMax { get; set; } = AutoRange.Auto();

    public Rational? XTicMin = null;
    public Rational? XTicMax = null;
    public Rational? XTicIncrement = null;

    public MplotArray<string> XTicLabels = new("X Tic Labels");

    public bool TicsOut = false;

    public RangeSpec YMin { get; set; } = AutoRange.Auto();
    public RangeSpec YMax { get; set; } = AutoRange.Auto();

    public Rational? YTicMin = null;
    public Rational? YTicMax = null;
    public Rational? YTicIncrement = null;
    public Rational Width { get; set; } = new(6);

    public Rational Height { get; set; } = new(4);

    public int Skip = 0;

    public readonly MplotArray<int> SkipArray = new(0, MplotArrayMode.Repeat, "Skip");

    public readonly MplotArray<Rational> BinSizeArray = new(1, MplotArrayMode.Repeat, "Bin size");

    public readonly MplotArray<Separator2> SeparatorArray = new(new List<Separator2> { new AutoSeparator() }, MplotArrayMode.Repeat, "Separator");

    public MplotArray<Marker> MarkerArray = new(new CircleMarker(), MplotArrayMode.Repeat, "Marker");

    public readonly MplotArray<DataSource> FilePathArray;

    public LegendPosition LegendPosition = new Empty();

    public int LegendCols = -1;
    public int LegendRows = -1;

    public string? FontName { get; set; } = null;
    public int FontSize { get; set; } = 10;

    public string? LastFilePath { get; set; }

    public MplotArray<string> SeriesNameArray = new MplotArray<string>("Series", MplotArrayMode.Repeat, "Series name");
    public bool TimeAxis { get; set; } = false;
    public Maybe<string> TimeFormat { get; set; } = NoneMarker.None;

    public string XFormat { get; set; } = "";

    private readonly ParseTreeWalker _walker = new();

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
            Ignores,
        };

        List<IMplotArray> normalLengthArrays = requiredArrays.Where(array => array.Mode() == MplotArrayMode.Normal).ToList();

        if (!normalLengthArrays.Any())
        {
            StringBuilder b = new();
            b.Append("At least one array for the series needs to be a countable array.\n");

            b.Append($"X cols: {XcolsArray.ToString()}\n");
            b.Append($"Y cols: {YcolsArray.ToString()}\n");
            b.Append($"Series name: {SeriesNameArray.ToString()}\n");
            b.Append($"File path: {FilePathArray.ToString()}\n");
            b.Append($"Marker: {MarkerArray.ToString()}\n");
            b.Append($"Skip: {SkipArray.ToString()}\n");
            b.Append($"Line color: {LineColorArray.ToString()}\n");
            b.Append($"Line width: {LineWidthArray.ToString()}\n");
            b.Append($"Plot series style: {PlotSeriesStyleArray.ToString()}\n");
            b.Append($"Separator: {SeparatorArray.ToString()}\n");
            b.Append($"Ignore: {Ignores.ToString()}\n");

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
            // Stream stream = FilePathArray[i].Stream();
            // StreamReader reader = new(stream, Encoding.UTF8);
            // var contents = reader.ReadToEnd();

            Separator2 sep2 = SeparatorArray[i];
            Separator sep = sep2.Determine(FilePathArray[i]);

            string xColSpecifier = XcolsArray[i].Match(specifier => specifier.ToString(), Evaluator.EvaluateGnuplot);
            string yColSpecifier = YcolsArray[i].Match(specifier => specifier.ToString(), Evaluator.EvaluateGnuplot);

            Maybe<List<MplotToken>> ignore = Ignores[i];

            if (TimeAxis)
            {
                xColSpecifier = TimeFormat.Match(
                    tf => $"(timecolumn(1, \"{tf}\"))",
                    _ => "(timecolumn(1, \"%Y-%m-%d %H:%M\"))"
                );
            }

            if (ignore.TryPickT0(out List<MplotToken> tokens, out NoneMarker _))
            {

                var colSpecifier = yColSpecifier;
                var newYSpecifier = YcolsArray[i].Match(
                    columnSpecifier => columnSpecifier.ToGnuplotExp(),
                    _ => colSpecifier
                );

                var xExp = XcolsArray[i].Match(
                    specifier => specifier.ToGnuplotExp(),
                    _ => xColSpecifier
                );

                var ignoreExpString = Evaluator.EvaluateGnuplot(tokens, xExp, newYSpecifier);

                // Wrap the Y value in a ternary condition. Has to be Y, since if you do it on a X that is time, doesn't seem to work.
                yColSpecifier = $"({ignoreExpString} ? NaN : {newYSpecifier})";
            }

            Series s = new(
                xColSpecifier,
                yColSpecifier,
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


    public MplotListener(Maybe<FileInfo> fileInfo)
    {
        _fileInfo = fileInfo;
        PlotDpi = DefaultDpi * 4;
        OutputFile = null;
        FilePathArray = new(new FileSource("data.tsv", _fileInfo), MplotArrayMode.Repeat, "File path");
    }

    public string ParseXLabelContext(MplotParser.XlabelContext context) => context.STRING().GetText().UnescapeMplotString();
    public string ParseYLabelContext(MplotParser.YlabelContext context) => context.STRING().GetText().UnescapeMplotString();

    public override void EnterXlabel(MplotParser.XlabelContext context) => XLabel = ParseXLabelContext(context).Trim();
    public override void EnterYlabel(MplotParser.YlabelContext context) => YLabel = ParseYLabelContext(context).Trim();

    public override void EnterFile(MplotParser.FileContext context)
    {
        bool repeat = context.stringArray().ASTERISK() is not null;
        bool cycle = context.stringArray().CYCLE() is not null;
        FilePathArray.Update(context.stringArray().STRING().Select(node => (DataSource)new FileSource(node.GetText().UnescapeMplotString(), _fileInfo)).ToList(), cycle, repeat);
    }

    public override void EnterLegendCols(MplotParser.LegendColsContext context) => LegendCols = int.Parse(context.INT().GetText());
    public override void EnterLegendRows(MplotParser.LegendRowsContext context) => LegendRows = int.Parse(context.INT().GetText());

    public override void EnterTicsOut(MplotParser.TicsOutContext context) => TicsOut = true;

    public override void EnterIgnore(MplotParser.IgnoreContext context)
    {
        if (context.ASTERISK() is not null) Ignores.SetMode(MplotArrayMode.Repeat);
        if (context.CYCLE() is not null) Ignores.SetMode(MplotArrayMode.Cycle);
        ExpressionListener l = new();
        _walker.Walk(l, context.expression());
        Ignores.Add(l.Tokens);
    }

    public override void EnterTitle(MplotParser.TitleContext context) => Title = context.STRING().GetText().UnescapeMplotString();
    public override void EnterGnuplotLiteral(MplotParser.GnuplotLiteralContext context) => GnuplotLiterals.AddRange(context.STRING().Select(s => s.GetText().UnescapeMplotString()));

    public override void EnterHist(MplotParser.HistContext context)
    {
        PlotSeriesStyleArray.Add(SeriesStyle.Boxes);
    }

    public override void EnterBinsize(MplotParser.BinsizeContext context)
    {
        BinSizeArray.Add(new Rational(context.rational()));
        if (context.ASTERISK().Length > 0) BinSizeArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterFstep(MplotParser.FstepContext context) => PlotSeriesStyleArray.Add(SeriesStyle.FStep);

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

    public override void EnterXticAdd(MplotParser.XticAddContext context)
    {
        Rational r = new Rational(context.rational());
        _additionalXTics.Add(new(r, context.STRING()?.GetText().UnescapeMplotString()));
    }

    public override void EnterYticAdd(MplotParser.YticAddContext context)
    {
        Rational r = new Rational(context.rational());
        _additionalYTics.Add(new(r, context.STRING()?.GetText().UnescapeMplotString()));
    }

    public override void EnterXticAdd2(MplotParser.XticAdd2Context context)
    {
        Rational r = new Rational(context.rational());
        _additionalXTics2.Add(new(r, context.STRING()?.GetText().UnescapeMplotString()));
    }

    public override void EnterYticAdd2(MplotParser.YticAdd2Context context)
    {
        Rational r = new Rational(context.rational());
        _additionalYTics2.Add(new(r, context.STRING()?.GetText().UnescapeMplotString()));
    }

    public override void EnterHeight(MplotParser.HeightContext context)
    {
        if (context.rational() is MplotParser.IntegerContext integerContext)
        {
            Height = new Rational(int.Parse(integerContext.INT().GetText()));
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
        XMin = context.range().lowLimit.ToRangeSpec();
        XMax = context.range().highLimit.ToRangeSpec();
    }

    public override void EnterYrange(MplotParser.YrangeContext context)
    {
        YMin = context.range().lowLimit.ToRangeSpec();
        YMax = context.range().highLimit.ToRangeSpec();
    }

    public override void EnterXmin(MplotParser.XminContext context) => XMin = context.rangeItem().ToRangeSpec();
    public override void EnterXmax(MplotParser.XmaxContext context) => XMax = context.rangeItem().ToRangeSpec();
    public override void EnterYmin(MplotParser.YminContext context) => YMin = context.rangeItem().ToRangeSpec();
    public override void EnterYmax(MplotParser.YmaxContext context) => YMax = context.rangeItem().ToRangeSpec();

    public override void EnterNoLegend(MplotParser.NoLegendContext context) => Legend = SpecifiedBool.False;


    public override void EnterWidth(MplotParser.WidthContext context)
    {
        if (context.rational() is MplotParser.IntegerContext integerContext)
        {
            Width = new Rational(int.Parse(integerContext.INT().GetText()));
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
        TimeFormat = context.STRING().GetText().UnescapeMplotString();
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
        var separatorText = context.stringArray().STRING().Select(s => s.GetText().UnescapeMplotString()).ToList();
        CharactersSeparator charactersSeparator = new(separatorText[0]);

        SeparatorArray.Update(separatorText.Select(s => (Separator2)new CharactersSeparator(s)).ToList(),
            context.stringArray().CYCLE().Any(), context.stringArray().ASTERISK().Any());
    }

    public override void EnterSeriesStyle(MplotParser.SeriesStyleContext context)
    {
        // The actual addition of the style to the array is handled in the listener for each.
        if (context.CYCLE() is not null) PlotSeriesStyleArray.SetMode(MplotArrayMode.Cycle);
        if (context.ASTERISK() is not null) PlotSeriesStyleArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterXticlabels(MplotParser.XticlabelsContext context)
    {
        var names = context.stringArray().STRING().Select(node => node.GetText().UnescapeMplotString().Trim()).ToList();
        XTicLabels.Update(names, context.stringArray().CYCLE().Any(), context.stringArray().ASTERISK().Any());
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

        if (MarkerArray.UseDefault)
        {
            MarkerArray = new(new NoMarker(), MplotArrayMode.Repeat, "Marker");
        }
    }

    public override void EnterMarker(MplotParser.MarkerContext context)
    {
        if (context.ASTERISK().Any())
        {
            MarkerArray.SetMode(MplotArrayMode.Repeat);
        }
        else if (context.CYCLE().Any())
        {
            MarkerArray.SetMode(MplotArrayMode.Cycle);
        }
    }

    public override void EnterMarkerTypeCount(MplotParser.MarkerTypeCountContext context)
    {
        Marker m = context.markerType() switch
        {
            MplotParser.CircleMarkerContext => new CircleMarker(),
            MplotParser.FilledCircleMarkerContext => new FilledCircleMarker(),
            MplotParser.PlusMarkerContext => new PlusMarker(),
            MplotParser.SquareMarkerContext => new SquareMarker(),
            _ => new CircleMarker()
        };

        int count = context.count() is null ? 1 : int.Parse(context.count().INT().GetText());
        for (int i = 0; i < count; i++) MarkerArray.Add(m);
    }

    public override void EnterLabels(MplotParser.LabelsContext context)
    {
        XLabel = context.STRING()[0].GetText().UnescapeMplotString();
        YLabel = context.STRING()[1].GetText().UnescapeMplotString();
    }

    public override void EnterRgbColor(MplotParser.RgbColorContext context)
    {
        var r = int.Parse(context.INT()[0].GetText());
        var g = int.Parse(context.INT()[1].GetText());
        var b = int.Parse(context.INT()[2].GetText());

        // Get Color from RGB
        Color color = Color.FromArgb(r, g, b);
        LineColorArray.Add(color);
        if (context.ASTERISK() is not null) LineColorArray.SetMode(MplotArrayMode.Repeat);
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
    }

    public override void EnterHexColor(MplotParser.HexColorContext context)
    {
        string hex = context.HEXCOLOR().GetText();

        // Get Color from Hex in form #RRGGBB
        Color color = ColorTranslator.FromHtml(hex);
        LineColorArray.Add(color);

        if (context.ASTERISK() is not null) LineColorArray.SetMode(MplotArrayMode.Repeat);
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
    }

    public override void EnterYcol(MplotParser.YcolContext context)
    {
        foreach (IParseTree? child in context.children)
        {
            // if (child is not ITerminalNode node) continue;
            if (child is MplotParser.IntCountContext intCountContext)
            {
                var count = int.Parse(intCountContext.count()?.INT().GetText() ?? "1");
                var integerCol = int.Parse(intCountContext.INT().GetText());
                ColumnSpecifier columnSpecifier = new(integerCol);
                for (int i = 0; i < count; i++)
                {
                    YcolsArray.Add(columnSpecifier);
                }
            }
            else if (child is MplotParser.StringCountContext stringCountContext)
            {
                var text = stringCountContext.STRING().GetText().UnescapeMplotString();
                var count = int.Parse(stringCountContext.count()?.INT().GetText() ?? "1");
                ColumnSpecifier columnSpecifier = new(text);

                for (int i = 0; i < count; i++)
                {
                    YcolsArray.Add(columnSpecifier);
                }
            }
            else if (child is ITerminalNode node)
            {
                if (node.Symbol.Type == MplotParser.ASTERISK)
                {
                    YcolsArray.SetMode(MplotArrayMode.Repeat);
                }
                else if (node.Symbol.Type == MplotParser.CYCLE)
                {
                    YcolsArray.SetMode(MplotArrayMode.Cycle);
                }
            }
        }
    }

    public override void EnterXcol(MplotParser.XcolContext context)
    {
        foreach (IParseTree? child in context.children)
        {
            // if (child is not ITerminalNode node) continue;
            if (child is MplotParser.IntCountContext intCountContext)
            {
                var count = int.Parse(intCountContext.count()?.INT().GetText() ?? "1");
                var integerCol = int.Parse(intCountContext.INT().GetText());
                ColumnSpecifier columnSpecifier = new(integerCol);
                for (int i = 0; i < count; i++)
                {
                    XcolsArray.Add(columnSpecifier);
                }
            }
            else if (child is MplotParser.StringCountContext stringCountContext)
            {
                var text = stringCountContext.STRING().GetText().UnescapeMplotString();
                var count = int.Parse(stringCountContext.count()?.INT().GetText() ?? "1");
                ColumnSpecifier columnSpecifier = new(text);

                for (int i = 0; i < count; i++)
                {
                    XcolsArray.Add(columnSpecifier);
                }
            }
            else if (child is ITerminalNode node)
            {
                if (node.Symbol.Type == MplotParser.ASTERISK)
                {
                    XcolsArray.SetMode(MplotArrayMode.Repeat);
                }
                else if (node.Symbol.Type == MplotParser.CYCLE)
                {
                    XcolsArray.SetMode(MplotArrayMode.Cycle);
                }
            }
        }
    }

    public override void EnterXexp(MplotParser.XexpContext context)
    {
        ExpressionListener l = new();
        _walker.Walk(l, context.expression());
        XcolsArray.Add(l.Tokens);
        if (context.ASTERISK() is not null) XcolsArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterYexp(MplotParser.YexpContext context)
    {
        ExpressionListener l = new();
        _walker.Walk(l, context.expression());
        YcolsArray.Add(l.Tokens);
        if (context.ASTERISK() is not null) YcolsArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterOutput(MplotParser.OutputContext context)
    {
        OutputFile = context.STRING().GetText().UnescapeMplotString();
    }

    public override void EnterXformat(MplotParser.XformatContext context)
    {
        XFormat = context.STRING().GetText().UnescapeMplotString();
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

    public override void EnterLineWidth(MplotParser.LineWidthContext context) => LineWidthArray.Update(context.intArray());

    public override void EnterKnownColorCycleAsterisk(MplotParser.KnownColorCycleAsteriskContext context)
    {
        if (context.CYCLE() is not null) LineColorArray.SetMode(MplotArrayMode.Cycle);
        if (context.ASTERISK() is not null) LineColorArray.SetMode(MplotArrayMode.Repeat);
    }

    // TODO: Add to arrays.
    public override void EnterRedColor(MplotParser.RedColorContext context) => LineColorArray.Add(Color.Red);
    public override void EnterGreenColor(MplotParser.GreenColorContext context) => LineColorArray.Add(Color.Green);
    public override void EnterBlueColor(MplotParser.BlueColorContext context) => LineColorArray.Add(Color.Blue);
    public override void EnterBlackColor(MplotParser.BlackColorContext context) => LineColorArray.Add(Color.Black);
    public override void EnterCcllcBlueColor(MplotParser.CcllcBlueColorContext context) => LineColorArray.Add(CcllcColors.Blue);
    public override void EnterCcllcGray(MplotParser.CcllcGrayContext context) => LineColorArray.Add(CcllcColors.Gray);
    public override void EnterCcllcMaroon(MplotParser.CcllcMaroonContext context) => LineColorArray.Add(CcllcColors.Maroon);
    public override void EnterCcllcGreen(MplotParser.CcllcGreenContext context) => LineColorArray.Add(CcllcColors.Green);
    public override void EnterCcllcYellow(MplotParser.CcllcYellowContext context) => LineColorArray.Add(CcllcColors.Yellow);

    public override void EnterTopLegend(MplotParser.TopLegendContext context) => LegendPosition = new Top();
    public override void EnterRightLegend(MplotParser.RightLegendContext context) => LegendPosition = new Right();
    public override void EnterUpperRightLegend(MplotParser.UpperRightLegendContext context) => LegendPosition = new InsideUpperRight();
    public override void EnterUpperLeftLegend(MplotParser.UpperLeftLegendContext context) => LegendPosition = new InsideUpperLeft();
    public override void EnterBottomLeftLegend(MplotParser.BottomLeftLegendContext context) => LegendPosition = new InsideBottomLeft();
    public override void EnterBottomRightLegend(MplotParser.BottomRightLegendContext context) => LegendPosition = new InsideBottomRight();
    public override void EnterGrid(MplotParser.GridContext context) => Grid = true;

    public override void EnterSeries(MplotParser.SeriesContext context)
    {
        var names = context.stringArray().STRING().Select(node => node.GetText().UnescapeMplotString().Trim()).ToList();
        SeriesNameArray.Update(names, context.stringArray().CYCLE().Any(), context.stringArray().ASTERISK().Any());
    }

    public override void EnterInlineData(MplotParser.InlineDataContext context)
    {
        string content = context.INLINEDATA().GetText();

        int indexOfFirstNewline = content.IndexOf('\n');

        // Check for an asterisk on the start token
        if (content[..indexOfFirstNewline].Contains('*'))
        {
            FilePathArray.SetMode(MplotArrayMode.Repeat);
        }

        int finalIndex = content.Length - 1;
        for (int i = finalIndex; i > -1; i--)
        {
            var character = content[i];
            // Also check for asterisk on the end token
            if (character == '*') FilePathArray.SetMode(MplotArrayMode.Repeat);
            if (character is ' ' or '\t' or '*' or '\n') continue;

            // 3 is for the 3 characters of 'END'
            finalIndex = i - 3;
            break;
        }

        string data = content[(indexOfFirstNewline + 1)..finalIndex];

        FilePathArray.Add(new InlineDataSource(data, "var" + _currentVar));
        _currentVar++;
    }

    public override void EnterCommandSource(MplotParser.CommandSourceContext context)
    {
        var arguments = context.STRING().Select(s => s.GetText().UnescapeMplotString()).ToList();
        var cmdDataSource = new CommandDataSource(arguments, _currentVar);
        _currentVar++;
        FilePathArray.Add(cmdDataSource);

        if (context.ASTERISK() is not null) FilePathArray.SetMode(MplotArrayMode.Repeat);
    }

    public override void EnterFont(MplotParser.FontContext context)
    {
        FontName = context.STRING().GetText().UnescapeMplotString();
    }

    public override void EnterFontSize(MplotParser.FontSizeContext context)
    {
        FontSize = int.Parse(context.INT().GetText());
    }

    public List<string> GnuplotCheck()
    {
        List<string> checks = new();

        if (!XTicLabels.UseDefault && XTicMin is null)
        {
            checks.Add($"Explicit x tic labels were provided, but no explicit x tic range (`xtics`) was provided.\n");
        }


        if (XTicMin is not null && XTicMax is not null && XTicIncrement is not null)
        {
            if (!XTicLabels.UseDefault)
            {
                if (XTicLabels.Mode() == MplotArrayMode.Normal)
                {
                    Rational division = (XTicMax - XTicMin) / XTicIncrement;
                    int numTicks = division.IsInt() ? (int)division + 1 : (int)division.Ceiling() ;
                    if (XTicLabels.Length != numTicks)
                    {
                        checks.Add($"The supplied number of x tic labels ({XTicLabels.Length}) does not equal the number of tics ({numTicks}).\n");
                    }
                }
            }
        }

        return checks;
    }


    public string WriteGnuplot()
    {
        List<string> lines = new();

        int widthPixels  =  (Width * PlotDpi).Round();
        int heightPixels = (Height * PlotDpi).Round();

        string fontScale = PlotDpi % TextDpi == 0 ? (PlotDpi / TextDpi).ToString() : ((double)PlotDpi / TextDpi).ToString("f3");

        string fontName = FontName ?? "";

        lines.Add($"set terminal pngcairo size {widthPixels},{heightPixels} font \"{fontName},{FontSize}\" fontscale {fontScale} pointscale {fontScale}");

        if (TimeAxis)
        {
            lines.Add("set timefmt \"%Y-%m-%d %H:%M\"");
            lines.Add("set xdata time");
        }

        lines.AddRange(GnuplotLiterals);

        // TODO: escape characters
        if (!string.IsNullOrWhiteSpace(OutputFile)) lines.Add($"set output \"{OutputFile}\"");

        if (!string.IsNullOrWhiteSpace(Title)) lines.Add($"set title \"{Title.ToGnuPlotString()}\"");
        if (!string.IsNullOrWhiteSpace(XLabel)) lines.Add($"set xlabel \"{XLabel}\"");
        if (!string.IsNullOrWhiteSpace(YLabel)) lines.Add($"set ylabel \"{YLabel}\"");

        if (Grid) lines.Add("set grid");
        List<Series> seriesList = GenSeries();

        string legendMaxRowsMaxCols =
            LegendCols > 0 ? $" maxcols {LegendCols}" : LegendRows > 0 ? $" maxrows {LegendRows}" : "";
        if (Legend == SpecifiedBool.False) lines.Add("set key off");
        else if (Legend == SpecifiedBool.True) lines.Add($"set key {LegendPosition.GnuPlotLegendPosition}{legendMaxRowsMaxCols}");
        else lines.Add(seriesList.Count > 1 ? $"set key {LegendPosition.GnuPlotLegendPosition}{legendMaxRowsMaxCols}" : "set key off");

        if (!XMin.IsAuto || !XMax.IsAuto) lines.Add($"set xrange [{XMin.ToGnuplot()}:{XMax.ToGnuplot()}]");
        if (!YMin.IsAuto || !YMax.IsAuto) lines.Add($"set yrange [{YMin.ToGnuplot()}:{YMax.ToGnuplot()}]");

        if (XTicMin is not null && XTicMax is not null && XTicIncrement is not null)
        {
            if (!XTicLabels.UseDefault)
            {
                Rational tic = XTicMin;
                StringBuilder b = new();
                b.Append("(");
                int index = 0;
                string label = XTicLabels[index];
                b.Append($"{label.ToGnuPlotString()} {tic.ToDecimalString()}");
                tic += XTicIncrement;
                index++;

                while (tic <= XTicMax)
                {
                    label = XTicLabels[index];
                    b.Append($", {label.ToGnuPlotString()} {tic.ToDecimalString()}");
                    tic += XTicIncrement;
                    index++;
                }

                b.Append(")");

                lines.Add($"set xtics {b}");
            }
            else
            {
                lines.Add( $"set xtics {XTicMin.ToDecimalString()}, {XTicIncrement.ToDecimalString()}, {XTicMax.ToDecimalString()}");
            }
        }

        if (_additionalXTics.Count > 0)
        {
            lines.Add($"set xtics add ({string.Join(", ", _additionalXTics.Select(tic => tic.ToGnuplot()))})");
        }

        if (_additionalXTics2.Count > 0)
        {
            lines.Add($"set x2tics add ({string.Join(", ", _additionalXTics2.Select(tic => tic.ToGnuplot()))})");
            lines.Add("set link x2");
            lines.Add("set format x2 \"\"");
        }

        if (YTicMin is not null && YTicMax is not null && YTicIncrement is not null)
        {
            lines.Add( $"set ytics {YTicMin.ToDecimalString()}, {YTicIncrement.ToDecimalString()}, {YTicMax.ToDecimalString()}");
        }

        if (_additionalYTics.Count > 0)
        {
            lines.Add($"set ytics add ({string.Join(", ", _additionalYTics.Select(tic => tic.ToGnuplot()))})");
        }

        if (_additionalYTics2.Count > 0)
        {
            lines.Add($"set y2tics add ({string.Join(", ", _additionalYTics2.Select(tic => tic.ToGnuplot()))})");
            lines.Add($"set link y2");
            lines.Add("set format y2 \"\"");
        }

        if (TicsOut)
        {
            lines.Add("set xtics out");
            lines.Add("set ytics out");
        }

        // Sets the format for tick marks. Typical format modifiers like '%f' and '%e'
        // Also sets the time formats
        if (!string.IsNullOrWhiteSpace(XFormat))
        {
            lines.Add($"set format x \"{XFormat}\"");
        }

        // If the File Path is set to repeat mode and the last one is a inline data source, we don't want to repeat.
        HashSet<string> varNamesUsed = new();
        foreach (var source in seriesList.Where(series => series.FilePath.IsInlindeDataSource).Select(series => series.FilePath.AsT1))
        {
            if (varNamesUsed.Contains(source.VariableName)) continue;

            lines.Add($"${source.VariableName}<<EOF");
            lines.AddRange(source.Contents.SplitLines());
            lines.Add("EOF");

            varNamesUsed.Add(source.VariableName);
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
    public static SeriesStyle Boxes = new(2, new List<string> {"hist"}, "boxes", false);
    public static SeriesStyle FStep = new(3, new List<string> {"fstep"}, "fstep", false);

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

public class InsideBottomLeft : LegendPosition
{
    public override string GnuPlotLegendPosition => "inside left bottom";
}

public class InsideBottomRight : LegendPosition
{
    public override string GnuPlotLegendPosition => "inside right bottom";
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
        if (secondLine is null) return new WhiteSpaceSeparator();

        // Check if the number of commas in the first line is the same as the second.
        int firstLineCommas = firstLine.Count(c => c == ',');
        int secondLineCommas = secondLine.Count(c => c == ',');

        if (firstLineCommas == secondLineCommas && firstLineCommas > 0) return new CommaSeparator();

        return new WhiteSpaceSeparator();
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

public class ColumnSelector : OneOfBase<ColumnSpecifier, List<MplotToken>>
{
    protected ColumnSelector(OneOf<ColumnSpecifier, List<MplotToken>> _) : base(_) { }

    public static implicit operator ColumnSelector(ColumnSpecifier _) => new(_);
    public static implicit operator ColumnSelector(List<MplotToken> _) => new(_);

    public override string ToString()
    {
        return Match(
            specifier => specifier.ToString(),
            list => string.Join(" ", list.Select(token => token.ToString()))
        );
    }
}

public class ColumnSpecifier
{
    public bool UseString { get; }
    /// <summary>
    /// Column name should not be surrounded with quotes
    /// </summary>
    public string ColumnName { get; }
    public int OneBasedColumnIndex { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="columnName">columnName should not be surrounded with quotes</param>
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

    public string ToGnuplotExp() => UseString ? $"column(\"{ColumnName}\")" : $"${OneBasedColumnIndex}";

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

public enum ColType
{
    Standard,
    Expression,
}

public enum NoneMarker
{
    None
}

public class AdditionalTic
{
    public Rational Rational { get; }
    public string?  Label { get; }

    public AdditionalTic(Rational r, string? l)
    {
        Rational = r;
        Label = l;
    }

    public string ToGnuplot()
    {
        // TODO: escape stuff for label.
        return $"\"{Label?.Trim() ?? ""}\" {Rational.ToDecimalString()}";
    }
}

public class Maybe<T> : OneOfBase<T, NoneMarker>
{
    protected Maybe(OneOf<T, NoneMarker> input) : base(input)
    {
    }

    public static implicit operator Maybe<T>(T _) => new(_);
    public static implicit operator Maybe<T>(NoneMarker _) => new(_);

    public bool IsNone => IsT1;
    public T AsValue => AsT0;
}
