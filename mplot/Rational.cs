namespace mplot;

public class Rational
{
    public int Numerator { get; }
    public int Denominator { get; }

    public Rational(MplotParser.RationalContext rationalContext)
    {
        if (rationalContext is MplotParser.FractionContext fraction)
        {
            Numerator = int.Parse(fraction.numerator.Text);
            Denominator = int.Parse(fraction.denominator.Text);
        }
        else if (rationalContext is MplotParser.MixedFractionContext mixedFraction)
        {
            Numerator = int.Parse(mixedFraction.denominator.Text) * int.Parse(mixedFraction.integer.Text) + int.Parse(mixedFraction.numerator.Text);
            Denominator = int.Parse(mixedFraction.denominator.Text);
        }
        else if (rationalContext is MplotParser.IntegerContext integerContext)
        {
            Numerator = int.Parse(integerContext.GetText());
            Denominator = 1;
        }
        else
        {
            throw new NotSupportedException("Unhandled rational context");
        }
    }

    public Rational(int numerator, int denominator)
    {
        // Store the rational in canonical form where denominator is positive
        // TODO: Use GCD.
        if (denominator < 0)
        {
            Numerator = -numerator;
            Denominator = -denominator;
        }
        else
        {
            Numerator = numerator;
            Denominator = denominator;
        }
    }

    public Rational(int numerator)
    {
        Numerator = numerator;
        Denominator = 1;
    }

    public override string ToString()
    {
        return $"{Numerator}/{Denominator}";
    }

    // Implement addition
    public static Rational operator +(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator + r2.Numerator * r1.Denominator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator+(Rational r1, int r2) => r1 + new Rational(r2);

    public static Rational operator+(int r1, Rational r2) => new Rational(r1) + r2;

    // Implement subtraction
    public static Rational operator -(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator - r2.Numerator * r1.Denominator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator-(Rational r1, int r2) => r1 - new Rational(r2);

    public static Rational operator-(int r1, Rational r2) => new Rational(r1) - r2;

    // Implement multiplication
    public static Rational operator *(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Numerator;
        int denominator = r1.Denominator * r2.Denominator;
        return new Rational(numerator, denominator);
    }

    public static Rational operator*(Rational r1, int r2) => r1 * new Rational(r2);

    public static Rational operator*(int r1, Rational r2) => new Rational(r1) * r2;

    // Implement division
    public static Rational operator /(Rational r1, Rational r2)
    {
        int numerator = r1.Numerator * r2.Denominator;
        int denominator = r1.Denominator * r2.Numerator;
        // If denominator is zero, throw DivideByZeroException
        if (denominator == 0) throw new DivideByZeroException();
        return new Rational(numerator, denominator);
    }

    public static Rational operator/(Rational r1, int r2)
    {
        return r1 / new Rational(r2);
    }

    public static Rational operator/(int r1, Rational r2)
    {
        return new Rational(r1) / r2;
    }

    // Implement equality and inequality
    public static bool operator ==(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator == r2.Numerator * r1.Denominator;
    }

    public static bool operator !=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator != r2.Numerator * r1.Denominator;
    }

    public static bool operator ==(Rational r1, int r2)
    {
        return r1 == new Rational(r2);
    }

    public static bool operator !=(Rational r1, int r2)
    {
        return r1 != new Rational(r2);
    }

    public static bool operator ==(int r1, Rational r2)
    {
        return new Rational(r1) == r2;
    }

    public static bool operator !=(int r1, Rational r2)
    {
        return new Rational(r1) != r2;
    }

    protected bool Equals(Rational other) => Numerator * other.Denominator == other.Numerator * Denominator;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Rational) obj);
    }

    // Implement Object.GetHashCode
    public override int GetHashCode()
    {
        return Numerator.GetHashCode() ^ Denominator.GetHashCode();
    }

    // Implement greater than and less than
    public static bool operator >(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator > r2.Numerator * r1.Denominator;
    }

    public static bool operator <(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator < r2.Numerator * r1.Denominator;
    }

    public static bool operator >(Rational r1, int r2)
    {
        return r1 > new Rational(r2);
    }

    public static bool operator <(Rational r1, int r2)
    {
        return r1 < new Rational(r2);
    }

    public static bool operator >(int r1, Rational r2)
    {
        return new Rational(r1) > r2;
    }

    public static bool operator <(int r1, Rational r2)
    {
        return new Rational(r1) < r2;
    }

    // Implement greater than or equal to and less than or equal to
    public static bool operator >=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator >= r2.Numerator * r1.Denominator;
    }

    public static bool operator <=(Rational r1, Rational r2)
    {
        return r1.Numerator * r2.Denominator <= r2.Numerator * r1.Denominator;
    }

    public static bool operator >=(Rational r1, int r2)
    {
        return r1 >= new Rational(r2);
    }

    public static bool operator <=(Rational r1, int r2)
    {
        return r1 <= new Rational(r2);
    }

    public static bool operator >=(int r1, Rational r2)
    {
        return new Rational(r1) >= r2;
    }

    public static bool operator <=(int r1, Rational r2)
    {
        return new Rational(r1) <= r2;
    }

    // Implement implicit conversion from int to Rational
    public static implicit operator Rational(int i)
    {
        return new Rational(i);
    }

    // Implement implicit conversion from Rational to double
    public static implicit operator double(Rational r)
    {
        return (double)r.Numerator / r.Denominator;
    }

    public bool IsPositive()
    {

        if (Numerator >= 0 && Denominator > 0)
        {
            return true;
        }

        if (Numerator <= 0 && Denominator < 0)
        {
            return true;
        }

        return false;
    }

    public int Floor()
    {
        return IsPositive() ? Numerator / Denominator : (Numerator / Denominator) - 1;
    }

    public int Round() => (this + new Rational(1, 2)).Floor();

    public string ToDecimalString()
    {
        if (Numerator % Denominator == 0) return (Numerator / Denominator).ToString();
        double value = (double)Numerator / Denominator;
        return value.ToString("f3");
    }
}