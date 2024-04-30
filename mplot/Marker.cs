namespace mplot;

public class SquareMarker : Marker
{
    public override string ToGnuplot() => "pt 4";
}

public class PlusMarker : Marker
{
    public override string ToGnuplot() => "pt 1";
}

public class FilledCircleMarker : Marker
{
    public override string ToGnuplot() => "pt 7";
}

public abstract class Marker
{
    public abstract string ToGnuplot();
}

public class CircleMarker : Marker
{
    public override string ToGnuplot() => "pt 6";
}

public class FilledSquareMarker : Marker
{
    public override string ToGnuplot() => "pt 5";
}

public class LineMarker : Marker
{
    public override string ToGnuplot()
    {
        return "";
    }
}

public class XMarker : Marker
{
    public override string ToGnuplot()
    {
        return "";
    }
}

public class NoMarker : Marker
{
    public override string ToGnuplot()
    {
        return "";
    }
}
