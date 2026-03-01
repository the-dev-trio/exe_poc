using System.Windows;
using InventoryApp.Data;

namespace InventoryApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DatabaseHelper.InitializeDatabase();
            InitializeComponent();
        }
    }
}
