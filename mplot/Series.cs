using System.Drawing;
using System.Text;

namespace mplot;

public class Series
{
    private readonly string _xCol;
    private readonly string _yCol;
    private readonly string _name;
    public DataSource FilePath { get; }
    private readonly Marker _marker;
    private readonly int _skip;
    private readonly Color _lineColor;
    private readonly int _lineWidth;
    private readonly SeriesStyle _style;
    public Separator2 Separator { get; }

    public Series(string xCol, string yCol, string name, DataSource filePath, Marker marker, int skip, Color lineColor, int lineWidth, SeriesStyle style, Separator2 separator)
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
        StringBuilder b = new();


        string skipText = _skip == 0 ? "" : $" skip {_skip}";
        string lineWidth = _lineWidth == 1 ? "" : $"lw {_lineWidth}";
        string title = string.IsNullOrWhiteSpace(_name) ? "" : $" title \"{_name}\"";

        string source = FilePath.Match(
            fileSource => $"\"{fileSource.Filepath}\"",
            inlineDataSource => "$" + inlineDataSource.VariableName,
            cmdDataSource => cmdDataSource.ToGnuPlotString()
        );

        b.Append(source);
        b.Append($" using {_xCol}:{_yCol}{skipText} with ");

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

        if (gnuplotStyle != "boxes" && _marker is not NoMarker)
        {
            b.Append($" {_marker.ToGnuplot()}");
        }

        if (gnuplotStyle == "boxes")
        {
            b.Append(" fs solid");
        }

        if (_lineWidth != 1) b.Append($" lw {_lineWidth}");

        b.Append($" linecolor {_lineColor.ToGnuplot()}");
        if (!string.IsNullOrWhiteSpace(title)) b.Append($" {title}");

        return b.ToString();
    }
}
