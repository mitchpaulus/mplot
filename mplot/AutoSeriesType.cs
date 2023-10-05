using System.Text;

namespace mplot;

public static class AutoSeriesType
{
    
    public static SeriesStyle GetStyle(DataSource source, ColumnSpecifier xSpecifier, Separator2 sep)
    {
        using Stream s = source.Match(
            fs => new FileStream(fs.Filepath, FileMode.Open),
            inlineDataSource => (Stream)new MemoryStream(Encoding.UTF8.GetBytes(inlineDataSource.Contents))
        );
        
        // if (sep.)
        

        return SeriesStyle.Scatter;
    }
}