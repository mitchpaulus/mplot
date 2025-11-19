using System.Collections.Generic;

namespace MplotLib;

public abstract class DashType
{
    public abstract string ToGnuplot();
}

public class SolidDash : DashType
{
    public override string ToGnuplot() => "dt 1";
}

public class SimpleDash : DashType
{
    public override string ToGnuplot() => "dt 2";
}

public class CustomDash : DashType
{
    private static readonly HashSet<char> AllowedCharacters = new() { '.', '-', ' ', '_' };
    private readonly string _pattern;

    public CustomDash(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        foreach (char c in pattern)
        {
            if (!AllowedCharacters.Contains(c))
            {
                throw new ArgumentException("Custom dashtype strings may only contain '.', '-', '_' or space characters.", nameof(pattern));
            }
        }

        _pattern = pattern;
    }

    public override string ToGnuplot() => $@"dashtype ""{_pattern}""";
}
