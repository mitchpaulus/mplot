namespace MplotGui.ViewModels;

public class StringArgumentViewModel : ViewModelBase
{
    private string _value = string.Empty;

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
