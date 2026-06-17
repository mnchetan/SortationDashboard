using SortationDashboard.Data;
using SortationDashboard.Models;
using SortationDashboard.Network; // Required for AsyncPlcServer
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SortationDashboard
{
    public class DashboardViewModelNew : ObservableObject
    {
        private string _serverStatusText = "TCP Server: OFFLINE";
        private string _serverStatusColor = "#E84118";
        private string _statusBarText = "Ready.";
        private bool _isServerRunning = false;

        // 1. Keep a reference to the real TCP server
        private AsyncPlcServer _tcpServer;

        public DashboardViewModelNew()
        {
            EventLogs = new ObservableCollection<string>();

            // Initialize Commands
            StartServerCommand = new RelayCommand(ExecuteStartServer, CanStartServer);
            StopServerCommand = new RelayCommand(ExecuteStopServer, CanStopServer);
            ClearLogsCommand = new RelayCommand(ExecuteClearLogs);
            LoadHistoryCommand = new RelayCommand(ExecuteLoadHistory);
            MockPlcEventCommand = new RelayCommand(ExecuteMockPlcEvent, CanMockPlcEvent);
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
        public ICommand LoadHistoryCommand { get; }
        public ICommand MockPlcEventCommand { get; }

        // --- Command Execution Logic ---

        private bool CanMockPlcEvent(object obj) => _isServerRunning;

        private void ExecuteMockPlcEvent(object obj)
        {
            // Run the client simulation on a background thread so it doesn't freeze the UI
            Task.Run(() =>
            {
                try
                {
                    Random random = new Random();
                    string msgId = $"MSG{random.Next(10000, 99999)}";
                    string label = $"LBL-{random.Next(10000, 99999)}";
                    string targetChute = $"CHUTE_{random.Next(1, 5)}";

                    // Format: "MSG_ID|EVENT_TYPE|LABEL_NUMBER|CHUTE"
                    string payload = $"{msgId}|SCAN|{label}|{targetChute}";

                    LogToUI($"[SIMULATOR] Sending test payload: {payload}");

                    // Use the PlcSimulator class you already wrote to act as the client
                    PlcSimulator simulator = new PlcSimulator();
                    simulator.SendCartonEvent("127.0.0.1", 8080, payload);
                }
                catch (Exception ex)
                {
                    LogToUI($"[SIMULATOR ERROR] {ex.Message}");
                }
            });
        }
        // --- The Critical Dispatcher Bridge ---
        // Safely marshals updates from background TCP threads to the main WPF UI thread.
        private void LogToUI(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EventLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            });
        }

        private async void ExecuteLoadHistory(object obj)
        {
            StatusBarText = "Loading history from local database...";
            EventLogs.Clear();

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["SortationDbConnection"].ConnectionString;
                CartonRepository repo = new CartonRepository(connStr);

                List<ParsedCartonEvent> history = await repo.GetRecentEventsAsync();

                foreach (ParsedCartonEvent item in history)
                {
                    EventLogs.Add($"[HISTORY {item.ProcessedTimestamp:HH:mm:ss}] {item.LabelNumber} -> {item.TargetChute}");
                }

                StatusBarText = $"Loaded {history.Count} historical events.";
            }
            catch (Exception ex)
            {
                StatusBarText = "Database read failed.";
                EventLogs.Add($"ERROR: {ex.Message}");
            }
        }

        private bool CanStartServer(object obj) => !_isServerRunning;

        private void ExecuteStartServer(object obj)
        {
            _isServerRunning = true;
            ServerStatusText = "TCP Server: ONLINE";
            ServerStatusColor = "#4CD137";
            StatusBarText = "Listening for PLC connections on Port 8080...";

            LogToUI("Starting TCP Server Engine...");

            // 2. Instantiate the server and wire up the events
            _tcpServer = new AsyncPlcServer();

            _tcpServer.OnClientConnected += (msg) => LogToUI($"[NETWORK] {msg}");
            _tcpServer.OnMessageReceived += (msg) => LogToUI($"[PLC RAW] {msg}");
            _tcpServer.OnServerError += (msg) => LogToUI($"[ERROR] {msg}");

            // 3. Start the server on a background task
            _ = _tcpServer.StartListeningAsync(8080);
        }

        private bool CanStopServer(object obj) => _isServerRunning;

        private void ExecuteStopServer(object obj)
        {
            _isServerRunning = false;

            // 4. Stop the real server safely
            _tcpServer?.Stop();

            ServerStatusText = "TCP Server: OFFLINE";
            ServerStatusColor = "#E84118";
            StatusBarText = "Server stopped.";

            LogToUI("TCP Server shut down manually.");
        }

        private void ExecuteClearLogs(object obj)
        {
            EventLogs.Clear();
        }

        // --- The Critical Dispatcher Bridge ---

        // Safely marshals updates from background TCP threads to the main WPF UI thread.
        // This prevents cross-thread exceptions when the background network task 
        // tries to update the ObservableCollection bound to the UI.
    }
}