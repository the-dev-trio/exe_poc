using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Printing;
using System.IO;
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

        private decimal _gstRate = 3.0m;
        public decimal GSTRate { get => _gstRate; set => SetProperty(ref _gstRate, value); }

        private decimal _makingChargeDefault = 0m;
        public decimal MakingChargeDefault { get => _makingChargeDefault; set => SetProperty(ref _makingChargeDefault, value); }

        private string _labelPrinter = string.Empty;
        public string LabelPrinter { get => _labelPrinter; set => SetProperty(ref _labelPrinter, value); }

        private string _receiptPrinter = string.Empty;
        public string ReceiptPrinter { get => _receiptPrinter; set => SetProperty(ref _receiptPrinter, value); }

        private bool _autoBackup;
        public bool AutoBackup { get => _autoBackup; set => SetProperty(ref _autoBackup, value); }

        private string _backupPath = string.Empty;
        public string BackupPath { get => _backupPath; set => SetProperty(ref _backupPath, value); }

        public ICommand SaveCommand               { get; }
        public ICommand BrowseBackupPathCommand   { get; }
        public ICommand BackupNowCommand          { get; }
        public ICommand TestLabelPrinterCommand   { get; }
        public ICommand TestReceiptPrinterCommand { get; }

        public SettingsViewModel(Action onSaved)
        {
            _onSaved = onSaved;
            SaveCommand               = new RelayCommand(_ => Save());
            BrowseBackupPathCommand   = new RelayCommand(_ => BrowseBackup());
            BackupNowCommand          = new RelayCommand(_ => BackupNow());
            TestLabelPrinterCommand   = new RelayCommand(_ => TestLabel());
            TestReceiptPrinterCommand = new RelayCommand(_ => TestReceipt());
            Load();
        }

        private void Load()
        {
            ShopName      = DatabaseHelper.GetSetting("ShopName", "My Jewellery");
            ShopAddress   = DatabaseHelper.GetSetting("ShopAddress", "");
            GSTNumber     = DatabaseHelper.GetSetting("GSTNumber", "");
            GSTRate       = decimal.TryParse(DatabaseHelper.GetSetting("GSTRate", "3.0"), out var gr) ? gr : 3.0m;
            MakingChargeDefault = decimal.TryParse(DatabaseHelper.GetSetting("MakingChargeDefault", "0"), out var mc) ? mc : 0m;
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
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                InstalledPrinters.Add(printer);
        }

        private void Save()
        {
            DatabaseHelper.SetSetting("ShopName",      ShopName);
            DatabaseHelper.SetSetting("ShopAddress",   ShopAddress);
            DatabaseHelper.SetSetting("GSTNumber",     GSTNumber);
            DatabaseHelper.SetSetting("GSTRate",       GSTRate.ToString());
            DatabaseHelper.SetSetting("MakingChargeDefault", MakingChargeDefault.ToString());
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

        private void BackupNow()
        {
            try
            {
                var targetFolder = BackupPath;
                if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
                {
                    using var dlg = new System.Windows.Forms.FolderBrowserDialog { Description = "Select Backup Folder" };
                    if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                    targetFolder = dlg.SelectedPath;
                    BackupPath = targetFolder;
                }

                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                var fileName = $"jms_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var dest = Path.Combine(targetFolder, fileName);
                DatabaseHelper.BackupTo(dest);

                System.Windows.MessageBox.Show($"Backup created:\n{dest}", "Backup",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Backup failed:\n{ex.Message}", "Backup",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void TestLabel()
        {
            Printing.LabelPrinter.PrintTag(LabelPrinter == "(none)" ? "" : LabelPrinter, ShopName, "TEST-001");
        }

        private void TestReceipt()
        {
            var item = new Models.CartItem { SKU = "TEST", Name = "Test Item", NetWt = 1m, Rate = 5000m };
            decimal subtotal = item.FinalAmount;
            decimal cgst     = Math.Round(subtotal * 0.015m, 2);
            decimal sgst     = cgst;
            decimal grandTotal = subtotal + cgst + sgst;
            Printing.ReceiptPrinter.PrintReceipt(
                ReceiptPrinter == "(none)" ? "" : ReceiptPrinter,
                ShopName, ShopAddress, GSTNumber,
                "Test Customer", DateTime.Now,
                new[] { item },
                subtotal, 0m, cgst, sgst, grandTotal, "Cash");
        }
    }
}
