using System.Windows.Input;
using System.Windows;
using System.IO;
using InventoryApp.Data;
using System.Windows.Threading;

namespace InventoryApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        private string _currentNav = "Dashboard";
        private string _shopName    = "My Jewellery";
        private string _currentDateTime = string.Empty;
        private string _currentPageTitle = "Dashboard";
        private readonly DispatcherTimer _clockTimer;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        public string ShopName
        {
            get => _shopName;
            set => SetProperty(ref _shopName, value);
        }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        // ── Nav styles ───────────────────────────────────────────────
        public Style DashboardNavStyle  => NavStyle("Dashboard");
        public Style DailyRatesNavStyle => NavStyle("DailyRates");
        public Style SalesNavStyle      => NavStyle("Sales");
        public Style InventoryNavStyle  => NavStyle("Inventory");
        public Style PurchasesNavStyle  => NavStyle("Purchases");
        public Style CustomersNavStyle  => NavStyle("Customers");
        public Style ReportsNavStyle    => NavStyle("Reports");
        public Style SettingsNavStyle   => NavStyle("Settings");

        // ── Commands ──────────────────────────────────────────────────
        public ICommand ShowDashboardCommand  { get; }
        public ICommand ShowDailyRatesCommand { get; }
        public ICommand ShowSalesCommand      { get; }
        public ICommand ShowInventoryCommand  { get; }
        public ICommand ShowPurchasesCommand  { get; }
        public ICommand ShowCustomersCommand  { get; }
        public ICommand ShowReportsCommand    { get; }
        public ICommand ShowSettingsCommand   { get; }
        public ICommand BackupNowCommand      { get; }

        public MainViewModel()
        {
            _shopName    = DatabaseHelper.GetSetting("ShopName", "My Jewellery");
            _currentView = new DashboardViewModel();

            ShowDashboardCommand  = new RelayCommand(_ => Navigate("Dashboard",  "🏠  Dashboard",   () => new DashboardViewModel()));
            ShowDailyRatesCommand = new RelayCommand(_ => Navigate("DailyRates", "💰  Daily Rates",  () => new DailyRatesViewModel()));
            ShowSalesCommand      = new RelayCommand(_ => Navigate("Sales",      "🛒  POS / Sales",  () => new SalesViewModel()));
            ShowInventoryCommand  = new RelayCommand(_ => Navigate("Inventory",  "📦  Inventory",    () => new InventoryViewModel()));
            ShowPurchasesCommand  = new RelayCommand(_ => Navigate("Purchases",  "🚚  Purchases",    () => new PurchasesViewModel()));
            ShowCustomersCommand  = new RelayCommand(_ => Navigate("Customers",  "👤  Customers",    () => new CustomersViewModel()));
            ShowReportsCommand    = new RelayCommand(_ => Navigate("Reports",    "📊  Monthly Reports", () => new ReportsViewModel()));
            ShowSettingsCommand   = new RelayCommand(_ => Navigate("Settings",   "⚙️  Settings",     () => new SettingsViewModel(OnSettingsSaved)));
            BackupNowCommand      = new RelayCommand(_ => BackupNow());

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => CurrentDateTime = DateTime.Now.ToString("ddd, dd MMM yyyy   hh:mm:ss tt");
            _clockTimer.Start();
            CurrentDateTime = DateTime.Now.ToString("ddd, dd MMM yyyy   hh:mm:ss tt");
        }

        private void Navigate(string name, string pageTitle, Func<ViewModelBase> factory)
        {
            _currentNav      = name;
            CurrentPageTitle = pageTitle;
            CurrentView      = factory();
            // Notify all nav styles
            OnPropertyChanged(nameof(DashboardNavStyle));
            OnPropertyChanged(nameof(DailyRatesNavStyle));
            OnPropertyChanged(nameof(SalesNavStyle));
            OnPropertyChanged(nameof(InventoryNavStyle));
            OnPropertyChanged(nameof(PurchasesNavStyle));
            OnPropertyChanged(nameof(CustomersNavStyle));
            OnPropertyChanged(nameof(ReportsNavStyle));
            OnPropertyChanged(nameof(SettingsNavStyle));
        }

        private Style NavStyle(string name)
        {
            var key = _currentNav == name ? "NavButtonActive" : "NavButton";
            return (Style)System.Windows.Application.Current.FindResource(key);
        }

        private void OnSettingsSaved()
        {
            ShopName = DatabaseHelper.GetSetting("ShopName", "My Jewellery");
        }

        private void BackupNow()
        {
            var backupPath = DatabaseHelper.GetSetting("BackupPath", "");
            if (string.IsNullOrWhiteSpace(backupPath) || !Directory.Exists(backupPath))
            {
                System.Windows.MessageBox.Show(
                    "Please set a Backup Path in Settings first.",
                    "Backup", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var dest = Path.Combine(backupPath, $"jms_backup_{DateTime.Now:yyyyMMdd_HHmm}.db");
            DatabaseHelper.BackupTo(dest);
            System.Windows.MessageBox.Show($"Backup saved to:\n{dest}", "Backup Complete",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
