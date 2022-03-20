using System.Drawing;
using System.Text;

namespace mplot;

internal class MplotListener : MplotBaseListener
{
    MplotQueue<Color> ColorStack = new(Color.Black);

    public string XLabel { get; set; }
    public string YLabel { get; set; }

    public MplotQueue<int> Ycols { get; set; } = new (2);
    public MplotQueue<int> Xcols { get; set; } = new (1);

    public bool Grid = false;

    public string Title { get; set; }

    public int DefaultDpi = 96;
    public int PlotDpi = 384; // 96 * 4
    public int TextDpi = 96;

    public int Skip = 0;

    public Separator Separator = new TabSeparator();

    public MplotQueue<Marker> MarkerStack = new(new CircleMarker());

    public MplotQueue<string> FilePathStack = new("data.tsv");

    public string LastFilePath { get; set; }

    public List<Series> Series = new List<Series>();


    public string ParseXLabelContext(MplotParser.XlabelContext context) => context.STRING().GetText().RemoveQuotes();
    public string ParseYLabelContext(MplotParser.YlabelContext context) => context.STRING().GetText().RemoveQuotes();

    public override void EnterXlabel(MplotParser.XlabelContext context) => XLabel = ParseXLabelContext(context);
    public override void EnterYlabel(MplotParser.YlabelContext context) => YLabel = ParseYLabelContext(context);

    public override void EnterTitles(MplotParser.TitlesContext context)
    {
        Title =  context.STRING()[0].GetText().RemoveQuotes();
        XLabel = context.STRING()[1].GetText().RemoveQuotes();
        YLabel = context.STRING()[2].GetText().RemoveQuotes();
    }

    public override void EnterFile(MplotParser.FileContext context)
    {
        FilePathStack.Queue(context.STRING().Select(node => node.GetText().RemoveQuotes()));
    }

    public override void EnterTitle(MplotParser.TitleContext context)
    {
        Title = context.STRING().GetText().RemoveQuotes();
    }

    public override void EnterCommaSep(MplotParser.CommaSepContext context) => Separator = new CommaSeparator();

    public override void EnterTabSep(MplotParser.TabSepContext context) => Separator = new TabSeparator();
    public override void EnterSeparator(MplotParser.SeparatorContext context)
    {
        string separatorText = context.STRING().GetText().RemoveQuotes();
        Separator = new CharactersSeparator(separatorText);
    }

    public override void EnterLinesMarker(MplotParser.LinesMarkerContext context) => MarkerStack.Queue(new LineMarker());

    public override void EnterPlusMarker(MplotParser.PlusMarkerContext context) => MarkerStack.Queue(new PlusMarker());
    public override void EnterCircleMarker(MplotParser.CircleMarkerContext context) => MarkerStack.Queue(new CircleMarker());

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
    }

    public override void EnterHexColor(MplotParser.HexColorContext context)
    {
        string hex = context.HEXCOLOR().GetText();

        // Get Color from Hex in form #RRGGBB
        Color color = ColorTranslator.FromHtml(hex);
        ColorStack.Queue(color);
    }

    public override void EnterYcol(MplotParser.YcolContext context) => Ycols.Queue(context.INT().Select(node => int.Parse(node.GetText())));

    public override void EnterXcol(MplotParser.XcolContext context) =>  Xcols.Queue(context.INT().Select(node => int.Parse(node.GetText())));

    public override void EnterSkip(MplotParser.SkipContext context)
    {
        Skip = int.Parse(context.INT().GetText());
    }

    public override void EnterGrid(MplotParser.GridContext context) => Grid = true;

    public override void EnterSeries(MplotParser.SeriesContext context)
    {
        var xCol = Xcols.Pop();
        var yCol = Ycols.Pop();

        string name = context.STRING().GetText().RemoveQuotes();
        string filePath = FilePathStack.Pop();

        var marker = MarkerStack.Pop();
        Color lineColor = ColorStack.Pop();

        Series series = new Series(xCol, yCol, name, filePath, marker, Skip, lineColor);

        Series.Add(series);
    }

    public string WriteGnuplot()
    {
        List<string> lines = new();
        lines.Add("set terminal pngcairo\n");

        if (!string.IsNullOrWhiteSpace(Title)) lines.Add($"set title \"{Title}\"");
        if (!string.IsNullOrWhiteSpace(XLabel)) lines.Add($"set xlabel \"{XLabel}\"");
        if (!string.IsNullOrWhiteSpace(YLabel)) lines.Add($"set ylabel \"{YLabel}\"");

        lines.Add(Separator.ToGnuplot());

        if (Grid) lines.Add("set grid");

        if (Series.Any())
        {
            if (Series.Count == 1)
            {
                Series series = Series[0];
                lines.Add($"plot {series.ToGnuplot()}");
            }
            else
            {
                lines.Add("plot \\");
                foreach (IndexedItem<Series> series in Series.WithIndex(1))
                {
                    string end = series.Index == Series.Count ? "" : ", \\";
                    lines.Add( "  " + series.Item.ToGnuplot() + end);
                }
            }
        }

        StringBuilder builder = new();
        foreach (var line in lines) builder.Append(line + "\n");
        return builder.ToString();
    }
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

}


public class CircleMarker : Marker
{




}

public class PlusMarker : Marker { }

public class LineMarker : Marker { }

public class XMarker : Marker { }

public class Series
{
    private readonly int _xCol;
    private readonly int _yCol;
    private readonly string _name;
    private readonly string _filePath;
    private readonly Marker _marker;
    private readonly int _skip;
    private readonly Color _lineColor;

    public Series(int xCol, int yCol, string name, string filePath, Marker marker, int skip, Color lineColor)
    {
        _xCol = xCol;
        _yCol = yCol;
        _name = name;
        _filePath = filePath;
        _marker = marker;
        _skip = skip;
        _lineColor = lineColor;
    }

    public string ToGnuplot()
    {
        string skipText = _skip == 0 ? "" : $" skip {_skip}";

        if (_marker is LineMarker)
        {
            return $"\"{_filePath}\" using {_xCol}:{_yCol}{skipText} with lines linecolor {_lineColor.ToGnuplot()} title \"{_name}\"";
        }
        else
        {
            return $"\"{_filePath}\" using {_xCol}:{_yCol}{skipText} with points linecolor {_lineColor.ToGnuplot()} title \"{_name}\"";
        }
    }
}

public abstract class Separator
{
    public abstract string ToGnuplot();
}

public class TabSeparator : Separator
{
    public override string ToGnuplot()
    {
        return "set datafile separator tab";
    }
}

public class CommaSeparator : Separator
{
    public override string ToGnuplot()
    {
        return "set datafile separator comma";
    }
}

public class CharactersSeparator : Separator
{
    private readonly string _sep;

    public CharactersSeparator(string sep)
    {
        _sep = sep;
    }
    public override string ToGnuplot()
    {
        return $"set datafile separator \"{_sep}\"";
    }
}
