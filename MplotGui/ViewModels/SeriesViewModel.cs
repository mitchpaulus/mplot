using MplotLib;

namespace MplotGui.ViewModels;

public enum SeriesStyleOption
{
    Scatter,
    Line,
    Boxes,
    FStep
}

public enum MarkerOption
{
    Auto,
    None,
    Circle,
    FilledCircle,
    Square,
    FilledSquare,
    Plus,
    X,
    Line
}

public enum SeparatorOption
{
    Auto,
    Comma,
    Tab,
    Whitespace
}

public enum DashTypeOption
{
    Solid,
    Simple
}

public enum YAxisSideOption
{
    Left,
    Right
}

public enum DataSourceOption
{
    File,
    Inline
}

public class SeriesViewModel : ViewModelBase
{
    private string _seriesName = "Series 1";
    private string _filePath = string.Empty;
    private string _inlineData = string.Empty;
    private DataSourceOption _dataSourceOption = DataSourceOption.File;
    private string _xColumn = "1";
    private string _yColumn = "2";
    private string _lineColorHex = "#1f77b4";
    private int _lineWidth = 2;
    private SeriesStyleOption _seriesStyle = SeriesStyleOption.Scatter;
    private MarkerOption _marker = MarkerOption.Circle;
    private SeparatorOption _separator = SeparatorOption.Auto;
    private DashTypeOption _dashType = DashTypeOption.Solid;
    private YAxisSideOption _yAxisSide = YAxisSideOption.Left;
    private int _skip = 0;
    private string _ignoreExpression = string.Empty;

    public string SeriesName
    {
        get => _seriesName;
        set => SetProperty(ref _seriesName, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public string InlineData
    {
        get => _inlineData;
        set => SetProperty(ref _inlineData, value);
    }

    public DataSourceOption DataSource
    {
        get => _dataSourceOption;
        set => SetProperty(ref _dataSourceOption, value);
    }

    public string XColumn
    {
        get => _xColumn;
        set => SetProperty(ref _xColumn, value);
    }

    public string YColumn
    {
        get => _yColumn;
        set => SetProperty(ref _yColumn, value);
    }

    public string LineColorHex
    {
        get => _lineColorHex;
        set => SetProperty(ref _lineColorHex, value);
    }

    public int LineWidth
    {
        get => _lineWidth;
        set => SetProperty(ref _lineWidth, value);
    }

    public SeriesStyleOption SeriesStyle
    {
        get => _seriesStyle;
        set => SetProperty(ref _seriesStyle, value);
    }

    public MarkerOption Marker
    {
        get => _marker;
        set => SetProperty(ref _marker, value);
    }

    public SeparatorOption Separator
    {
        get => _separator;
        set => SetProperty(ref _separator, value);
    }

    public DashTypeOption DashType
    {
        get => _dashType;
        set => SetProperty(ref _dashType, value);
    }

    public YAxisSideOption YAxisSide
    {
        get => _yAxisSide;
        set => SetProperty(ref _yAxisSide, value);
    }

    public int Skip
    {
        get => _skip;
        set => SetProperty(ref _skip, value);
    }

    public string IgnoreExpression
    {
        get => _ignoreExpression;
        set => SetProperty(ref _ignoreExpression, value);
    }
}
