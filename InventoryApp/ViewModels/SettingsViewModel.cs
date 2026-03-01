using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Printing;
using InventoryApp.Data;

namespace InventoryApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly Action _onSaved;

        public ObservableCollection<string> InstalledPrinters { get; } = new();

        private string _shopName = string.Empty;
        public string ShopName { get => _shopName; set => SetProperty(ref _shopName, value); }

        private string _shopAddress = string.Empty;
        public string ShopAddress { get => _shopAddress; set => SetProperty(ref _shopAddress, value); }

        private string _gstNumber = string.Empty;
        public string GSTNumber { get => _gstNumber; set => SetProperty(ref _gstNumber, value); }

        private string _labelPrinter = string.Empty;
        public string LabelPrinter { get => _labelPrinter; set => SetProperty(ref _labelPrinter, value); }

        private string _receiptPrinter = string.Empty;
        public string ReceiptPrinter { get => _receiptPrinter; set => SetProperty(ref _receiptPrinter, value); }

        private bool _autoBackup;
        public bool AutoBackup { get => _autoBackup; set => SetProperty(ref _autoBackup, value); }

        private string _backupPath = string.Empty;
        public string BackupPath { get => _backupPath; set => SetProperty(ref _backupPath, value); }

        public ICommand SaveCommand              { get; }
        public ICommand BrowseBackupPathCommand  { get; }
        public ICommand TestLabelPrinterCommand  { get; }
        public ICommand TestReceiptPrinterCommand{ get; }

        public SettingsViewModel(Action onSaved)
        {
            _onSaved = onSaved;
            SaveCommand               = new RelayCommand(_ => Save());
            BrowseBackupPathCommand   = new RelayCommand(_ => BrowseBackup());
            TestLabelPrinterCommand   = new RelayCommand(_ => TestLabel());
            TestReceiptPrinterCommand = new RelayCommand(_ => TestReceipt());
            Load();
        }

        private void Load()
        {
            ShopName      = DatabaseHelper.GetSetting("ShopName", "Reddy Jewellery");
            ShopAddress   = DatabaseHelper.GetSetting("ShopAddress", "");
            GSTNumber     = DatabaseHelper.GetSetting("GSTNumber", "");
            LabelPrinter  = DatabaseHelper.GetSetting("LabelPrinter", "");
            ReceiptPrinter = DatabaseHelper.GetSetting("ReceiptPrinter", "");
            AutoBackup    = DatabaseHelper.GetSetting("AutoBackup", "0") == "1";
            BackupPath    = DatabaseHelper.GetSetting("BackupPath", "");

            // Enumerate installed printers
            InstalledPrinters.Clear();
            InstalledPrinters.Add("(none)");
            try
            {
                using var queue = new PrintQueue(new PrintServer(), "");
            }
            catch { }
            // Fallback: use System.Drawing.Printing
            foreach (System.Drawing.Printing.PrinterSettings.StringCollection printers =
                     System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                foreach (string printer in printers) InstalledPrinters.Add(printer);
            }
        }

        private void Save()
        {
            DatabaseHelper.SetSetting("ShopName",      ShopName);
            DatabaseHelper.SetSetting("ShopAddress",   ShopAddress);
            DatabaseHelper.SetSetting("GSTNumber",     GSTNumber);
            DatabaseHelper.SetSetting("LabelPrinter",  LabelPrinter  == "(none)" ? "" : LabelPrinter);
            DatabaseHelper.SetSetting("ReceiptPrinter", ReceiptPrinter == "(none)" ? "" : ReceiptPrinter);
            DatabaseHelper.SetSetting("AutoBackup",    AutoBackup ? "1" : "0");
            DatabaseHelper.SetSetting("BackupPath",    BackupPath);
            _onSaved?.Invoke();
            System.Windows.MessageBox.Show("Settings saved.", "Settings",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void BrowseBackup()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog { Description = "Select Backup Folder" };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                BackupPath = dlg.SelectedPath;
        }

        private void TestLabel()
        {
            Printing.LabelPrinter.PrintTag(LabelPrinter == "(none)" ? "" : LabelPrinter, ShopName, "TEST-001");
        }

        private void TestReceipt()
        {
            Printing.ReceiptPrinter.PrintReceipt(
                ReceiptPrinter == "(none)" ? "" : ReceiptPrinter,
                ShopName, ShopAddress, "Test Customer",
                DateTime.Now, new[] { new Models.CartItem { SKU="TEST", Name="Test Item", NetWt=1m, Rate=5000m } },
                5000m, "Cash");
        }
    }
}
