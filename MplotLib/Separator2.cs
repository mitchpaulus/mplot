using OneOf;

namespace MplotLib;

public class Separator2 : OneOfBase<AutoSeparator, Separator>
{
    public Separator2(OneOf<AutoSeparator, Separator> input) : base(input)
    {
    }

    public static implicit operator Separator2(AutoSeparator _) => new(_);
    public static implicit operator Separator2(TabSeparator _) => new(_);
    public static implicit operator Separator2(CommaSeparator _) => new(_);
    public static implicit operator Separator2(CharactersSeparator _) => new(_);
    public static implicit operator Separator2(WhiteSpaceSeparator _) => new(_);

    public bool IsAuto => IsT0;
    public bool IsKnownSep => IsT1;

    public Separator Determine(DataSource source)
    {
        return TryPickT0(out AutoSeparator autoSep, out var rest) ? autoSep.Determine(source) : rest;
    }
}

