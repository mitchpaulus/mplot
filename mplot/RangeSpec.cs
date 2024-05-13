using OneOf;

namespace mplot;

public class RangeSpec : OneOfBase<Rational, AutoRange, DateTime>
{
    protected RangeSpec(OneOf<Rational, AutoRange, DateTime> input) : base(input)
    {
    }

    public static implicit operator RangeSpec(AutoRange r) => new(r);
    public static implicit operator RangeSpec(Rational r) => new(r);
    public static implicit operator RangeSpec(DateTime r) => new(r);

    public bool IsAuto => IsT1;

    public string ToGnuplot()
    {
        return Match(

            rational => rational.ToDecimalString(),
            _ => "*",
            time => $"\"{time:yyyy-MM-dd HH:mm}\""
        );
    }
}

public sealed class AutoRange
{
    private static readonly AutoRange Instance = new();

    private AutoRange() { }

    public static AutoRange Auto()
    {
        return Instance;
    }
}
