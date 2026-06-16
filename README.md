# Carton Sortation Event Processor & Dashboard

A high-throughput, low-latency Windows desktop application designed to interface with warehouse automation hardware (PLCs, sortation chutes, and barcode scanners).

Built with **.NET Framework 4.8** and **WPF**, this project demonstrates robust asynchronous socket programming, thread-safe UI rendering, strict MVVM architecture, and automated database provisioning.

---

## 🚀 Key Features

### High-Volume TCP Event Processing
Utilizes `async/await` and non-blocking I/O to handle multiple concurrent hardware connections without tying up CPU threads.

### Resilient Networking
Implements custom `ResilientTcpClient` logic featuring exponential backoff to handle noisy hardware networks and dropped connections gracefully.

### Idempotent Operations
Includes a thread-safe caching mechanism (`ConcurrentDictionary`) to detect and discard duplicate network messages, ensuring consistent database states.

### Self-Provisioning Database
Implements a "Code-First" approach using raw ADO.NET. On startup, the application checks for the SQL Server database and automatically generates the physical `.mdf`/`.ldf` files and schema tables if they do not exist.

### Responsive WPF UI
Built on a decoupled **MVVM (Model-View-ViewModel)** architecture.

### Thread-Safe Rendering
Strict adherence to Dispatcher boundaries, safely marshaling high-speed background socket data back to the primary UI thread.

---

## 🛠️ Technology Stack

| Category | Technology |
|-----------|------------|
| Language | C# |
| Framework | .NET Framework 4.8 |
| UI Presentation | Windows Presentation Foundation (WPF/XAML) |
| Networking | System.Net.Sockets (Raw TCP/IP Streams) |
| Database | SQL Server LocalDB, ADO.NET (`System.Data.SqlClient`) |
| Architecture | MVVM, Event-Driven, Code-First Provisioning |

---

## 📂 Project Structure

```text
├── App.config
│   └── Connection strings and framework configuration
│
├── App.xaml
│   └── Application entry point and global resources
│
├── MainWindow.xaml
│   └── Visual tree and application layout (View)
│
├── ViewModels/
│   ├── ObservableObject.cs
│   │   └── INotifyPropertyChanged base implementation
│   │
│   ├── RelayCommand.cs
│   │   └── ICommand implementation for UI actions
│   │
│   └── DashboardViewModel.cs
│       └── Presentation logic and UI state management
│
├── Network/
│   ├── AsyncPlcServer.cs
│   │   └── Asynchronous TCP listener for incoming hardware events
│   │
│   └── ResilientTcpClient.cs
│       └── Outbound TCP connections with exponential backoff
│
├── Data/
│   └── CartonRepository.cs
│       └── ADO.NET operations, SQL execution, and auto-provisioning
│
├── Models/
│   └── ParsedCartonEvent.cs
│       └── Flat data structure optimized for database insertion
│
└── Integration/
    └── CartonEventProcessor.cs
        └── Protocol parsing and idempotency caching
```

---

## ⚙️ Getting Started

### Prerequisites

- Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- SQL Server Express LocalDB
  - Installed by default with Visual Studio Data workloads
- Windows 10 or Windows 11

---

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/YourUsername/YourRepositoryName.git
```

#### 2. Open the Solution

Open the `.sln` file in Visual Studio.

#### 3. Build the Project

```text
Ctrl + Shift + B
```

or select:

```text
Build → Build Solution
```

#### 4. Run the Application

```text
F5
```

or select:

```text
Debug → Start Debugging
```

---

## 🔒 Security Note

On first execution, **Windows Defender Firewall** or **Smart App Control** may prompt you to block the application because it opens a local TCP listener (default port: `8080`).

To enable hardware communication:

1. Click **Allow Access**
2. Ensure the application is permitted on private networks

Failure to allow access will prevent the socket server from receiving hardware events.

---

## 🗄️ First Run Behavior

During the first launch, you may notice a brief startup delay.

This occurs because the application:

1. Connects to SQL Server LocalDB
2. Verifies database existence
3. Creates the database files (`.mdf`, `.ldf`) if necessary
4. Generates the required schema objects

Subsequent launches start immediately.

---

# 🧠 Architectural Highlights

## Code-First Database Provisioning

To avoid merge conflicts and file-locking issues associated with version-controlling binary SQL Server database files, the application dynamically provisions its database at runtime.

The `CartonRepository`:

- Connects to the SQL Server `master` database
- Checks for the existence of `SortationDB`
- Creates the physical database files when required
- Generates schema objects using dynamic SQL

### Benefits

- No database files stored in source control
- Zero setup for new developers
- Fully clone-and-run experience
- Consistent local environments

---

## The Dispatcher & Thread Affinity

WPF enforces strict thread affinity, meaning UI components may only be accessed by the thread that created them.

This project demonstrates safe cross-thread communication using:

```csharp
Application.Current.Dispatcher.Invoke(...)
```

Background socket-processing threads marshal updates to the UI thread, preventing:

```text
InvalidOperationException:
"The calling thread cannot access this object because a different thread owns it."
```

### Benefits

- Stable UI updates under heavy load
- No cross-thread access violations
- Predictable rendering behavior

---

## Protocol Parsing & High-Performance Data Access

The system consumes raw TCP byte streams through `NetworkStream` instances.

Incoming payloads are:

1. Received asynchronously
2. Parsed from pipe-delimited protocol messages
3. Converted into lightweight C# objects
4. Persisted using parameterized SQL commands

### Why ADO.NET Instead of an ORM?

For maximum throughput, the application bypasses heavy ORM frameworks such as Entity Framework and instead relies on pure ADO.NET.

Advantages include:

- Lower memory overhead
- Reduced object tracking costs
- Faster insert performance
- Direct SQL control
- Native SQL Server connection pooling

This design is particularly well suited for warehouse automation workloads where thousands of events may arrive per minute.

---

## 📈 Design Goals

- High throughput
- Low latency
- Fault tolerance
- Idempotent processing
- Thread-safe UI updates
- Minimal deployment friction
- Zero-touch database provisioning

---

## 📄 License

This project is provided for educational and demonstration purposes.

Add your preferred license (MIT, Apache 2.0, GPL, etc.) before publishing publicly.

---

## 🤝 Contributing

Contributions, suggestions, and improvements are welcome.

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Submit a pull request

---

## 📬 Contact

For questions, issues, or feature requests, please open a GitHub Issue in the repository.