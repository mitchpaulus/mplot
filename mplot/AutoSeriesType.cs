using System.Text;
using OneOf;

namespace mplot;

public static class AutoSeriesType
{
    
    public static SeriesStyle GetStyle(DataSource source, ColumnSpecifier xSpecifier, Separator2 sep)
    {
        using Stream s = source.Match(
            fs => new FileStream(fs.Filepath, FileMode.Open),
            inlineDataSource => (Stream)new MemoryStream(Encoding.UTF8.GetBytes(inlineDataSource.Contents)),
            cmdDataSource => new MemoryStream(Encoding.UTF8.GetBytes(cmdDataSource.Execute().Output))
        );
        
        // if (sep.)
        
        return SeriesStyle.Scatter;
    }

    public static List<Point> GetData(DataSource source, ColumnSpecifier xSpecifier, ColumnSpecifier ySpecifier, Separator2 sep, int skip = 0)
    {
        using Stream s = source.Match(
            fs => new FileStream(fs.Filepath, FileMode.Open),
            inlineDataSource => (Stream)new MemoryStream(Encoding.UTF8.GetBytes(inlineDataSource.Contents)),
            cmdDataSource => new MemoryStream(Encoding.UTF8.GetBytes(cmdDataSource.Execute().Output))
        );
        
        using StreamReader reader = new StreamReader(s, Encoding.UTF8);

        var knownSep = sep.Determine(source);

        int xCol = -1;
        int yCol = -1;
        // Has header
        if (xSpecifier.UseString || ySpecifier.UseString)
        {
            var headerLine = reader.ReadLine();
            if (headerLine is null) return new List<Point>();
            var fields = knownSep.Split(headerLine);
            xCol = xSpecifier.ColMatchZeroBased(fields);
            yCol = ySpecifier.ColMatchZeroBased(fields);

            if (xCol < -1 || yCol < -1)
            {
                // TODO: Improve this message
                throw new InvalidDataException("Could not find matching column");
            }

            List<Point> points = new();
            while (reader.ReadLine() is { } line)
            {
                fields = knownSep.Split(line);
                Point p = new Point(fields[xCol], fields[yCol]);
                points.Add(p);
            }

            return points;
        }
        else // No header, or rely on current skip value.
        {
            xCol = xSpecifier.OneBasedColumnIndex - 1;
            yCol = ySpecifier.OneBasedColumnIndex - 1;
            List<Point> points = new();
            foreach (var line in reader.SplitLines().Skip(skip))
            {
                var fields = knownSep.Split(line);
                Point p = new Point(fields[xCol], fields[yCol]);
                points.Add(p);
            }

            return points;
        }
    }

    public class Point
    {
        public string X { get; }
        public string Y { get; }
        
        public Point(string x, string y)
        {
            X = x;
            Y = y;
        }
    }
}