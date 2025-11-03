using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MplotGui.ViewModels;

namespace MplotGui;

public partial class MainWindow : Window
{
    public PlotViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void OnCopyToClipboard(object? sender, RoutedEventArgs e)
    {
        var (success, script, error) = ViewModel.TryGenerateGnuplot();
        if (success && !string.IsNullOrWhiteSpace(script))
        {
            if (Clipboard is not null)
            {
                await Clipboard.SetTextAsync(script);
                ViewModel.UpdateStatus($"Copied to clipboard at {DateTime.Now:T}");
            }
        }
        else if (!success && !string.IsNullOrWhiteSpace(error))
        {
            ViewModel.UpdateStatus(error);
        }
    }

    private void OnGeneratePreview(object? sender, RoutedEventArgs e)
    {
        ViewModel.GenerateMplotPreview();
    }
}
