using System.Windows;
using System.Windows.Input;
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
