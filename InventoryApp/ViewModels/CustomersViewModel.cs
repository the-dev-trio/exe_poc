using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using InventoryApp.Data;
using InventoryApp.Models;

namespace InventoryApp.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private ObservableCollection<Customer> _allCustomers = new();
        private ICollectionView _customersView;
        private string _searchQuery = string.Empty;

        public ICollectionView CustomersView
        {
            get => _customersView;
            set => SetProperty(ref _customersView, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    CustomersView.Refresh();
                }
            }
        }

        public CustomersViewModel()
        {
            LoadData();
        }

        private void LoadData()
        {
            _allCustomers.Clear();
            var customers = DatabaseHelper.GetCustomersWithLastPurchase();
            foreach (var c in customers)
            {
                _allCustomers.Add(c);
            }

            _customersView = CollectionViewSource.GetDefaultView(_allCustomers);
            _customersView.Filter = FilterCustomers;
            OnPropertyChanged(nameof(CustomersView));
        }

        private bool FilterCustomers(object obj)
        {
            if (obj is not Customer customer) return false;
            if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

            var q = SearchQuery.ToLower();
            return customer.Name.ToLower().Contains(q) ||
                   customer.Mobile.Contains(q);
        }
    }
}
