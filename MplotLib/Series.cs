using System.Drawing;
using System.Text;

namespace MplotLib;

public class Series
{
    private readonly string _xCol;
    private readonly string _yCol;
    public readonly string Name;
    public DataSource FilePath { get; }
    private readonly Marker _marker;
    private readonly int _skip;
    private readonly Color _lineColor;
    private readonly int _lineWidth;
    private readonly SeriesStyle _style;
    private readonly DashType _dashType;
    public Separator2 Separator { get; }
    private YAxisSide _yAxisSide { get; }

    public Series(string xCol, string yCol, string name, DataSource filePath, Marker marker, int skip, Color lineColor, int lineWidth, SeriesStyle style, Separator2 separator, DashType dashType, YAxisSide yAxisSide)
    {
        _xCol = xCol;
        _yCol = yCol;
        Name = name;
        FilePath = filePath;
        _marker = marker;
        _skip = skip;
        _lineColor = lineColor;
        _lineWidth = lineWidth;
        _style = style;
        _dashType = dashType;
        Separator = separator;
        _yAxisSide = yAxisSide;
    }

    public string ToGnuplot()
    {
        StringBuilder b = new();

        string skipText = _skip == 0 ? "" : $" skip {_skip}";
        string title = string.IsNullOrWhiteSpace(Name) ? "" : $" title \"{Name}\"";

        string source = FilePath.Match(
            fileSource => $"\"{fileSource.Filepath}\"",
            inlineDataSource => "$" + inlineDataSource.VariableName,
            cmdDataSource => cmdDataSource.ToGnuPlotString(),
            _ => "'-'"
        );

        b.Append(source);
        b.Append($" using {_xCol}:{_yCol}{skipText} axes {_yAxisSide.ToGnuplot()} with ");


        string gnuplotStyle;
        if (_style.Id == SeriesStyle.Line.Id && _marker is not NoMarker)
        {
            gnuplotStyle = "linespoints";
        }
        else if (_style.IsScatter)
        {
            gnuplotStyle = "points";
        }
        else
        {
            gnuplotStyle = _style.GnuplotStyle;
        }

        b.Append(gnuplotStyle);

        if (gnuplotStyle != "boxes" && gnuplotStyle != "fstep" && _marker is not NoMarker)
        {
            b.Append($" {_marker.ToGnuplot()}");
        }

        if (gnuplotStyle == "boxes")
        {
            b.Append(" fs solid border -1");
        }

        if (_lineWidth != 1) b.Append($" lw {_lineWidth}");

        if (_dashType is not SolidDash) b.Append($" {_dashType.ToGnuplot()}");

        b.Append($" linecolor {_lineColor.ToGnuplot()}");
        if (!string.IsNullOrWhiteSpace(title)) b.Append($" {title}");

        return b.ToString();
    }
}
