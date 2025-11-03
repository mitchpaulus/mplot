using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MplotGui.Commands;
using MplotLib;

namespace MplotGui.ViewModels;

public enum LegendModeOption
{
    Auto,
    On,
    Off
}

public enum LegendPositionOption
{
    Automatic,
    Top,
    Right,
    InsideUpperRight,
    InsideUpperLeft,
    InsideBottomLeft,
    InsideBottomRight,
    OuterTopRight
}

public class PlotViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _xLabel = string.Empty;
    private string _yLabel = string.Empty;
    private string _y2Label = string.Empty;
    private string _outputFile = string.Empty;
    private string _fontName = string.Empty;
    private int _fontSize = 10;
    private int _width = 6;
    private int _height = 4;
    private bool _grid;
    private LegendModeOption _legendMode = LegendModeOption.Auto;
    private LegendPositionOption _legendPosition = LegendPositionOption.Automatic;
    private int _legendColumns = -1;
    private int _legendRows = -1;
    private bool _timeAxis;
    private string _timeFormat = string.Empty;
    private string _xFormat = string.Empty;
    private string _yFormat = string.Empty;
    private string _gnuplotLiterals = string.Empty;
    private int _defaultDpi = 96;
    private int _textDpi = 96;
    private string _xRangeMin = string.Empty;
    private string _xRangeMax = string.Empty;
    private string _yRangeMin = string.Empty;
    private string _yRangeMax = string.Empty;
    private string _y2RangeMin = string.Empty;
    private string _y2RangeMax = string.Empty;
    private string _statusMessage = string.Empty;
    private string? _previewMplot;
    private string? _lastGnuplotScript;

    public PlotViewModel()
    {
        StringArguments = new ObservableCollection<StringArgumentViewModel>();

        FilePathItems = new MplotArrayEditorViewModel<DataSourceItemViewModel>(_ => new DataSourceItemViewModel(),
            new[] { new DataSourceItemViewModel() });
        SeriesNameItems = new MplotArrayEditorViewModel<ValueViewModel<string>>(index => new ValueViewModel<string>($"Series {index + 1}"),
            new[] { new ValueViewModel<string>("Series 1") });
        XColumnItems = new MplotArrayEditorViewModel<ValueViewModel<string>>(_ => new ValueViewModel<string>("1"),
            new[] { new ValueViewModel<string>("1") });
        YColumnItems = new MplotArrayEditorViewModel<ValueViewModel<string>>(_ => new ValueViewModel<string>("2"),
            new[] { new ValueViewModel<string>("2") });
        MarkerItems = new MplotArrayEditorViewModel<ValueViewModel<MarkerOption>>(_ => new ValueViewModel<MarkerOption>(MarkerOption.Circle),
            new[] { new ValueViewModel<MarkerOption>(MarkerOption.Circle) });
        LineColorItems = new MplotArrayEditorViewModel<ValueViewModel<string>>(_ => new ValueViewModel<string>("#1f77b4"),
            new[] { new ValueViewModel<string>("#1f77b4") });
        LineWidthItems = new MplotArrayEditorViewModel<ValueViewModel<int>>(_ => new ValueViewModel<int>(2),
            new[] { new ValueViewModel<int>(2) });
        SeriesStyleItems = new MplotArrayEditorViewModel<ValueViewModel<SeriesStyleOption>>(_ => new ValueViewModel<SeriesStyleOption>(SeriesStyleOption.Scatter),
            new[] { new ValueViewModel<SeriesStyleOption>(SeriesStyleOption.Scatter) });
        SeparatorItems = new MplotArrayEditorViewModel<ValueViewModel<SeparatorOption>>(_ => new ValueViewModel<SeparatorOption>(SeparatorOption.Auto),
            new[] { new ValueViewModel<SeparatorOption>(SeparatorOption.Auto) });
        DashTypeItems = new MplotArrayEditorViewModel<ValueViewModel<DashTypeOption>>(_ => new ValueViewModel<DashTypeOption>(DashTypeOption.Solid),
            new[] { new ValueViewModel<DashTypeOption>(DashTypeOption.Solid) });
        YAxisSideItems = new MplotArrayEditorViewModel<ValueViewModel<YAxisSideOption>>(_ => new ValueViewModel<YAxisSideOption>(YAxisSideOption.Left),
            new[] { new ValueViewModel<YAxisSideOption>(YAxisSideOption.Left) });
        SkipItems = new MplotArrayEditorViewModel<ValueViewModel<int>>(_ => new ValueViewModel<int>(0),
            new[] { new ValueViewModel<int>(0) });
        IgnoreExpressionItems = new MplotArrayEditorViewModel<ValueViewModel<string>>(_ => new ValueViewModel<string>(string.Empty),
            new[] { new ValueViewModel<string>(string.Empty) });

        AddStringArgumentCommand = new RelayCommand(AddStringArgument);
        RemoveStringArgumentCommand = new RelayCommand(_ => RemoveSelectedStringArgument(), _ => SelectedStringArgument != null);
    }

    public Array LegendModeOptions => Enum.GetValues(typeof(LegendModeOption));
    public Array LegendPositionOptions => Enum.GetValues(typeof(LegendPositionOption));
    public Array SeriesStyleOptions => Enum.GetValues(typeof(SeriesStyleOption));
    public Array MarkerOptions => Enum.GetValues(typeof(MarkerOption));
    public Array SeparatorOptions => Enum.GetValues(typeof(SeparatorOption));
    public Array DashTypeOptions => Enum.GetValues(typeof(DashTypeOption));
    public Array YAxisSideOptions => Enum.GetValues(typeof(YAxisSideOption));
    public Array DataSourceOptions => Enum.GetValues(typeof(DataSourceOption));
    public Array ArrayModeOptions => Enum.GetValues(typeof(MplotArrayMode));

    public MplotArrayEditorViewModel<DataSourceItemViewModel> FilePathItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<string>> SeriesNameItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<string>> XColumnItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<string>> YColumnItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<MarkerOption>> MarkerItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<string>> LineColorItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<int>> LineWidthItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<SeriesStyleOption>> SeriesStyleItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<SeparatorOption>> SeparatorItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<DashTypeOption>> DashTypeItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<YAxisSideOption>> YAxisSideItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<int>> SkipItems { get; }
    public MplotArrayEditorViewModel<ValueViewModel<string>> IgnoreExpressionItems { get; }

    public ObservableCollection<StringArgumentViewModel> StringArguments { get; }

    private StringArgumentViewModel? _selectedStringArgument;
    public StringArgumentViewModel? SelectedStringArgument
    {
        get => _selectedStringArgument;
        set
        {
            if (SetProperty(ref _selectedStringArgument, value))
            {
                RemoveStringArgumentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand AddStringArgumentCommand { get; }
    public RelayCommand RemoveStringArgumentCommand { get; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string XLabel
    {
        get => _xLabel;
        set => SetProperty(ref _xLabel, value);
    }

    public string YLabel
    {
        get => _yLabel;
        set => SetProperty(ref _yLabel, value);
    }

    public string Y2Label
    {
        get => _y2Label;
        set => SetProperty(ref _y2Label, value);
    }

    public string OutputFile
    {
        get => _outputFile;
        set => SetProperty(ref _outputFile, value);
    }

    public string FontName
    {
        get => _fontName;
        set => SetProperty(ref _fontName, value);
    }

    public int FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public bool Grid
    {
        get => _grid;
        set => SetProperty(ref _grid, value);
    }

    public LegendModeOption LegendMode
    {
        get => _legendMode;
        set => SetProperty(ref _legendMode, value);
    }

    public LegendPositionOption LegendPosition
    {
        get => _legendPosition;
        set => SetProperty(ref _legendPosition, value);
    }

    public int LegendColumns
    {
        get => _legendColumns;
        set => SetProperty(ref _legendColumns, value);
    }

    public int LegendRows
    {
        get => _legendRows;
        set => SetProperty(ref _legendRows, value);
    }

    public bool TimeAxis
    {
        get => _timeAxis;
        set => SetProperty(ref _timeAxis, value);
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set => SetProperty(ref _timeFormat, value);
    }

    public string XFormat
    {
        get => _xFormat;
        set => SetProperty(ref _xFormat, value);
    }

    public string YFormat
    {
        get => _yFormat;
        set => SetProperty(ref _yFormat, value);
    }

    public string GnuplotLiterals
    {
        get => _gnuplotLiterals;
        set => SetProperty(ref _gnuplotLiterals, value);
    }

    public int DefaultDpi
    {
        get => _defaultDpi;
        set => SetProperty(ref _defaultDpi, value);
    }

    public int TextDpi
    {
        get => _textDpi;
        set => SetProperty(ref _textDpi, value);
    }

    public string XRangeMin
    {
        get => _xRangeMin;
        set => SetProperty(ref _xRangeMin, value);
    }

    public string XRangeMax
    {
        get => _xRangeMax;
        set => SetProperty(ref _xRangeMax, value);
    }

    public string YRangeMin
    {
        get => _yRangeMin;
        set => SetProperty(ref _yRangeMin, value);
    }

    public string YRangeMax
    {
        get => _yRangeMax;
        set => SetProperty(ref _yRangeMax, value);
    }

    public string Y2RangeMin
    {
        get => _y2RangeMin;
        set => SetProperty(ref _y2RangeMin, value);
    }

    public string Y2RangeMax
    {
        get => _y2RangeMax;
        set => SetProperty(ref _y2RangeMax, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? PreviewMplot
    {
        get => _previewMplot;
        private set => SetProperty(ref _previewMplot, value);
    }

    public void UpdateStatus(string message)
    {
        StatusMessage = message;
    }

    public void GenerateMplotPreview()
    {
        try
        {
            PreviewMplot = BuildMplotScript();
            StatusMessage = $"mplot generated at {DateTime.Now:T}";
        }
        catch (Exception ex)
        {
            PreviewMplot = null;
            StatusMessage = ex.Message;
        }
    }

    public MplotArrayMode FilePathMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode SeriesNameMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode XColumnMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode YColumnMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode MarkerMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode LineColorMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode LineWidthMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode SeriesStyleMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode SeparatorMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode DashTypeMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode YAxisSideMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode SkipMode { get; set; } = MplotArrayMode.Normal;
    public MplotArrayMode IgnoreMode { get; set; } = MplotArrayMode.Normal;

    public (bool Success, string? Script, string? Error) TryGenerateGnuplot()
    {
        try
        {
            var stringArgs = StringArguments.Select(arg => arg.Value).ToList();
            MplotListener listener = new(NoneMarker.None, stringArgs);
            listener.Title = Title;
            listener.XLabel = string.IsNullOrWhiteSpace(XLabel) ? null : XLabel;
            listener.YLabel = string.IsNullOrWhiteSpace(YLabel) ? null : YLabel;
            listener.Y2Label = string.IsNullOrWhiteSpace(Y2Label) ? null : Y2Label;
            listener.OutputFile = string.IsNullOrWhiteSpace(OutputFile) ? null : OutputFile;
            listener.FontName = string.IsNullOrWhiteSpace(FontName) ? null : FontName;
            listener.FontSize = FontSize;
            listener.Width = new Rational(Width);
            listener.Height = new Rational(Height);
            listener.Grid = Grid;
            listener.DefaultDpi = DefaultDpi;
            listener.TextDpi = TextDpi;
            listener.TimeAxis = TimeAxis;
            listener.TimeFormat = string.IsNullOrWhiteSpace(TimeFormat) ? NoneMarker.None : TimeFormat;
            listener.XFormat = XFormat;
            listener.YFormat = YFormat;

            listener.Legend = LegendMode switch
            {
                LegendModeOption.Auto => SpecifiedBool.Unset,
                LegendModeOption.On => SpecifiedBool.True,
                LegendModeOption.Off => SpecifiedBool.False,
                _ => SpecifiedBool.Unset
            };
            listener.LegendPosition = LegendPosition switch
            {
                LegendPositionOption.Automatic => new Empty(),
                LegendPositionOption.Top => new Top(),
                LegendPositionOption.Right => new Right(),
                LegendPositionOption.InsideUpperRight => new InsideUpperRight(),
                LegendPositionOption.InsideUpperLeft => new InsideUpperLeft(),
                LegendPositionOption.InsideBottomLeft => new InsideBottomLeft(),
                LegendPositionOption.InsideBottomRight => new InsideBottomRight(),
                LegendPositionOption.OuterTopRight => new OuterTopRight(),
                _ => new Empty()
            };
            listener.LegendCols = LegendColumns;
            listener.LegendRows = LegendRows;

            listener.XMin = ParseRangeValue(XRangeMin);
            listener.XMax = ParseRangeValue(XRangeMax);
            listener.YMin = ParseRangeValue(YRangeMin);
            listener.YMax = ParseRangeValue(YRangeMax);
            listener.Y2Min = ParseRangeValue(Y2RangeMin);
            listener.Y2Max = ParseRangeValue(Y2RangeMax);

            listener.GnuplotLiterals.Clear();
            if (!string.IsNullOrWhiteSpace(GnuplotLiterals))
            {
                foreach (var line in GnuplotLiterals.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    listener.GnuplotLiterals.Add(line);
                }
            }

            listener.FilePathArray.Clear();
            listener.SeriesNameArray.Clear();
            listener.XcolsArray.Clear();
            listener.YcolsArray.Clear();
            listener.MarkerArray.Clear();
            listener.LineColorArray.Clear();
            listener.LineWidthArray.Clear();
            listener.PlotSeriesStyleArray.Clear();
            listener.SeparatorArray.Clear();
            listener.DashTypeArray.Clear();
            listener.SkipArray.Clear();
            listener.YAxisSidesArray.Clear();
            listener.Ignores.Clear();

            listener.FilePathArray.SetMode(FilePathMode);
            listener.SeriesNameArray.SetMode(SeriesNameMode);
            listener.XcolsArray.SetMode(XColumnMode);
            listener.YcolsArray.SetMode(YColumnMode);
            listener.MarkerArray.SetMode(MarkerMode);
            listener.LineColorArray.SetMode(LineColorMode);
            listener.LineWidthArray.SetMode(LineWidthMode);
            listener.PlotSeriesStyleArray.SetMode(SeriesStyleMode);
            listener.SeparatorArray.SetMode(SeparatorMode);
            listener.DashTypeArray.SetMode(DashTypeMode);
            listener.SkipArray.SetMode(SkipMode);
            listener.YAxisSidesArray.SetMode(YAxisSideMode);
            listener.Ignores.SetMode(IgnoreMode);

            int inlineIndex = 0;
            foreach (var source in FilePathItems.Items)
            {
                DataSource dataSource = source.DataSource == DataSourceOption.Inline
                    ? new InlineDataSource(source.InlineData, $"inline_{inlineIndex++}")
                    : new FileSource(source.FilePath, NoneMarker.None);
                listener.FilePathArray.Add(dataSource);
            }

            foreach (var (seriesName, index) in SeriesNameItems.Items.Select((value, index) => (value, index)))
            {
                string value = string.IsNullOrWhiteSpace(seriesName.Value) ? $"Series {index + 1}" : seriesName.Value;
                listener.SeriesNameArray.Add(value);
            }

            foreach (var column in XColumnItems.Items)
            {
                listener.XcolsArray.Add(CreateColumnSelector(column.Value, stringArgs));
            }

            foreach (var column in YColumnItems.Items)
            {
                listener.YcolsArray.Add(CreateColumnSelector(column.Value, stringArgs));
            }

            foreach (var marker in MarkerItems.Items)
            {
                listener.MarkerArray.Add(MapMarker(marker.Value));
            }

            foreach (var color in LineColorItems.Items)
            {
                listener.LineColorArray.Add(ToColor(color.Value));
            }

            foreach (var width in LineWidthItems.Items)
            {
                listener.LineWidthArray.Add(width.Value);
            }

            foreach (var style in SeriesStyleItems.Items)
            {
                listener.PlotSeriesStyleArray.Add(MapSeriesStyle(style.Value));
            }

            foreach (var separator in SeparatorItems.Items)
            {
                listener.SeparatorArray.Add(MapSeparator(separator.Value));
            }

            foreach (var dash in DashTypeItems.Items)
            {
                listener.DashTypeArray.Add(MapDashType(dash.Value));
            }

            foreach (var skip in SkipItems.Items)
            {
                listener.SkipArray.Add(skip.Value);
            }

            foreach (var axisSide in YAxisSideItems.Items)
            {
                listener.YAxisSidesArray.Add(axisSide.Value == YAxisSideOption.Left ? YAxisSide.Left : YAxisSide.Right);
            }

            foreach (var ignore in IgnoreExpressionItems.Items)
            {
                if (string.IsNullOrWhiteSpace(ignore.Value))
                {
                    listener.Ignores.Add(NoneMarker.None);
                }
                else
                {
                    var tokens = ParseExpression(ignore.Value, stringArgs);
                    listener.Ignores.Add(tokens);
                }
            }

            string script = listener.WriteGnuplot();
            _lastGnuplotScript = script;
            StatusMessage = $"Gnuplot generated at {DateTime.Now:T}";
            return (true, script, null);
        }
        catch (Exception ex)
        {
            _lastGnuplotScript = null;
            StatusMessage = ex.Message;
            return (false, null, ex.Message);
        }
    }

    private void ConfigureArrayModes(MplotListener listener)
    {
        // Methods above reinitialise arrays, so this method remains for future extension if we choose to
        // adjust existing instances rather than recreating them.
    }

    private ColumnSelector CreateColumnSelector(string text, List<string> stringArgs)
    {
        if (int.TryParse(text, out int index))
        {
            return new ColumnSpecifier(index);
        }

        string trimmed = text.Trim();
        if (trimmed.StartsWith("$") || trimmed.Contains(' '))
        {
            var tokens = ParseExpressionTokens(trimmed, stringArgs);
            return tokens;
        }

        trimmed = trimmed.Trim('"');
        return new ColumnSpecifier(trimmed);
    }

    private List<MplotToken> ParseExpressionTokens(string expression, List<string> stringArgs)
    {
        AntlrInputStream inputStream = new(expression);
        MplotLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        MplotParser parser = new(tokenStream);
        var context = parser.expression();
        ExpressionListener listener = new(stringArgs);
        ParseTreeWalker walker = new();
        walker.Walk(listener, context);
        return listener.Tokens;
    }

    private Maybe<List<MplotToken>> ParseExpression(string expression, List<string> stringArgs)
    {
        return ParseExpressionTokens(expression, stringArgs);
    }

    private static Marker MapMarker(MarkerOption option) => option switch
    {
        MarkerOption.None => new NoMarker(),
        MarkerOption.Circle => new CircleMarker(),
        MarkerOption.FilledCircle => new FilledCircleMarker(),
        MarkerOption.Square => new SquareMarker(),
        MarkerOption.FilledSquare => new FilledSquareMarker(),
        MarkerOption.Plus => new PlusMarker(),
        MarkerOption.X => new XMarker(),
        MarkerOption.Line => new LineMarker(),
        _ => new CircleMarker()
    };

    private static SeriesStyle MapSeriesStyle(SeriesStyleOption option) => option switch
    {
        SeriesStyleOption.Line => SeriesStyle.Line,
        SeriesStyleOption.Boxes => SeriesStyle.Boxes,
        SeriesStyleOption.FStep => SeriesStyle.FStep,
        _ => SeriesStyle.Scatter
    };

    private static Separator2 MapSeparator(SeparatorOption option) => option switch
    {
        SeparatorOption.Comma => new CommaSeparator(),
        SeparatorOption.Tab => new TabSeparator(),
        SeparatorOption.Whitespace => new WhiteSpaceSeparator(),
        _ => new AutoSeparator()
    };

    private static DashType MapDashType(DashTypeOption option) => option switch
    {
        DashTypeOption.Simple => new SimpleDash(),
        _ => new SolidDash()
    };

    private static Color ToColor(string color)
    {
        try
        {
            return ColorTranslator.FromHtml(color);
        }
        catch
        {
            return Color.Black;
        }
    }

    private static RangeSpec ParseRangeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AutoRange.Auto();
        }

        if (DateTime.TryParse(value, out var dt))
        {
            return dt;
        }

        if (double.TryParse(value, out double numeric))
        {
            return ToRational(numeric);
        }

        return AutoRange.Auto();
    }

    private static Rational ToRational(double value)
    {
        const int precision = 1000;
        int numerator = (int)Math.Round(value * precision);
        int denominator = precision;
        int gcd = GreatestCommonDivisor(Math.Abs(numerator), denominator);
        return new Rational(numerator / gcd, denominator / gcd);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a == 0 ? 1 : a;
    }

    private string BuildMplotScript()
    {
        StringBuilder builder = new();

        static string Quote(string value)
        {
            string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        static string RangePart(string value) => string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();

        void AppendRange(string command, string min, string max)
        {
            if (string.IsNullOrWhiteSpace(min) && string.IsNullOrWhiteSpace(max)) return;
            builder.AppendLine($"{command} {RangePart(min)}:{RangePart(max)}");
        }

        if (!string.IsNullOrWhiteSpace(Title)) builder.AppendLine($"ct {Quote(Title)}");
        if (!string.IsNullOrWhiteSpace(XLabel)) builder.AppendLine($"xl {Quote(XLabel)}");
        if (!string.IsNullOrWhiteSpace(YLabel)) builder.AppendLine($"yl {Quote(YLabel)}");
        if (!string.IsNullOrWhiteSpace(Y2Label)) builder.AppendLine($"y2l {Quote(Y2Label)}");
        if (!string.IsNullOrWhiteSpace(OutputFile)) builder.AppendLine($"out {Quote(OutputFile)}");
        if (!string.IsNullOrWhiteSpace(FontName)) builder.AppendLine($"font {Quote(FontName)}");
        builder.AppendLine($"fs {FontSize}");
        builder.AppendLine($"w {Width}");
        builder.AppendLine($"h {Height}");
        if (Grid) builder.AppendLine("grid");
        if (LegendMode == LegendModeOption.Off) builder.AppendLine("nokey");
        else if (LegendMode == LegendModeOption.On) builder.AppendLine("legend");
        if (LegendColumns >= 0) builder.AppendLine($"legendcols {LegendColumns}");
        if (LegendRows >= 0) builder.AppendLine($"legendrows {LegendRows}");
        if (TimeAxis) builder.AppendLine("time");
        if (!string.IsNullOrWhiteSpace(TimeFormat)) builder.AppendLine($"tf {Quote(TimeFormat)}");
        if (!string.IsNullOrWhiteSpace(XFormat)) builder.AppendLine($"xf {Quote(XFormat)}");
        if (!string.IsNullOrWhiteSpace(YFormat)) builder.AppendLine($"yf {Quote(YFormat)}");

        AppendRange("xr", XRangeMin, XRangeMax);
        AppendRange("yr", YRangeMin, YRangeMax);
        AppendRange("y2r", Y2RangeMin, Y2RangeMax);

        builder.AppendLine($"dpi {DefaultDpi}");

        if (!string.IsNullOrWhiteSpace(GnuplotLiterals))
        {
            foreach (var literal in GnuplotLiterals.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                builder.AppendLine($"gnuplot {Quote(literal)}");
            }
        }

        void AppendWithSpacing(Action action)
        {
            int before = builder.Length;
            action();
            if (builder.Length > before)
            {
                builder.AppendLine();
            }
        }

        AppendWithSpacing(() =>
        {
            foreach (var source in FilePathItems.Items)
            {
                if (source.DataSource == DataSourceOption.Inline)
                {
                    builder.AppendLine("start");
                    if (!string.IsNullOrWhiteSpace(source.InlineData))
                    {
                        foreach (var line in source.InlineData.Replace("\r", string.Empty).Split('\n'))
                        {
                            builder.AppendLine(line);
                        }
                    }
                    builder.AppendLine("end");
                }
                else if (!string.IsNullOrWhiteSpace(source.FilePath))
                {
                    builder.AppendLine($"f {Quote(source.FilePath)}");
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var column in XColumnItems.Items)
            {
                if (!string.IsNullOrWhiteSpace(column.Value))
                {
                    builder.AppendLine($"x {column.Value}");
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var column in YColumnItems.Items)
            {
                if (!string.IsNullOrWhiteSpace(column.Value))
                {
                    builder.AppendLine($"y {column.Value}");
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var (seriesName, index) in SeriesNameItems.Items.Select((value, index) => (value, index)))
            {
                string value = string.IsNullOrWhiteSpace(seriesName.Value) ? $"Series {index + 1}" : seriesName.Value;
                builder.AppendLine($"s {Quote(value)}");
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var color in LineColorItems.Items)
            {
                if (!string.IsNullOrWhiteSpace(color.Value))
                {
                    string trimmed = color.Value.Trim();
                    builder.AppendLine(trimmed.StartsWith('#') ? trimmed : "#" + trimmed);
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var width in LineWidthItems.Items)
            {
                builder.AppendLine($"lw {width.Value}");
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var style in SeriesStyleItems.Items)
            {
                string styleText = style.Value switch
                {
                    SeriesStyleOption.Scatter => "scatter",
                    SeriesStyleOption.Line => "line",
                    SeriesStyleOption.Boxes => "hist",
                    SeriesStyleOption.FStep => "fstep",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(styleText)) builder.AppendLine(styleText);
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var marker in MarkerItems.Items)
            {
                string markerText = marker.Value switch
                {
                    MarkerOption.None => "nomarker",
                    MarkerOption.Circle => "o",
                    MarkerOption.FilledCircle => "filledcircle",
                    MarkerOption.Square => "square",
                    MarkerOption.FilledSquare => "filledsquare",
                    MarkerOption.Plus => "plus",
                    MarkerOption.X => "x",
                    MarkerOption.Line => "line",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(markerText)) builder.AppendLine(markerText);
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var dash in DashTypeItems.Items)
            {
                string dashText = dash.Value switch
                {
                    DashTypeOption.Simple => "dash",
                    DashTypeOption.Solid => "nodash",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(dashText)) builder.AppendLine(dashText);
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var separator in SeparatorItems.Items)
            {
                string separatorText = separator.Value switch
                {
                    SeparatorOption.Comma => "comma",
                    SeparatorOption.Tab => "tab",
                    SeparatorOption.Whitespace => "sep \" \"",
                    SeparatorOption.Auto => string.Empty,
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(separatorText)) builder.AppendLine(separatorText);
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var skip in SkipItems.Items)
            {
                if (skip.Value > 0)
                {
                    builder.AppendLine($"skip {skip.Value}");
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var axisSide in YAxisSideItems.Items)
            {
                if (axisSide.Value == YAxisSideOption.Right)
                {
                    builder.AppendLine("y2");
                }
            }
        });

        AppendWithSpacing(() =>
        {
            foreach (var ignore in IgnoreExpressionItems.Items)
            {
                if (!string.IsNullOrWhiteSpace(ignore.Value))
                {
                    builder.AppendLine($"ignore {ignore.Value}");
                }
            }
        });

        return builder.ToString().TrimEnd();
    }

    private void AddStringArgument()
    {
        var arg = new StringArgumentViewModel();
        StringArguments.Add(arg);
        SelectedStringArgument = arg;
    }

    private void RemoveSelectedStringArgument()
    {
        if (SelectedStringArgument is null)
        {
            return;
        }

        StringArguments.Remove(SelectedStringArgument);
        SelectedStringArgument = StringArguments.LastOrDefault();
    }
}
