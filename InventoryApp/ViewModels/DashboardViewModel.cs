using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private MetalRates _rates = new();
        public MetalRates Rates { get => _rates; set => SetProperty(ref _rates, value); }

        private int _totalCount;
        private decimal _goldWeight, _silverWeight, _totalValue;

        public int    TotalCount   { get => _totalCount;   set => SetProperty(ref _totalCount, value); }
        public decimal GoldWeight  { get => _goldWeight;   set => SetProperty(ref _goldWeight, value); }
        public decimal SilverWeight{ get => _silverWeight; set => SetProperty(ref _silverWeight, value); }
        public decimal TotalValue  { get => _totalValue;   set => SetProperty(ref _totalValue, value); }

        public System.Windows.Input.ICommand UpdateRatesCommand { get; }
        public System.Windows.Input.ICommand NewSaleCommand     { get; }
        public System.Windows.Input.ICommand AddStockCommand    { get; }

        public DashboardViewModel()
        {
            UpdateRatesCommand = new RelayCommand(_ => UpdateRates());
            NewSaleCommand     = new RelayCommand(_ => { });   // Navigation handled by MainViewModel
            AddStockCommand    = new RelayCommand(_ => { });
            Load();
        }

        private void Load()
        {
            Rates = DatabaseHelper.GetMetalRates();
            RefreshSummary();
        }

        private void UpdateRates()
        {
            DatabaseHelper.SaveMetalRates(Rates);
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            var (count, goldWt, silverWt, totalVal) = DatabaseHelper.GetDashboardSummary(Rates);
            TotalCount    = count;
            GoldWeight    = goldWt;
            SilverWeight  = silverWt;
            TotalValue    = totalVal;
        }
    }
}
