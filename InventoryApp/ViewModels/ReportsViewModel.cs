using System.Collections.ObjectModel;
using System.Windows.Input;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private DateTime _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _endDate = DateTime.Today;

        public DateTime StartDate
        {
            get => _startDate;
            set { SetProperty(ref _startDate, value); LoadData(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { SetProperty(ref _endDate, value); LoadData(); }
        }

        public ObservableCollection<SaleReportRow> SalesData { get; } = new();

        private decimal _totalSalesAmount;
        public decimal TotalSalesAmount
        {
            get => _totalSalesAmount;
            set => SetProperty(ref _totalSalesAmount, value);
        }

        private decimal _totalWeightSold;
        public decimal TotalWeightSold
        {
            get => _totalWeightSold;
            set => SetProperty(ref _totalWeightSold, value);
        }

        public ICommand FilterCommand { get; }

        public ReportsViewModel()
        {
            FilterCommand = new RelayCommand(_ => LoadData());
            LoadData();
        }

        private void LoadData()
        {
            SalesData.Clear();
            var report = DatabaseHelper.GetSaleReport(StartDate, EndDate);
            
            decimal totalAmt = 0;
            decimal totalWt = 0;

            foreach (var row in report)
            {
                SalesData.Add(row);
                totalAmt += row.TotalAmount;
                totalWt += row.TotalWeight;
            }

            TotalSalesAmount = totalAmt;
            // 4-decimal precision for weight
            TotalWeightSold = Math.Round(totalWt, 4);
        }
    }
}
