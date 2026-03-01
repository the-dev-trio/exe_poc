using System.Windows;
using System.Threading;
using InventoryApp.Data;

namespace InventoryApp
{
    public partial class App : Application
    {
        private Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Single-instance guard
            _mutex = new Mutex(true, "JewelleryManagementSystem_SingleInstance", out bool isNew);
            if (!isNew)
            {
                MessageBox.Show("The application is already running.", "Jewellery Manager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            DispatcherUnhandledException += (_, args) =>
            {
                var msg = args.Exception.Message.Contains("database") || args.Exception.Message.Contains("SQLite")
                    ? "Database error — the drive may have been disconnected.\n\n" + args.Exception.Message
                    : args.Exception.Message;
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Auto-backup if enabled
            if (DatabaseHelper.GetSetting("AutoBackup") == "1")
            {
                var backupPath = DatabaseHelper.GetSetting("BackupPath");
                if (!string.IsNullOrWhiteSpace(backupPath) && Directory.Exists(backupPath))
                {
                    try
                    {
                        var dest = Path.Combine(backupPath, $"jms_backup_{DateTime.Now:yyyyMMdd_HHmm}.db");
                        DatabaseHelper.BackupTo(dest);
                    }
                    catch { /* silent fail on exit */ }
                }
            }
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
