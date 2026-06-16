using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SortationDashboard
{
    public class DashboardViewModel : ObservableObject
    {
        private string _serverStatusText = "TCP Server: OFFLINE";
        private string _serverStatusColor = "#E84118";
        private string _statusBarText = "Ready.";
        private bool _isServerRunning = false; // Tracks state for enabling/disabling buttons

        public DashboardViewModel()
        {
            EventLogs = new ObservableCollection<string>();

            // Initialize Commands
            StartServerCommand = new RelayCommand(ExecuteStartServer, CanStartServer);
            StopServerCommand = new RelayCommand(ExecuteStopServer, CanStopServer);
            ClearLogsCommand = new RelayCommand(ExecuteClearLogs);
        }

        public ObservableCollection<string> EventLogs { get; }

        // --- Properties ---
        public string ServerStatusText
        {
            get => _serverStatusText;
            set => SetProperty(ref _serverStatusText, value);
        }

        public string ServerStatusColor
        {
            get => _serverStatusColor;
            set => SetProperty(ref _serverStatusColor, value);
        }

        public string StatusBarText
        {
            get => _statusBarText;
            set => SetProperty(ref _statusBarText, value);
        }

        // --- Commands ---
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand ClearLogsCommand { get; }

        // --- Command Execution Logic ---

        private bool CanStartServer(object obj) => !_isServerRunning;

        private void ExecuteStartServer(object obj)
        {
            _isServerRunning = true;
            ServerStatusText = "TCP Server: ONLINE";
            ServerStatusColor = "#4CD137";
            StatusBarText = "Listening for PLC connections on Port 8080...";

            EventLogs.Add($"[{DateTime.Now:HH:mm:ss}] TCP Server started. Awaiting socket connections.");

            // Start the background listening process
            _ = SimulateIncomingSocketDataAsync();
        }

        private bool CanStopServer(object obj) => _isServerRunning;

        private void ExecuteStopServer(object obj)
        {
            _isServerRunning = false;
            ServerStatusText = "TCP Server: OFFLINE";
            ServerStatusColor = "#E84118";
            StatusBarText = "Server stopped.";
            EventLogs.Add($"[{DateTime.Now:HH:mm:ss}] TCP Server shut down manually.");
        }

        // --- The Critical Dispatcher Implementation ---

        private async Task SimulateIncomingSocketDataAsync()
        {
            Random random = new Random();

            // This loop runs on a BACKGROUND thread, keeping the UI perfectly responsive.
            while (_isServerRunning)
            {
                // Simulate the wait time between physical cartons rolling down a conveyor belt (1 to 3 seconds)
                await Task.Delay(random.Next(1000, 3000));

                if (!_isServerRunning) break; // Exit if the user clicked Stop

                // Generate the mock TCP payload
                string mockTcpMessage = $"[{DateTime.Now:HH:mm:ss}] Carton Scanned: LBL-{random.Next(10000, 99999)} - Route: Chute {random.Next(1, 5)}";

                // CRITICAL: We cannot just call EventLogs.Add() here. We must use the Dispatcher.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EventLogs.Add(mockTcpMessage);

                    // Optional: Keep the list from growing infinitely in memory
                    if (EventLogs.Count > 100)
                    {
                        EventLogs.RemoveAt(0);
                    }
                });
            }
        }

        private void ExecuteClearLogs(object obj)
        {
            EventLogs.Clear();
        }
    }
}