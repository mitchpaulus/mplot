namespace MplotLib;

public interface MPlotString
{
    public string GetString(List<string> arguments);
}

public class SimpleString(string s) : MPlotString
{
    public string GetString(List<string> arguments) => s;
}

public class ArgString(int argNum, string? defaultStr) : MPlotString
{
    public string GetString(List<string> arguments)
    {
        if (argNum - 1 < arguments.Count) return arguments[argNum - 1];
        if (defaultStr is not null) return defaultStr;

        throw new Exception($"Argument not provided for sarg{argNum}");
    }
}