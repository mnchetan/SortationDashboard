using SortationDashboard.Data;
using SortationDashboard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
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
            LoadHistoryCommand = new RelayCommand(ExecuteLoadHistory);
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

        // --- Command Execution Logic ---


        // 3. Add the execution logic
        private async void ExecuteLoadHistory(object obj)
        {
            StatusBarText = "Loading history from local database...";
            EventLogs.Clear();

            try
            {
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["SortationDbConnection"].ConnectionString;
                CartonRepository repo = new SortationDashboard.Data.CartonRepository(connStr);

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
            Random random = new System.Random();
            string connStr = ConfigurationManager.ConnectionStrings["SortationDbConnection"].ConnectionString;
            CartonRepository repo = new CartonRepository(connStr);

            while (_isServerRunning)
            {
                await Task.Delay(random.Next(1000, 3000));
                if (!_isServerRunning) break;

                // Create the event
                ParsedCartonEvent newEvent = new ParsedCartonEvent
                {
                    MessageId = System.Guid.NewGuid().ToString(), // Unique ID
                    EventType = "SCAN",
                    LabelNumber = $"LBL-{random.Next(10000, 99999)}",
                    TargetChute = $"CHUTE_{random.Next(1, 5)}",
                    ProcessedTimestamp = System.DateTime.Now
                };

                // WRITE TO SQL DATABASE
                bool saved = await repo.InsertEventAsync(newEvent);

                if (saved)
                {
                    // Safely push to UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EventLogs.Add($"[DB SAVED {newEvent.ProcessedTimestamp:HH:mm:ss}] {newEvent.LabelNumber} -> {newEvent.TargetChute}");
                    });
                }
            }
        }

        private void ExecuteClearLogs(object obj)
        {
            EventLogs.Clear();
        }
    }
}