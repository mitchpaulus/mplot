namespace MplotGui.ViewModels;

public class DataSourceItemViewModel : ViewModelBase
{
    private DataSourceOption _dataSource = DataSourceOption.File;
    private string _filePath = string.Empty;
    private string _inlineData = string.Empty;

    public DataSourceOption DataSource
    {
        get => _dataSource;
        set => SetProperty(ref _dataSource, value);
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
}
