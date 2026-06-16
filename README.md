# Carton Sortation Event Processor & Dashboard

A high-throughput, low-latency Windows desktop application designed to interface with warehouse automation hardware (PLCs, sortation chutes, and barcode scanners). Built with **.NET Framework 4.8** and **WPF**, this project demonstrates robust asynchronous socket programming, thread-safe UI rendering, and strict MVVM architecture.

## 🚀 Key Features

* **High-Volume TCP Event Processing:** Utilizes `async/await` and non-blocking I/O to handle multiple concurrent hardware connections without tying up CPU threads.
* **Resilient Networking:** Implements custom `ResilientTcpClient` logic featuring exponential backoff to handle noisy hardware networks and dropped connections gracefully.
* **Idempotent Operations:** Includes a thread-safe caching mechanism (`ConcurrentDictionary`) to detect and discard duplicate network messages, ensuring consistent database states.
* **Responsive WPF UI:** Built on a decoupled MVVM (Model-View-ViewModel) architecture.
* **Thread-Safe Rendering:** Strict adherence to `Dispatcher` boundaries, safely marshaling high-speed background socket data back to the primary UI thread.

## 🛠️ Technology Stack

* **Language:** C#
* **Framework:** .NET Framework 4.8
* **UI Presentation:** Windows Presentation Foundation (WPF / XAML)
* **Networking:** `System.Net.Sockets` (Raw TCP/IP Streams)
* **Architecture:** MVVM, Event-Driven

## 📂 Project Structure

The solution is divided into distinct logical layers to maintain separation of concerns:

```text
├── App.xaml                  # Application entry point and global resources
├── MainWindow.xaml           # The visual tree and layout (View)
├── MainWindow.xaml.cs        # Code-behind (Strictly limited to DataContext initialization)
│
├── ViewModels/
│   ├── ObservableObject.cs   # INotifyPropertyChanged base implementation
│   ├── RelayCommand.cs       # ICommand implementation for UI actions
│   └── DashboardViewModel.cs # Presentation logic and UI state management
│
├── Network/
│   ├── AsyncPlcServer.cs     # Asynchronous TCP Listener for incoming hardware events
│   └── ResilientTcpClient.cs # Wrapper for outbound connections with exponential backoff
│
└── Integration/
    └── CartonEventProcessor.cs # Protocol parsing and Idempotency caching
