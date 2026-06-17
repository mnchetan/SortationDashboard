using System.Windows;

namespace SortationDashboard
{
    public partial class MainWindow : Window
    {
        private readonly DashboardViewModelNew _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate the ViewModel
            _viewModel = new DashboardViewModelNew();

            // Tell the UI that this ViewModel is its source of data
            DataContext = _viewModel;
        }
    }
}