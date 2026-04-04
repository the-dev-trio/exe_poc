using System.Windows.Input;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class DailyRatesViewModel : ViewModelBase
    {
        private decimal _gold24K, _gold22K, _gold18K, _silver;
        private string _lastUpdated = string.Empty;

        public decimal Gold24K
        {
            get => _gold24K;
            set { SetProperty(ref _gold24K, value); OnPropertyChanged(nameof(Gold22KAuto)); OnPropertyChanged(nameof(Gold18KAuto)); }
        }
        public decimal Gold22K  { get => _gold22K;  set => SetProperty(ref _gold22K, value); }
        public decimal Gold18K  { get => _gold18K;  set => SetProperty(ref _gold18K, value); }
        public decimal Silver   { get => _silver;   set => SetProperty(ref _silver, value); }

        /// <summary>Suggested 22K = 24K × (22/24). Displayed as a hint.</summary>
        public string Gold22KAuto => Gold24K > 0 ? $"Suggested: ₹{(Gold24K * 22m / 24m):N2}/g" : string.Empty;
        /// <summary>Suggested 18K = 24K × (18/24). Displayed as a hint.</summary>
        public string Gold18KAuto => Gold24K > 0 ? $"Suggested: ₹{(Gold24K * 18m / 24m):N2}/g" : string.Empty;

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public ICommand SaveRatesCommand { get; }

        public DailyRatesViewModel()
        {
            SaveRatesCommand = new RelayCommand(_ => Save());
            Load();
        }

        private void Load()
        {
            var rates = DatabaseHelper.GetMetalRates();
            Gold24K = rates.Gold24K;
            Gold22K = rates.Gold22K;
            Gold18K = rates.Gold18K;
            Silver  = rates.Silver;

            var ts = DatabaseHelper.GetSetting("RatesLastUpdated", "");
            LastUpdated = string.IsNullOrEmpty(ts)
                ? "Not set yet"
                : $"Last saved: {DateTime.Parse(ts):ddd, dd MMM yyyy  hh:mm tt}";
        }

        private void Save()
        {
            if (Gold24K <= 0 && Gold22K <= 0 && Gold18K <= 0 && Silver <= 0)
            {
                System.Windows.MessageBox.Show(
                    "Please enter at least one metal rate before saving.",
                    "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var rates = new MetalRates
            {
                Gold24K = Math.Round(Gold24K, 4),
                Gold22K = Math.Round(Gold22K, 4),
                Gold18K = Math.Round(Gold18K, 4),
                Silver  = Math.Round(Silver,  4),
            };
            DatabaseHelper.SaveMetalRates(rates);
            DatabaseHelper.SetSetting("RatesLastUpdated", DateTime.Now.ToString("o"));
            LastUpdated = $"Last saved: {DateTime.Now:ddd, dd MMM yyyy  hh:mm tt}";

            System.Windows.MessageBox.Show(
                "Metal rates saved successfully.\nAll POS calculations will now use these rates.",
                "Rates Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
