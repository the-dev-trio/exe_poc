using System.Collections.ObjectModel;
using System.Windows.Input;
using InventoryApp.Models;
using InventoryApp.Data;

namespace InventoryApp.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private ObservableCollection<InventoryItem> _items = new();
        public ObservableCollection<InventoryItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private InventoryItem _selectedItem = new();
        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }

        public InventoryViewModel()
        {
            LoadItems();
            SaveCommand = new RelayCommand(_ => SaveItem());
            DeleteCommand = new RelayCommand(p => DeleteItem(p), p => p is InventoryItem);
            ClearCommand = new RelayCommand(_ => ClearSelection());
        }

        private void LoadItems()
        {
            Items = new ObservableCollection<InventoryItem>(DatabaseHelper.GetAllItems());
        }

        private void SaveItem()
        {
            if (string.IsNullOrWhiteSpace(SelectedItem.Name)) return;

            if (SelectedItem.Id == 0)
            {
                DatabaseHelper.AddItem(SelectedItem);
            }
            else
            {
                DatabaseHelper.UpdateItem(SelectedItem);
            }
            
            ClearSelection();
            LoadItems();
        }

        private void DeleteItem(object? parameter)
        {
            if (parameter is InventoryItem item)
            {
                DatabaseHelper.DeleteItem(item.Id);
                LoadItems();
                if (SelectedItem?.Id == item.Id) ClearSelection();
            }
        }

        private void ClearSelection()
        {
            SelectedItem = new InventoryItem();
        }
    }
}
