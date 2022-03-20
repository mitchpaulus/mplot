namespace mplot;

public class MplotQueue<T>
{
    private readonly T _defaultValue;
    private readonly Queue<T> _queue = new Queue<T>();
    private T _lastPoppedValue;

    public void Queue(T item) => _queue.Enqueue(item);

    public void Queue(IEnumerable<T> items)   
    {
        foreach (var item in items)
        {
            _queue.Enqueue(item); 
        }
    }

    public MplotQueue(T defaultValue)
    {
        _defaultValue = defaultValue;
        _lastPoppedValue = defaultValue;
    }

    public T Pop()
    {
        if (_queue.TryDequeue(out T? result))
        {
            _lastPoppedValue = result;
            return result;
        }
        else
        {
            return _lastPoppedValue;
        }
    }

    public int Count => _queue.Count;
}