using System.Windows;
using InventoryApp.Data;

namespace InventoryApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
        }
    }
}
