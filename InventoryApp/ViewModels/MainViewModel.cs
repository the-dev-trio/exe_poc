using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using InventoryApp.Data;
using System.Timers;
using System.Windows.Threading;

namespace InventoryApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        private bool _isSidebarExpanded = true;
        private string _currentNav = "Dashboard";
        private string _shopName = "Reddy Jewellery";
        private string _currentDateTime = string.Empty;
        private readonly DispatcherTimer _clockTimer;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string SidebarWidth => _isSidebarExpanded ? "200" : "0";
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

        // Nav button styles
        public Style DashboardNavStyle   => NavStyle("Dashboard");
        public Style SalesNavStyle       => NavStyle("Sales");
        public Style InventoryNavStyle   => NavStyle("Inventory");
        public Style PurchasesNavStyle   => NavStyle("Purchases");
        public Style SettingsNavStyle    => NavStyle("Settings");

        public ICommand ShowDashboardCommand  { get; }
        public ICommand ShowSalesCommand      { get; }
        public ICommand ShowInventoryCommand  { get; }
        public ICommand ShowPurchasesCommand  { get; }
        public ICommand ShowSettingsCommand   { get; }
        public ICommand ToggleSidebarCommand  { get; }
        public ICommand BackupNowCommand      { get; }

        public MainViewModel()
        {
            _shopName = DatabaseHelper.GetSetting("ShopName", "Reddy Jewellery");
            _currentView = new DashboardViewModel();

            ShowDashboardCommand  = new RelayCommand(_ => Navigate("Dashboard",  () => new DashboardViewModel()));
            ShowSalesCommand      = new RelayCommand(_ => Navigate("Sales",      () => new SalesViewModel()));
            ShowInventoryCommand  = new RelayCommand(_ => Navigate("Inventory",  () => new InventoryViewModel()));
            ShowPurchasesCommand  = new RelayCommand(_ => Navigate("Purchases",  () => new PurchasesViewModel()));
            ShowSettingsCommand   = new RelayCommand(_ => Navigate("Settings",   () => new SettingsViewModel(OnSettingsSaved)));
            ToggleSidebarCommand  = new RelayCommand(_ => ToggleSidebar());
            BackupNowCommand      = new RelayCommand(_ => BackupNow());

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => CurrentDateTime = DateTime.Now.ToString("ddd, dd MMM yyyy   hh:mm:ss tt");
            _clockTimer.Start();
            CurrentDateTime = DateTime.Now.ToString("ddd, dd MMM yyyy   hh:mm:ss tt");
        }

        private void Navigate(string name, Func<ViewModelBase> factory)
        {
            _currentNav = name;
            CurrentView = factory();
            // Notify all nav styles to refresh
            OnPropertyChanged(nameof(DashboardNavStyle));
            OnPropertyChanged(nameof(SalesNavStyle));
            OnPropertyChanged(nameof(InventoryNavStyle));
            OnPropertyChanged(nameof(PurchasesNavStyle));
            OnPropertyChanged(nameof(SettingsNavStyle));
        }

        private Style NavStyle(string name)
        {
            var key = _currentNav == name ? "NavButtonActive" : "NavButton";
            return (Style)System.Windows.Application.Current.FindResource(key);
        }

        private void ToggleSidebar()
        {
            _isSidebarExpanded = !_isSidebarExpanded;
            OnPropertyChanged(nameof(SidebarWidth));
        }

        private void OnSettingsSaved()
        {
            ShopName = DatabaseHelper.GetSetting("ShopName", "Reddy Jewellery");
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
