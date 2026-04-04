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

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set 
            { 
                if (SetProperty(ref _selectedCategory, value))
                {
                    // Master-Detail mapping: Clicking a category in sidebar filters the items
                    LoadItems();
                }
            }
        }

        private Category _editCategory = new();
        public Category EditCategory
        {
            get => _editCategory;
            set => SetProperty(ref _editCategory, value);
        }

        private bool _isCategoryModalOpen = false;
        public bool IsCategoryModalOpen
        {
            get => _isCategoryModalOpen;
            set => SetProperty(ref _isCategoryModalOpen, value);
        }

        public ICommand OpenCategoryModalCommand { get; }
        public ICommand CloseCategoryModalCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand SaveCategoryCommand  { get; }
        public ICommand DeleteCategoryCommand{ get; }
        public ICommand NewCategoryCommand   { get; }

        // ── Inventory Items ───────────────────────────────────────────
        public ObservableCollection<InventoryItem> Items { get; } = new();
        public ObservableCollection<Category> CategoriesForFilter { get; } = new();
        public string[] StatusFilters { get; } = { "All", "In Stock", "Sold" };

        private int _totalItemsCount;
        public int TotalItemsCount
        {
            get => _totalItemsCount;
            set => SetProperty(ref _totalItemsCount, value);
        }

        private int _inStockCount;
        public int InStockCount
        {
            get => _inStockCount;
            set => SetProperty(ref _inStockCount, value);
        }

        private int _visibleItemsCount;
        public int VisibleItemsCount
        {
            get => _visibleItemsCount;
            set => SetProperty(ref _visibleItemsCount, value);
        }

        private int _categoriesCount;
        public int CategoriesCount
        {
            get => _categoriesCount;
            set => SetProperty(ref _categoriesCount, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); LoadItems(); }
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
            OpenCategoryModalCommand = new RelayCommand(_ => IsCategoryModalOpen = true);
            CloseCategoryModalCommand= new RelayCommand(_ => IsCategoryModalOpen = false);
            
            EditCategoryCommand   = new RelayCommand(p => EditCategory = p as Category ?? new Category());
            SaveCategoryCommand   = new RelayCommand(_ => SaveCategory());
            DeleteCategoryCommand = new RelayCommand(p => DeleteCategory(p as Category), p => p is Category c && c.Id > 0);
            NewCategoryCommand    = new RelayCommand(_ => EditCategory = new Category());
            
            OpenAddModalCommand   = new RelayCommand(p => OpenModal(p as InventoryItem));
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
            var prevCatId = SelectedCategory?.Id;
            Categories.Clear();
            CategoriesForFilter.Clear();
            CategoriesForFilter.Add(new Category { Id = 0, Name = "All Categories" });
            
            // Re-fetch (now includes ItemCount)
            foreach (var c in DatabaseHelper.GetCategories())
            {
                Categories.Add(c);
                CategoriesForFilter.Add(c);
            }
            CategoriesCount = Categories.Count;
            
            // Restore selection if possible, otherwise select "All" (which is actually null in our SelectedCategory semantics)
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == prevCatId);
        }

        private void SaveCategory()
        {
            if (string.IsNullOrWhiteSpace(EditCategory.Name)) return;
            if (EditCategory.Id == 0) DatabaseHelper.AddCategory(EditCategory);
            else DatabaseHelper.UpdateCategory(EditCategory);
            LoadCategories();
            EditCategory = new Category();
        }

        private void DeleteCategory(Category? cat)
        {
            if (cat == null || cat.Id <= 0) return;
            DatabaseHelper.DeleteCategory(cat.Id);
            if (SelectedCategory?.Id == cat.Id) SelectedCategory = null;
            LoadCategories();
            EditCategory = new Category(); // clear
        }

        // ── Inventory item logic ──────────────────────────────────────
        private void LoadItems()
        {
            bool? isSold = FilterStatus switch { "In Stock" => false, "Sold" => true, _ => null };
            var catId = SelectedCategory?.Id > 0 ? SelectedCategory.Id : 0;
            
            Items.Clear();
            foreach (var item in DatabaseHelper.GetInventoryItems(SearchText, catId, isSold))
                Items.Add(item);
            VisibleItemsCount = Items.Count;

            var counts = DatabaseHelper.GetInventoryCounts();
            TotalItemsCount = counts.Total;
            InStockCount = counts.InStock;
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
            // Reload categories so ItemCount updates on the sidebar
            LoadCategories();
            LoadItems();
        }

        private void DeleteItem(InventoryItem? item)
        {
            if (item == null) return;
            DatabaseHelper.DeleteInventoryItem(item.SKU);
            LoadCategories();
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
