using System.Text;

namespace MplotLib;

/// <summary>
/// Date/time format strings without a '%' use a letter syntax similar to .NET/Excel:
///
///   yy yyyy        2 or 4 digit year
///   m mm mmm mmmm  month: 1, 01, Jan, January
///   d dd ddd dddd  day: 5, 05, Mon, Monday
///   h hh           24 hour: 9, 09
///   c cc           12 hour (clock): 9, 09
///   n nn           minute, always 2 digits
///   s ss           second, always 2 digits
///   ampm am/pm     am/pm marker (gnuplot always writes it lowercase)
///
/// Letters are case-insensitive. Any other character is copied as is. A backslash
/// escapes the next character, and text in single quotes is copied as is.
/// Strings containing a '%' are passed to gnuplot unchanged.
/// </summary>
public class MplotTimeFormat
{
    public List<TimeFormatPart> Parts { get; }
    public List<string> Warnings { get; } = new();

    private MplotTimeFormat(List<TimeFormatPart> parts)
    {
        Parts = parts;
    }

    public static bool IsGnuplotFormat(string format) => format.Contains('%');

    public static MplotTimeFormat Parse(string format)
    {
        List<TimeFormatPart> parts = new();
        StringBuilder literal = new();

        void FlushLiteral()
        {
            if (literal.Length == 0) return;
            parts.Add(new TimeFormatLiteral(literal.ToString()));
            literal.Clear();
        }

        int i = 0;
        while (i < format.Length)
        {
            char c = format[i];

            if (c == '\\')
            {
                if (i + 1 < format.Length) literal.Append(format[i + 1]);
                i += 2;
                continue;
            }

            if (c == '\'')
            {
                int end = format.IndexOf('\'', i + 1);
                if (end < 0) throw new InvalidDataException($"Unterminated single quote in time format \"{format}\".");
                literal.Append(format, i + 1, end - i - 1);
                i = end + 1;
                continue;
            }

            string? ampm = MatchAmPm(format, i);
            if (ampm is not null)
            {
                FlushLiteral();
                parts.Add(new TimeFormatToken(TimeTokenKind.AmPm, 1));
                i += ampm.Length;
                continue;
            }

            char lower = char.ToLowerInvariant(c);
            if ("ymdhcns".Contains(lower))
            {
                int count = 1;
                while (i + count < format.Length && char.ToLowerInvariant(format[i + count]) == lower) count++;

                FlushLiteral();
                parts.Add(MakeToken(lower, count, format));
                i += count;
                continue;
            }

            literal.Append(c);
            i++;
        }

        FlushLiteral();

        MplotTimeFormat timeFormat = new(parts);
        timeFormat.CheckForMinuteMistakes(format);
        return timeFormat;
    }

    private static string? MatchAmPm(string format, int index)
    {
        foreach (var candidate in new[] { "am/pm", "ampm" })
        {
            if (string.Compare(format, index, candidate, 0, candidate.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return format.Substring(index, candidate.Length);
            }
        }

        return null;
    }

    private static TimeFormatToken MakeToken(char letter, int count, string format)
    {
        (TimeTokenKind kind, int maxCount, string help) = letter switch
        {
            'y' => (TimeTokenKind.Year, 4, "use yy or yyyy"),
            'm' => (TimeTokenKind.Month, 4, "use m, mm, mmm, or mmmm"),
            'd' => (TimeTokenKind.Day, 4, "use d, dd, ddd, or dddd"),
            'h' => (TimeTokenKind.Hour24, 2, "use h or hh"),
            'c' => (TimeTokenKind.Hour12, 2, "use c or cc"),
            'n' => (TimeTokenKind.Minute, 2, "use n or nn"),
            's' => (TimeTokenKind.Second, 2, "use s or ss"),
            _ => throw new NotImplementedException()
        };

        if (count > maxCount || (kind == TimeTokenKind.Year && count != 2 && count != 4))
        {
            throw new InvalidDataException(
                $"'{new string(letter, count)}' is not valid in time format \"{format}\"; {help}. Use a backslash or single quotes for literal text.");
        }

        return new TimeFormatToken(kind, count);
    }

    /// <summary>
    /// In .NET formats, 'mm' is minutes. Here it is month, so warn on the common 'hh:mm' mistake.
    /// </summary>
    private void CheckForMinuteMistakes(string format)
    {
        for (int i = 2; i < Parts.Count; i++)
        {
            if (Parts[i] is TimeFormatToken { Kind: TimeTokenKind.Month, Count: <= 2 } &&
                Parts[i - 1] is TimeFormatLiteral { Text: ":" } &&
                Parts[i - 2] is TimeFormatToken { Kind: TimeTokenKind.Hour24 or TimeTokenKind.Hour12 })
            {
                Warnings.Add($"Warning: in time format \"{format}\", 'm' after an hour and ':' is month. Use 'n' or 'nn' for minutes.");
                return;
            }
        }
    }

    /// <summary>
    /// Format for reading data with gnuplot's strptime/timecolumn. Reading does not care about zero padding.
    /// </summary>
    public string ToGnuplotInputFormat()
    {
        StringBuilder b = new();
        foreach (var part in Parts)
        {
            b.Append(part switch
            {
                TimeFormatLiteral literal => literal.Text,
                TimeFormatToken token => token.GnuplotInputSpecifier(),
                _ => throw new NotImplementedException()
            });
        }

        return b.ToString();
    }

    /// <summary>
    /// Format for gnuplot's 'set format', where a width of 1 (%1d) writes a number without zero padding.
    /// </summary>
    public string ToGnuplotOutputFormat()
    {
        StringBuilder b = new();
        foreach (var part in Parts)
        {
            b.Append(part switch
            {
                TimeFormatLiteral literal => literal.Text,
                TimeFormatToken token => token.GnuplotOutputSpecifier(),
                _ => throw new NotImplementedException()
            });
        }

        return b.ToString();
    }
}

public enum TimeTokenKind
{
    Year,
    Month,
    Day,
    Hour24,
    Hour12,
    Minute,
    Second,
    AmPm,
}

public abstract record TimeFormatPart;

public record TimeFormatLiteral(string Text) : TimeFormatPart;

public record TimeFormatToken(TimeTokenKind Kind, int Count) : TimeFormatPart
{
    /// <summary>
    /// Reading ignores zero padding, and gnuplot can't read %I. %H with %p reads 12 hour times correctly.
    /// </summary>
    public string GnuplotInputSpecifier() => Kind == TimeTokenKind.Hour12 ? "%H" : GnuplotSpecifier();

    public string GnuplotOutputSpecifier()
    {
        bool unpadded = Count == 1 && Kind is TimeTokenKind.Month or TimeTokenKind.Day or TimeTokenKind.Hour24 or TimeTokenKind.Hour12;
        return unpadded ? GnuplotSpecifier().Insert(1, "1") : GnuplotSpecifier();
    }

    private string GnuplotSpecifier() => (Kind, Count) switch
    {
        (TimeTokenKind.Year, 2) => "%y",
        (TimeTokenKind.Year, _) => "%Y",
        (TimeTokenKind.Month, 3) => "%b",
        (TimeTokenKind.Month, 4) => "%B",
        (TimeTokenKind.Month, _) => "%m",
        (TimeTokenKind.Day, 3) => "%a",
        (TimeTokenKind.Day, 4) => "%A",
        (TimeTokenKind.Day, _) => "%d",
        (TimeTokenKind.Hour24, _) => "%H",
        (TimeTokenKind.Hour12, _) => "%I",
        (TimeTokenKind.Minute, _) => "%M",
        (TimeTokenKind.Second, _) => "%S",
        (TimeTokenKind.AmPm, _) => "%p",
        _ => throw new NotImplementedException()
    };
}
