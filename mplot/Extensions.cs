using System.Drawing;
using System.Text;

namespace mplot;

public static class Extensions
{
    public static string RemoveQuotes(this string inputString) => inputString.Substring(1, inputString.Length - 2);
    public static T TryPop<T>(this Stack<T> stack, T defaultVal) => stack.TryPop(out T? found) ? found : defaultVal;

    public static List<IndexedItem<T>> WithIndex<T>(this IEnumerable<T> list, int init = 0) =>
        list.Select((arg1, i) => new IndexedItem<T>(i + init, arg1)).ToList();

    public static string ToGnuplot(this Color color)
    {
        // Get r in Hex format (FF)
        var r = color.R.ToString("X2");
        // Get g in Hex format (FF)
        var g = color.G.ToString("X2");
        // Get b in Hex format (FF)
        var b = color.B.ToString("X2");

        return $"\"0x{r}{g}{b}\"";
    }

    public static IEnumerable<string> SplitLines(this string input)
    {
        using StringReader sr = new(input);
        while (sr.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    public static string EscapeShell(this string input)
    {
        // Special characters are: \ " `
        bool needsQuotes = false;

        StringBuilder sb = new(input.Length + 10);
        //Space ( ): Used as a delimiter between commands and arguments.
        // Tab (\t): Used as a delimiter, similar to space.
        // Newline (\n): Indicates the end of a command or input.
        // Ampersand (&): Used for running commands in the background.
        // Semicolon (;): Used to separate multiple commands on a single line.
        // Pipe (|): Used for command chaining and redirecting output.
        // Less Than (<) and Greater Than (>): Used for input and output redirection.
        // Question Mark (?): Used in wildcard patterns for matching a single character.
        // Asterisk (*): Used in wildcard patterns for matching zero or more characters.
        // Square Brackets ([ and ]): Used for grouping characters in wildcard patterns.
        // Parentheses (( and )): Used for command grouping and arithmetic evaluation.
        // Dollar Sign ($): Used for variable expansion and command substitution.
        // Backslash (): Used to escape special characters.
        // Single Quote ('): Used to preserve the literal value of each character within the quotes.
        // Double Quote ("): Used to allow for variable expansion and interpretation of some special characters.

        List<char> specialChars = new() { ' ', '\t', '\n', '&', ';', '|', '<', '>', '?', '*', '[', ']', '(', ')', '$', '\\', '\'', '"' };

        foreach (var c in input)
        {
            if (specialChars.Contains(c)) needsQuotes = true;

            if (c == '\'')
            {
                sb.Append("\"'\"");
            }
            else
            {
                sb.Append(c);
            }
        }

        if (needsQuotes)
        {
            sb.Insert(0, '\'');
            sb.Append('\'');
        }
        return sb.ToString();
    }

    public static string ToGnuPlotString(this string input)
    {
        // Add quotes around string, escaping interior quotes, and interior backslashes
        StringBuilder sb = new(input.Length + 10);
        sb.Append('"');
        foreach (var c in input)
        {
            if (c == '"' || c == '\\')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }

    public static IEnumerable<string> SplitLines(this TextReader input)
    {
        while (input.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    public static void Update(this MplotArray<int> intArray, MplotParser.IntArrayContext intArrayContext)
    {
        intArray.Update(intArrayContext.INT().Select(node => int.Parse(node.GetText())).ToList(),  intArrayContext.CYCLE().Any(), intArrayContext.ASTERISK().Any()  );
    }

    public static void Update(this MplotArray<string> stringArray, MplotParser.StringArrayContext stringArrayContext)
    {
        stringArray.Update(stringArrayContext.STRING().Select(node =>  node.GetText().RemoveQuotes().Trim()).ToList(),  stringArrayContext.CYCLE().Any(), stringArrayContext.ASTERISK().Any());
    }
}
