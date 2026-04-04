using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class PurchasesViewModel : ViewModelBase
    {
        public ObservableCollection<Supplier>       Suppliers      { get; } = new();
        public ObservableCollection<Category>       Categories     { get; } = new();
        public ObservableCollection<InventoryItem>  PurchaseLines  { get; } = new();
        public ObservableCollection<PurchaseRecord> PurchaseHistory{ get; } = new();
        public string[] PurityOptions { get; } = Enum.GetNames(typeof(PurityType));

        private Supplier? _selectedSupplier;
        public Supplier? SelectedSupplier { get => _selectedSupplier; set => SetProperty(ref _selectedSupplier, value); }

        private string _newSupplierName = string.Empty;
        public string NewSupplierName { get => _newSupplierName; set => SetProperty(ref _newSupplierName, value); }

        private string _invoiceNumber = string.Empty;
        public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }

        private DateTime _purchaseDate = DateTime.Today;
        public DateTime PurchaseDate { get => _purchaseDate; set => SetProperty(ref _purchaseDate, value); }

        // Running totals for the current batch
        public decimal BatchTotalWeight => PurchaseLines.Sum(i => i.NetWt);
        public int     BatchItemCount   => PurchaseLines.Count;

        // Form line item
        private InventoryItem _lineForm = new();
        public InventoryItem LineForm { get => _lineForm; set => SetProperty(ref _lineForm, value); }

        public ICommand AddLineCommand               { get; }
        public ICommand RemoveLineCommand            { get; }
        public ICommand AddSupplierCommand           { get; }
        public ICommand SaveAndAddToInventoryCommand { get; }

        public PurchasesViewModel()
        {
            AddLineCommand               = new RelayCommand(_ => AddLine(), _ => !string.IsNullOrWhiteSpace(LineForm?.Name));
            RemoveLineCommand            = new RelayCommand(p => RemoveLine(p as InventoryItem), p => p is InventoryItem);
            AddSupplierCommand           = new RelayCommand(_ => AddSupplier(), _ => !string.IsNullOrWhiteSpace(NewSupplierName));
            SaveAndAddToInventoryCommand = new RelayCommand(_ => SaveAndAddToInventory(),
                _ => PurchaseLines.Count > 0 && SelectedSupplier != null);
            Load();
        }

        private void Load()
        {
            Suppliers.Clear();
            foreach (var s in DatabaseHelper.GetSuppliers()) Suppliers.Add(s);
            Categories.Clear();
            foreach (var c in DatabaseHelper.GetCategories()) Categories.Add(c);
            LoadHistory();
        }

        private void LoadHistory()
        {
            PurchaseHistory.Clear();
            foreach (var p in DatabaseHelper.GetPurchases(100)) PurchaseHistory.Add(p);
        }

        private void AddLine()
        {
            if (string.IsNullOrWhiteSpace(LineForm.Name)) return;
            LineForm.NetWt = Math.Round(LineForm.GrossWt - LineForm.StoneWt, 4);
            PurchaseLines.Add(LineForm);
            LineForm = new InventoryItem();
            OnPropertyChanged(nameof(BatchTotalWeight));
            OnPropertyChanged(nameof(BatchItemCount));
        }

        private void RemoveLine(InventoryItem? item)
        {
            if (item == null) return;
            PurchaseLines.Remove(item);
            OnPropertyChanged(nameof(BatchTotalWeight));
            OnPropertyChanged(nameof(BatchItemCount));
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
            if (SelectedSupplier == null || PurchaseLines.Count == 0) return;

            decimal totalWt = 0;
            foreach (var item in PurchaseLines)
            {
                var cat    = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
                var prefix = cat?.Name?.Length >= 2 ? cat.Name[..2].ToUpper() : "PU";
                item.SKU         = DatabaseHelper.GenerateSKU(prefix);
                item.CreatedDate = PurchaseDate;
                DatabaseHelper.AddInventoryItem(item);
                totalWt += item.NetWt;
            }

            // Persist the purchase batch
            var record = new PurchaseRecord
            {
                Date          = PurchaseDate,
                SupplierId    = SelectedSupplier.Id,
                SupplierName  = SelectedSupplier.Name,
                InvoiceNumber = InvoiceNumber,
                TotalWeight   = Math.Round(totalWt, 4),
                ItemCount     = PurchaseLines.Count,
                LineItemsJson = JsonSerializer.Serialize(PurchaseLines),
            };
            DatabaseHelper.AddPurchase(record);

            System.Windows.MessageBox.Show(
                $"{PurchaseLines.Count} item(s) ({totalWt:N4}g total) added to inventory.",
                "Purchase Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            PurchaseLines.Clear();
            InvoiceNumber = string.Empty;
            PurchaseDate  = DateTime.Today;
            OnPropertyChanged(nameof(BatchTotalWeight));
            OnPropertyChanged(nameof(BatchItemCount));
            LoadHistory();
        }
    }
}
