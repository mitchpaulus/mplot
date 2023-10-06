using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
                return (false, string.Empty, $"Starting the process with arguments {string.Join(", ", Arguments)} resulted in null.");

            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            _output = output;
            return (true, output, string.Empty);
        }
        catch (Exception e)
        {
            return (false, string.Empty, $"There was an exception executing the process: {e.Message}");
        }
    }
}
