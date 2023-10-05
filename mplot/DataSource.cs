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
    public CommandDataSource(List<string> arguments)
    {
        Arguments = arguments;
    }

    public List<string> Arguments { get; }
}