namespace mplot;

public class MplotStack<T>
{
    private readonly T _defaultValue;
    private readonly Stack<T> _stack = new Stack<T>();
    private T _lastPoppedValue;

    public void Push(T item) => _stack.Push(item);

    public MplotStack(T defaultValue)
    {
        _defaultValue = defaultValue;
        _lastPoppedValue = defaultValue;
    }

    public T Pop()
    {
        if (_stack.TryPop(out T? result))
        {
            _lastPoppedValue = result;
            return result;
        }
        else
        {
            return _lastPoppedValue;
        }
    }
}