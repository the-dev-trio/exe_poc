using System.Collections.ObjectModel;
using System.Windows.Input;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class PurchasesViewModel : ViewModelBase
    {
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<InventoryItem> PurchaseLines { get; } = new();
        public string[] PurityOptions { get; } = Enum.GetNames(typeof(PurityType));

        private Supplier? _selectedSupplier;
        public Supplier? SelectedSupplier { get => _selectedSupplier; set => SetProperty(ref _selectedSupplier, value); }

        private string _newSupplierName = string.Empty;
        public string NewSupplierName { get => _newSupplierName; set => SetProperty(ref _newSupplierName, value); }

        private string _invoiceNumber = string.Empty;
        public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }

        private DateTime _purchaseDate = DateTime.Today;
        public DateTime PurchaseDate { get => _purchaseDate; set => SetProperty(ref _purchaseDate, value); }

        // Form line item
        private InventoryItem _lineForm = new();
        public InventoryItem LineForm { get => _lineForm; set => SetProperty(ref _lineForm, value); }

        public ICommand AddLineCommand          { get; }
        public ICommand RemoveLineCommand       { get; }
        public ICommand AddSupplierCommand      { get; }
        public ICommand SaveAndAddToInventoryCommand { get; }

        public PurchasesViewModel()
        {
            AddLineCommand = new RelayCommand(_ => AddLine());
            RemoveLineCommand = new RelayCommand(p => PurchaseLines.Remove((InventoryItem)p!), p => p is InventoryItem);
            AddSupplierCommand = new RelayCommand(_ => AddSupplier());
            SaveAndAddToInventoryCommand = new RelayCommand(_ => SaveAndAddToInventory(), _ => PurchaseLines.Count > 0 && SelectedSupplier != null);
            Load();
        }

        private void Load()
        {
            Suppliers.Clear();
            foreach (var s in DatabaseHelper.GetSuppliers()) Suppliers.Add(s);
            Categories.Clear();
            foreach (var c in DatabaseHelper.GetCategories()) Categories.Add(c);
        }

        private void AddLine()
        {
            if (string.IsNullOrWhiteSpace(LineForm.Name)) return;
            LineForm.NetWt = LineForm.GrossWt - LineForm.StoneWt;
            PurchaseLines.Add(LineForm);
            LineForm = new InventoryItem();
        }

        private void AddSupplier()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName)) return;
            DatabaseHelper.AddSupplier(NewSupplierName);
            NewSupplierName = string.Empty;
            Load();
        }

        private void SaveAndAddToInventory()
        {
            foreach (var item in PurchaseLines)
            {
                var cat = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
                var prefix = cat?.Name?.Length >= 2 ? cat.Name[..2].ToUpper() : "PU";
                item.SKU = DatabaseHelper.GenerateSKU(prefix);
                item.CreatedDate = PurchaseDate;
                DatabaseHelper.AddInventoryItem(item);
            }
            System.Windows.MessageBox.Show(
                $"{PurchaseLines.Count} item(s) added to inventory.",
                "Purchase Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            PurchaseLines.Clear();
            InvoiceNumber = string.Empty;
            PurchaseDate = DateTime.Today;
        }
    }
}
