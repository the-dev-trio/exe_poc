using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;
using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Printing;

namespace InventoryApp.ViewModels
{
    public class SalesViewModel : ViewModelBase
    {
        private MetalRates _rates;
        public MetalRates Rates { get; private set; }

        // Cart
        public ObservableCollection<CartItem> Cart { get; } = new();
        private string _scanInput = string.Empty;
        public string ScanInput
        {
            get => _scanInput;
            set => SetProperty(ref _scanInput, value);
        }

        private decimal _makingCharge;
        public decimal MakingCharge
        {
            get => _makingCharge;
            set { SetProperty(ref _makingCharge, value); RecalcTotals(); }
        }

        private decimal _subtotal, _gstAmount, _grandTotal;
        public decimal Subtotal    { get => _subtotal;   set => SetProperty(ref _subtotal, value); }
        public decimal GstAmount   { get => _gstAmount;  set => SetProperty(ref _gstAmount, value); }
        public decimal GrandTotal  { get => _grandTotal; set => SetProperty(ref _grandTotal, value); }

        // Customer
        public ObservableCollection<Customer> CustomerSuggestions { get; } = new();
        private Customer _currentCustomer = new();
        public Customer CurrentCustomer
        {
            get => _currentCustomer;
            set => SetProperty(ref _currentCustomer, value);
        }

        private string _customerSearch = string.Empty;
        public string CustomerSearch
        {
            get => _customerSearch;
            set { SetProperty(ref _customerSearch, value); SearchCustomers(); }
        }

        // Payment
        public string[] PaymentModes { get; } = { "Cash", "Card", "UPI", "Old Gold Exchange" };
        private string _selectedPaymentMode = "Cash";
        public string SelectedPaymentMode
        {
            get => _selectedPaymentMode;
            set => SetProperty(ref _selectedPaymentMode, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand ScanCommand          { get; }
        public ICommand RemoveCartItemCommand { get; }
        public ICommand SelectCustomerCommand { get; }
        public ICommand CompleteSaleCommand  { get; }
        public ICommand ClearCartCommand     { get; }

        public SalesViewModel()
        {
            _rates = DatabaseHelper.GetMetalRates();
            Rates = _rates;
            ScanCommand           = new RelayCommand(_ => ScanItem());
            RemoveCartItemCommand = new RelayCommand(p => RemoveFromCart(p as CartItem), p => p is CartItem);
            SelectCustomerCommand = new RelayCommand(p => SelectCustomer(p as Customer));
            CompleteSaleCommand   = new RelayCommand(_ => CompleteSale(), _ => Cart.Count > 0);
            ClearCartCommand      = new RelayCommand(_ => { Cart.Clear(); RecalcTotals(); });
        }

        private void ScanItem()
        {
            var sku = ScanInput.Trim();
            ScanInput = string.Empty;
            if (string.IsNullOrEmpty(sku)) return;

            var item = DatabaseHelper.GetItemBySKU(sku);
            if (item == null) { StatusMessage = $"SKU '{sku}' not found."; return; }
            if (item.IsSold)  { StatusMessage = $"'{sku}' is already sold."; return; }

            var rate = _rates.GetRate(item.Purity);
            Cart.Add(new CartItem { SKU = item.SKU, Name = item.Name, NetWt = item.NetWt, Purity = item.Purity, Rate = rate, MakingCharge = MakingCharge });
            StatusMessage = $"Added: {item.Name}";
            RecalcTotals();
        }

        private void RemoveFromCart(CartItem? item)
        {
            if (item != null) { Cart.Remove(item); RecalcTotals(); }
        }

        private void RecalcTotals()
        {
            Subtotal   = Cart.Sum(i => i.FinalAmount);
            GstAmount  = Math.Round(Subtotal * 0.03m, 2); // 3% GST
            GrandTotal = Subtotal + GstAmount;
        }

        private void SearchCustomers()
        {
            CustomerSuggestions.Clear();
            if (CustomerSearch.Length < 2) return;
            foreach (var c in DatabaseHelper.SearchCustomers(CustomerSearch))
                CustomerSuggestions.Add(c);
        }

        private void SelectCustomer(Customer? c)
        {
            if (c == null) return;
            CurrentCustomer = c;
            CustomerSearch = c.Name;
            CustomerSuggestions.Clear();
        }

        private void CompleteSale()
        {
            if (Cart.Count == 0) return;

            // Save/get customer
            if (!string.IsNullOrWhiteSpace(CurrentCustomer.Name))
                CurrentCustomer.Id = DatabaseHelper.AddOrGetCustomer(CurrentCustomer);

            // Record transaction
            var tx = new TransactionRecord
            {
                Date         = DateTime.Now,
                CustomerId   = CurrentCustomer.Id,
                CustomerName = CurrentCustomer.Name,
                TotalAmount  = GrandTotal,
                LineItemsJson = JsonSerializer.Serialize(Cart)
            };
            DatabaseHelper.AddTransaction(tx);

            // Mark items sold
            foreach (var item in Cart)
                DatabaseHelper.MarkAsSold(item.SKU);

            // Print receipt
            var printerName = DatabaseHelper.GetSetting("ReceiptPrinter", "");
            var shopName    = DatabaseHelper.GetSetting("ShopName", "Reddy Jewellery");
            var address     = DatabaseHelper.GetSetting("ShopAddress", "");
            ReceiptPrinter.PrintReceipt(printerName, shopName, address,
                CurrentCustomer.Name, tx.Date, Cart, GrandTotal, SelectedPaymentMode);

            StatusMessage = "✅ Sale completed!";
            Cart.Clear();
            RecalcTotals();
            CurrentCustomer = new Customer();
            CustomerSearch = string.Empty;
        }
    }
}
