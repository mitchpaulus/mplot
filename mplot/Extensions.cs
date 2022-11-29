using System.Drawing;

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
}
