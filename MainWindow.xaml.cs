using System.Windows;

namespace SortationDashboard
{
    public partial class MainWindow : Window
    {
        private readonly DashboardViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate the ViewModel
            _viewModel = new DashboardViewModel();

            // Tell the UI that this ViewModel is its source of data
            DataContext = _viewModel;
        }
    }
}