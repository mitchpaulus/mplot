using Antlr4.Runtime.Tree;

namespace mplot;

public class MplotArray<T> : IMplotArray
{
    public string Name() => _name;
    private readonly List<T>? _defaultElements;
    private readonly MplotArrayMode _defaultMode = MplotArrayMode.Repeat;

    public MplotArrayMode Mode()
    {
        return UseDefault ? _defaultMode : _mode;
    }

    private MplotArrayMode _mode = MplotArrayMode.Normal;

    public void SetMode(MplotArrayMode mode) => _mode = mode;
    private readonly List<T> _elements = new();
    public bool UseDefault { get; private set; }
    private readonly string _name;

    public int Length => _elements.Count;

    public MplotArray(string name)
    {
        _name = name;
        UseDefault = false;
    }

    public MplotArray(List<T> elements, MplotArrayMode mode, string name)
    {
        _defaultElements = elements;
        _defaultMode = mode;
        _name = name;
        UseDefault = true;
    }

    public MplotArray(T element, MplotArrayMode mode, string name)
    {
        _defaultElements = new List<T> { element };
        _defaultMode = mode;
        _name = name;
        UseDefault = true;
    }

    public bool Add(T item)
    {
        UseDefault = false;
        _elements.Add(item);
        return true;
    }

    public void Update(List<T> items, bool cycle, bool repeat)
    {
        foreach (T i in items) Add(i);
        if (cycle) _mode = MplotArrayMode.Cycle;
        if (repeat) _mode = MplotArrayMode.Repeat;
        UseDefault = false;
    }

    public T this[int index]
    {
        get
        {
            List<T> eleList = UseDefault ? (_defaultElements ?? new List<T>()) : _elements;

            if (!eleList.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Attempted to index an empty MplotArray");
            }

            return Mode() switch
            {
                MplotArrayMode.Cycle => eleList[index % eleList.Count],
                MplotArrayMode.Repeat => index >= eleList.Count ? eleList[^1] : eleList[index],
                MplotArrayMode.Normal => eleList[index],
                _ => eleList[index]
            };
        }
    }

    public bool EquivalentLength(MplotArray<T> other)
    {
        if (Mode() is MplotArrayMode.Cycle or MplotArrayMode.Repeat || other.Mode() is MplotArrayMode.Cycle or MplotArrayMode.Repeat) return true;
        return Length == other.Length;
    }

    int IMplotArray.Length() => Length;

    public override string ToString()
    {
        var elements = UseDefault ? _defaultElements ?? new List<T>() : _elements;

        if (Mode() == MplotArrayMode.Cycle)
        {
            return $"[{string.Join(", ", elements.Select(arg => arg!.ToString()))}, cycle..]";
        }
        else if (Mode() == MplotArrayMode.Normal)
        {
            return $"[{string.Join(", ", elements.Select(arg => arg!.ToString()))}]";
        }
        else if (Mode() == MplotArrayMode.Repeat)
        {
            return $"[{string.Join(", ", elements.Select(arg => arg!.ToString()))}*]";
        }
        else
        {
            return "Mode type not handled.";
        }
    }
}

public interface IMplotArray
{
    int Length();
    MplotArrayMode Mode();
    string Name();
}

public enum MplotArrayMode
{
    Normal = 0,
    Cycle = 1,
    Repeat = 2,
}
