namespace MplotGui.ViewModels;

public class DataSourceItemViewModel : ViewModelBase
{
    private DataSourceOption _dataSource = DataSourceOption.File;
    private string _filePath = string.Empty;
    private string _inlineData = string.Empty;

    public DataSourceOption DataSource
    {
        get => _dataSource;
        set
        {
            if (SetProperty(ref _dataSource, value))
            {
                OnPropertyChanged(nameof(IsFileSource));
                OnPropertyChanged(nameof(IsInlineSource));
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public string InlineData
    {
        get => _inlineData;
        set => SetProperty(ref _inlineData, value);
    }

    public bool IsFileSource => DataSource == DataSourceOption.File;

    public bool IsInlineSource => DataSource == DataSourceOption.Inline;
}
