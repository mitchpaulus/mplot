namespace MplotGui.ViewModels;

public class ValueViewModel<T> : ViewModelBase
{
    private T _value;

    public ValueViewModel()
    {
        _value = default!;
    }

    public ValueViewModel(T value)
    {
        _value = value;
    }

    public T Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
