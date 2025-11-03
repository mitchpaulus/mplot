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
