using System.Collections.ObjectModel;
using System.Windows.Input;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Printing;

namespace InventoryApp.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        // ── Categories ────────────────────────────────────────────────
        public ObservableCollection<Category> Categories { get; } = new();

        private Category _selectedCategory = new();
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { SetProperty(ref _selectedCategory, value); EditCategory = value != null ? new Category { Id = value.Id, Name = value.Name, MetalType = value.MetalType } : new(); }
        }

        private Category _editCategory = new();
        public Category EditCategory
        {
            get => _editCategory;
            set => SetProperty(ref _editCategory, value);
        }

        public ICommand SaveCategoryCommand  { get; }
        public ICommand DeleteCategoryCommand{ get; }
        public ICommand NewCategoryCommand   { get; }

        // ── Inventory Items ───────────────────────────────────────────
        public ObservableCollection<InventoryItem> Items { get; } = new();
        public ObservableCollection<Category> CategoriesForFilter { get; } = new();
        public string[] StatusFilters { get; } = { "All", "In Stock", "Sold" };

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); LoadItems(); }
        }

        private Category? _filterCategory;
        public Category? FilterCategory
        {
            get => _filterCategory;
            set { SetProperty(ref _filterCategory, value); LoadItems(); }
        }

        private string _filterStatus = "All";
        public string FilterStatus
        {
            get => _filterStatus;
            set { SetProperty(ref _filterStatus, value); LoadItems(); }
        }

        private InventoryItem _selectedItem = new();
        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set { SetProperty(ref _selectedItem, value); OnPropertyChanged(nameof(IsEditing)); }
        }

        private bool _isAddModalOpen = false;
        public bool IsAddModalOpen
        {
            get => _isAddModalOpen;
            set => SetProperty(ref _isAddModalOpen, value);
        }

        private InventoryItem _formItem = new();
        public InventoryItem FormItem
        {
            get => _formItem;
            set => SetProperty(ref _formItem, value);
        }

        public bool IsEditing => SelectedItem?.SKU?.Length > 0;
        public string[] PurityOptions { get; } = Enum.GetNames(typeof(PurityType));

        public ICommand OpenAddModalCommand { get; }
        public ICommand CloseModalCommand   { get; }
        public ICommand SaveItemCommand     { get; }
        public ICommand DeleteItemCommand   { get; }
        public ICommand PrintTagCommand     { get; }

        public InventoryViewModel()
        {
            SaveCategoryCommand   = new RelayCommand(_ => SaveCategory());
            DeleteCategoryCommand = new RelayCommand(_ => DeleteCategory(), _ => SelectedCategory?.Id > 0);
            NewCategoryCommand    = new RelayCommand(_ => { SelectedCategory = new Category(); EditCategory = new Category(); });
            OpenAddModalCommand   = new RelayCommand(_ => OpenModal(null));
            CloseModalCommand     = new RelayCommand(_ => IsAddModalOpen = false);
            SaveItemCommand       = new RelayCommand(_ => SaveItem());
            DeleteItemCommand     = new RelayCommand(p => DeleteItem(p as InventoryItem), p => p is InventoryItem);
            PrintTagCommand       = new RelayCommand(p => PrintTag(p as InventoryItem), p => p is InventoryItem);

            LoadCategories();
            LoadItems();
        }

        // ── Category logic ────────────────────────────────────────────
        private void LoadCategories()
        {
            Categories.Clear();
            CategoriesForFilter.Clear();
            CategoriesForFilter.Add(new Category { Id = 0, Name = "All Categories" });
            foreach (var c in DatabaseHelper.GetCategories())
            {
                Categories.Add(c);
                CategoriesForFilter.Add(c);
            }
        }

        private void SaveCategory()
        {
            if (string.IsNullOrWhiteSpace(EditCategory.Name)) return;
            if (EditCategory.Id == 0) DatabaseHelper.AddCategory(EditCategory);
            else DatabaseHelper.UpdateCategory(EditCategory);
            LoadCategories();
            EditCategory = new Category();
        }

        private void DeleteCategory()
        {
            DatabaseHelper.DeleteCategory(SelectedCategory.Id);
            LoadCategories();
        }

        // ── Inventory item logic ──────────────────────────────────────
        private void LoadItems()
        {
            bool? isSold = FilterStatus switch { "In Stock" => false, "Sold" => true, _ => null };
            var catId = FilterCategory?.Id > 0 ? FilterCategory.Id : 0;
            Items.Clear();
            foreach (var item in DatabaseHelper.GetInventoryItems(SearchText, catId, isSold))
                Items.Add(item);
        }

        private void OpenModal(InventoryItem? existing)
        {
            FormItem = existing != null
                ? new InventoryItem { SKU = existing.SKU, Name = existing.Name, CategoryId = existing.CategoryId, Purity = existing.Purity, GrossWt = existing.GrossWt, StoneWt = existing.StoneWt, NetWt = existing.NetWt, CostPrice = existing.CostPrice }
                : new InventoryItem { CreatedDate = DateTime.Now };
            IsAddModalOpen = true;
        }

        private void SaveItem()
        {
            if (string.IsNullOrWhiteSpace(FormItem.Name)) return;
            if (string.IsNullOrWhiteSpace(FormItem.SKU))
            {
                var cat = Categories.FirstOrDefault(c => c.Id == FormItem.CategoryId);
                var prefix = cat?.Name?.Length >= 2 ? cat.Name[..2].ToUpper() : "IT";
                FormItem.SKU = DatabaseHelper.GenerateSKU(prefix);
                FormItem.CreatedDate = DateTime.Now;
                DatabaseHelper.AddInventoryItem(FormItem);
            }
            else
            {
                DatabaseHelper.UpdateInventoryItem(FormItem);
            }
            IsAddModalOpen = false;
            LoadItems();
        }

        private void DeleteItem(InventoryItem? item)
        {
            if (item == null) return;
            DatabaseHelper.DeleteInventoryItem(item.SKU);
            LoadItems();
        }

        private void PrintTag(InventoryItem? item)
        {
            if (item == null) return;
            var printerName = DatabaseHelper.GetSetting("LabelPrinter", "");
            var shopName    = DatabaseHelper.GetSetting("ShopName", "Reddy Jewellery");
            LabelPrinter.PrintTag(printerName, shopName, item.SKU);
        }
    }
}
