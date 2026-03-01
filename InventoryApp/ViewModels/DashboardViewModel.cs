using InventoryApp.Data;

namespace InventoryApp.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set => SetProperty(ref _totalItems, value);
        }

        private decimal _totalValue;
        public decimal TotalValue
        {
            get => _totalValue;
            set => SetProperty(ref _totalValue, value);
        }

        public DashboardViewModel()
        {
            LoadSummary();
        }

        private void LoadSummary()
        {
            var summary = DatabaseHelper.GetSummary();
            TotalItems = summary.TotalItems;
            TotalValue = summary.TotalValue;
        }
    }
}
