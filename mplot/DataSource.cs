using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OfficeOpenXml;
using OneOf;

namespace mplot;

public class DataSource : OneOfBase<FileSource, InlineDataSource, CommandDataSource>
{
    protected DataSource(OneOf<FileSource, InlineDataSource, CommandDataSource> input) : base(input)
    {
    }

    public static implicit operator DataSource(FileSource fileSource) => new(fileSource);
    public static implicit operator DataSource(InlineDataSource inlineDataSource) => new(inlineDataSource);
    public static implicit operator DataSource(CommandDataSource commandDataSource) => new(commandDataSource);

    public bool IsFileSource => IsT0;
    public bool IsInlindeDataSource => IsT1;
    public bool IsCmdDataSource => IsT2;

    public Stream Stream()
    {
        Stream s = Match(
            fs => new FileStream(fs.Filepath, FileMode.Open),
            inlineDataSource => (Stream) new MemoryStream(Encoding.UTF8.GetBytes(inlineDataSource.Contents)),
            cmdDataSource => new MemoryStream(Encoding.UTF8.GetBytes(cmdDataSource.Execute().Output))
        );
        return s;
    }
}

public class FileSource
{
    public FileSource(string filepath)
    {
        Filepath = filepath;
    }

    public string Filepath { get; private set; }

    public override string ToString() => Filepath;
}

public class InlineDataSource
{
    public string VariableName { get; }
    public string Contents { get; private set; }

    public InlineDataSource(string contents, string variableName)
    {
        VariableName = variableName;
        Contents = contents;
    }
}

public class CommandDataSource
{
    private readonly int _id;
    private string? _output = null;

    public CommandDataSource(List<string> arguments, int id)
    {
        _id = id;
        Arguments = arguments;
    }

    public List<string> Arguments { get; }

    public string TmpFilePath()
    {
        // If on Unix, use /tmp/mplot_{id}.txt. On Windows use: %LOCALAPPDATA%\mplot\tmp\{id}.txt.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var tmpDir = $"{localAppData}\\mplot\\tmp";
            Directory.CreateDirectory(tmpDir);
            return $"{tmpDir}\\mplot_{_id}.txt";
        }

        return $"/tmp/mplot_{_id}.txt";
    }

    public string ToGnuPlotString()
    {
        var shellString =  "< " + string.Join(" ", Arguments.Select(s => s.EscapeShell()));
        return shellString.ToGnuPlotString();
    }

    public (bool Success, string Output, string ErrorMsg) Execute()
    {
        if (_output != null) return (true, _output, string.Empty);

        if (!Arguments.Any()) return (false, string.Empty, "No arguments were found for command data source.");

        var psi = new ProcessStartInfo
        {
            FileName = Arguments[0],
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in Arguments.Skip(1)) psi.ArgumentList.Add(arg);

        try
        {
            var process = Process.Start(psi);
            if (process == null)
            {
                return (false, string.Empty, $"Starting the process with arguments {string.Join(", ", Arguments)} resulted in null.");
            }

            // Read MUST happen before wait, otherwise deadlock.
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            _output = output;
            return (true, output, string.Empty);
        }
        catch (Exception e)
        {
            return (false, string.Empty, $"There was an exception executing the process: {e.Message}");
        }
    }
}

public class ExcelFileDataSource
{
    public ExcelFileDataSource(string filePath, string worksheet, string cell)
    {
        FilePath = filePath;
        Worksheet = worksheet;
        Cell = cell;
    }

    public string FilePath { get; set; }
    public string Worksheet { get; set; }
    public string Cell { get; set; }


    public Stream GetTsvStream()
    {
        using ExcelPackage package = new ExcelPackage(FilePath);
        var workbook = package.Workbook;

        foreach (var sheet in workbook.Worksheets)
        {
            if (sheet.Name != Worksheet) continue;

            var upperLeftRange = sheet.Cells[Cell];
            // Read left to right until blank on first row, expect this to be header, or at least set number of cols.

            var startCol = upperLeftRange.Start.Column;
            var endCol = upperLeftRange.Start.Column;
            var firstRow = upperLeftRange.Start.Row;

            var currentRange = upperLeftRange;

            while (!string.IsNullOrWhiteSpace(currentRange.Text))
            {
                endCol++;
                currentRange = sheet.Cells[firstRow, endCol];
            }

            StringBuilder b = new StringBuilder();
            var currentRow = firstRow;

            bool validRow = false;
            List<string> fields = new();
            do
            {
                validRow = false;
                for (int c = startCol; c <= endCol; c++)
                {
                    string s = sheet.Cells[currentRow, c].Text;
                    if (!string.IsNullOrWhiteSpace(s)) validRow = true;
                    fields.Add(s);
                }

                if (!validRow) continue;

                b.Append(string.Join("\t", fields));
                b.Append('\n');
                fields.Clear();

            } while (validRow);

            string content = b.ToString();
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        return new MemoryStream();
    }
}

public interface IDataSource
{
    /// <summary>
    /// Get the data related to the one-based column
    /// </summary>
    public List<string> Get(int colOneBased);
    public List<string> Get(string name);
}

public class TestDataSource : IDataSource
{
    public List<string> Get(int colOneBased)
    {
        return new List<int>() { 1, 2, 3 }.Select(i => (i * colOneBased).ToString()).ToList();
    }

    public List<string> Get(string name)
    {
        return new List<int>() { 1, 2, 3 }.Select(i => (i * name.Length).ToString()).ToList();
    }
}

public class SimpleDelimitedTextDataSource : IDataSource
{
    private readonly string _contents;
    private readonly int _skip;
    private readonly Separator _separator;

    public SimpleDelimitedTextDataSource(string contents, int skip, Separator separator)
    {
        _contents = contents;
        _skip = skip;
        _separator = separator;
    }

    /// <summary>
    /// Get data for one-based column
    /// </summary>
    public List<string> Get(int colOneBased)
    {
        var lines = _contents.SplitLines();
        List<string> result = new();
        foreach (var line in lines.Skip(_skip))
        {
            var fields = _separator.Split(line);
            if (fields.Count < colOneBased)
            {
                throw new Exception($"Column {colOneBased} does not exist in the data source.");
            }
            result.Add(fields[colOneBased - 1]);
        }
        return result;
    }

    public List<string> Get(string name)
    {
        List<string> lines = _contents.SplitLines().ToList();
        List<string> result = new();
        if (!lines.Any()) return result;
        List<string> header = _separator.Split(lines[0]);
        int colOneBased = -1;
        for (int i = 0; i < header.Count; i++)
        {
            if (header[i] == name)
            {
                colOneBased = i + 1;
                break;
            }
        }

        if (colOneBased == -1)
        {
            throw new Exception($"Column '{name}' does not exist in the data source.");
        }

        foreach (var line in lines.Skip(_skip))
        {
            var fields = _separator.Split(line);
            if (fields.Count < colOneBased)
            {
                throw new Exception($"Column {colOneBased} does not exist in the data source.");
            }
            result.Add(fields[colOneBased - 1]);
        }
        return result;
    }
}
