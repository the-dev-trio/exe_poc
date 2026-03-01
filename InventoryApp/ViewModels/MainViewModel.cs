using System.Windows.Input;

namespace InventoryApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowInventoryCommand { get; }

        public MainViewModel()
        {
            // Set initial view
            _currentView = new DashboardViewModel();
            
            ShowDashboardCommand = new RelayCommand(_ => CurrentView = new DashboardViewModel());
            ShowInventoryCommand = new RelayCommand(_ => CurrentView = new InventoryViewModel());
        }
    }
}
