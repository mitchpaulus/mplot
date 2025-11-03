using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MplotGui.Commands;

namespace MplotGui.ViewModels;

public class MplotArrayEditorViewModel<TItem> : ViewModelBase
{
    private readonly Func<int, TItem> _createItem;

    public MplotArrayEditorViewModel(Func<int, TItem> createItem, IEnumerable<TItem>? initialItems = null)
    {
        _createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        Items = new ObservableCollection<TItem>(initialItems ?? Enumerable.Empty<TItem>());

        AddCommand = new RelayCommand(_ => AddNewItem());
        RemoveCommand = new RelayCommand(_ => RemoveSelectedItem(), _ => SelectedItem is not null);

        if (Items.Count > 0)
        {
            SelectedItem = Items[0];
        }
    }

    public ObservableCollection<TItem> Items { get; }

    private TItem? _selectedItem;
    public TItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RemoveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand AddCommand { get; }

    public RelayCommand RemoveCommand { get; }

    private void AddNewItem()
    {
        var newItem = _createItem(Items.Count);
        Items.Add(newItem);
        SelectedItem = newItem;
    }

    private void RemoveSelectedItem()
    {
        if (SelectedItem is null) return;

        Items.Remove(SelectedItem);
        SelectedItem = Items.LastOrDefault();
    }
}
