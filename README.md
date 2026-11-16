# SmartDS Auto Launcher

**SmartDS Auto Launcher** is a modern, reliable Windows desktop application built with **C#, WPF, and .NET 8** designed to automatically launch, monitor, and manage the **PostgreSQL pgAdmin 4**, **Node.js Backend**, and **Next.js Frontend** stack in sequence whenever a Windows user logs in.

---

## 🌟 Key Features

- **Automated 3-Step Startup Sequence**:
  1. **PostgreSQL pgAdmin 4**: Launched via Windows shortcut (`C:\ProgramData\Microsoft\Windows\Start Menu\Programs\PostgreSQL 18\pgAdmin 4.lnk`), shell-executed with an interactive desktop window. Configurable default delay: **10 seconds**.
  2. **Node.js Backend API**: Launched via `node.exe` with arguments `dist\server.js`, working directory `C:\SmartDS\backend`, environment `NODE_ENV=production`, and asynchronous log capture to `C:\SmartDS\logs\backend.log`. Configurable default delay: **15 seconds** with TCP readiness health check on `localhost:3000`.
  3. **Next.js Frontend**: Launched via `node.exe` with arguments `.next\standalone\server.js`, working directory `C:\SmartDS\frontend`, environment `PORT=3001`, `HOSTNAME=0.0.0.0`, `NODE_ENV=production`, and log capture to `C:\SmartDS\logs\frontend.log`.
- **Fluent-Inspired Dark UI**: Polished WPF dark theme with rounded cards, subtle shadows, status indicators, and responsive live metrics.
- **Configurable Sequence Editor**: Add, delete, reorder (Move Up/Down), enable/disable, and edit parameters (paths, arguments, working directories, environment variables, delays, failure policies, and readiness checks).
- **Safe Process Management**: Tracks application-owned process IDs (`Process.Id`). Stopping services safely terminates only the child process tree via `taskkill /pid /t /f`, completely avoiding termination of unrelated `node.exe` or external processes.
- **Windows Auto-Start**: Per-user startup registration via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. No administrator privileges required. Supports configurable startup delay (default **20 seconds**) and start-minimized.
- **System Tray Notification Area**: Minimizes to system tray with a right-click context menu (Start All, Stop All, Open Dashboard, Exit) and double-click to restore.
- **Live Log Viewer & Rotation**: Real-time scrolling log viewer with keyword filtering, minimum log level filter, clipboard copy, and folder access.
- **Settings Persistence**: Stored safely in `%LocalAppData%\SmartDSAutoLauncher\settings.json`.

---

## 🏗️ Architecture & Project Structure

The project follows the standard **Model-View-ViewModel (MVVM)** architecture with dependency injection:

```text
d:\GitHub_Project\SmartDS_Launcher\
├── SmartDSAutoLauncher.sln                  # Visual Studio 2022 Solution
├── SmartDSAutoLauncher/
│   ├── SmartDSAutoLauncher.csproj          # .NET 8 WPF Windows Project
│   ├── app.manifest                        # DPI-aware, asInvoker manifest
│   ├── App.xaml / App.xaml.cs              # DI container, Tray icon, App lifecycle
│   ├── Helpers/
│   │   ├── AsyncRelayCommand.cs            # Asynchronous command implementation
│   │   ├── RelayCommand.cs                 # Synchronous command implementation
│   │   ├── ObservableObject.cs             # INotifyPropertyChanged base
│   │   ├── Converters.cs                   # WPF XAML value converters
│   │   └── WindowsStartupHelper.cs         # CLI args & node detection helper
│   ├── Models/
│   │   ├── AppSettings.cs                  # User & application preferences model
│   │   ├── ServiceConfig.cs                # Step definition & execution model
│   │   ├── ServiceStatus.cs                # State enum & runtime instance tracker
│   │   ├── LaunchStep.cs                   # Step progression & countdown model
│   │   ├── ReadinessCheckConfig.cs         # TCP & HTTP readiness checks
│   │   └── LogEntry.cs                     # Log message model with level & timestamp
│   ├── Services/
│   │   ├── ISettingsService.cs             # JSON configuration persistence
│   │   ├── SettingsService.cs              # Default services factory & loader
│   │   ├── IProcessManager.cs              # Safe process launcher & tree stopper
│   │   ├── ProcessManager.cs               # Process start, stdout/stderr capture
│   │   ├── ILaunchOrchestrator.cs          # Step sequencer, delay & retry logic
│   │   ├── LaunchOrchestrator.cs           # Async startup & restart orchestration
│   │   ├── IReadinessCheckService.cs       # TCP & HTTP port health checking
│   │   ├── ReadinessCheckService.cs        # TcpClient & HttpClient health probes
│   │   ├── IStartupRegistrationService.cs  # Windows HKCU Run registry manager
│   │   ├── StartupRegistrationService.cs   # Register/Unregister user login startup
│   │   ├── INotificationService.cs         # Windows tray balloon notification service
│   │   ├── NotificationService.cs          # Balloon tip presenter
│   │   └── LogService.cs                   # Thread-safe circular memory log buffer
│   ├── ViewModels/
│   │   ├── MainViewModel.cs                # Master shell ViewModel & navigation
│   │   ├── DashboardViewModel.cs           # Live metrics & active countdown
│   │   ├── ServiceInstanceViewModel.cs     # Per-service card controls & status
│   │   ├── SequenceViewModel.cs            # Step editor, browse & reordering
│   │   ├── SettingsViewModel.cs            # Windows auto-start & port settings
│   │   ├── LogsViewModel.cs                # Live log search & level filtering
│   │   └── LogEntryViewModel.cs            # Single log line ViewModel
│   ├── Views/
│   │   ├── MainWindow.xaml / .cs           # Main window frame & sidebar navigation
│   │   ├── DashboardView.xaml / .cs        # Dashboard overview, cards & controls
│   │   ├── SequenceView.xaml / .cs         # Sequence editor & step properties
│   │   ├── SettingsView.xaml / .cs         # Settings view
│   │   ├── LogsView.xaml / .cs             # Log terminal viewer
│   │   └── AboutView.xaml / .cs            # About & diagnostic information
│   ├── Themes/
│   │   ├── DarkTheme.xaml                  # Fluent dark mode color palette
│   │   └── LightTheme.xaml                 # Modern light mode color palette
│   ├── Styles/
│   │   └── CommonStyles.xaml               # Rounded buttons, cards, typography
│   └── Resources/
│       └── app.ico                         # Windows application icon
├── Installer/
│   ├── SmartDS_Auto_Launcher.iss           # Complete Inno Setup 6 installer script
│   └── Build_Installer.ps1                 # Automated publish and packaging script
└── publish/                                # Compiled release binaries for distribution
```

---

## ⚡ Default Startup Sequence

When launched (or when the computer boots and the user logs in), the launcher initiates the following sequence:

| Step | Service | Target Path / Command | Working Directory | Delay / Check | Failure Policy |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **1** | **pgAdmin 4** | `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\PostgreSQL 18\pgAdmin 4.lnk` | Shell Executed | **10s** delay | Continue |
| **2** | **Node.js Backend** | `node.exe dist\server.js`<br>`NODE_ENV=production` | `C:\SmartDS\backend` | **15s** delay + TCP port `3000` readiness check | Stop sequence on failure |
| **3** | **Next.js Frontend** | `node.exe .next\standalone\server.js`<br>`PORT=3001 HOSTNAME=0.0.0.0` | `C:\SmartDS\frontend` | **0s** (Final step) + TCP port `3001` readiness check | Continue |

*Note: All paths, ports, timeouts, delays, and failure policies can be edited in the **Startup Sequence** tab in the UI.*

---

## 💡 User-Login Startup vs. Boot-Time Windows Services

### Why User-Login Startup?
- **Interactive GUI Applications**: `pgAdmin 4` is a graphical desktop application. Windows Services (running in Session 0) cannot interact with or render windows on the user desktop.
- **User Environment & Credentials**: Node.js and user desktop environments execute within the authenticated user session.
- **Power On Behavior**: When the user powers on the computer and logs in, Windows automatically executes `SmartDSAutoLauncher.exe --minimized` via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Initial Delay (20s default)**: Ensures network adapters, Windows background services, and PostgreSQL server have fully initialized before launching the sequence.

---

## 🛠️ How to Build and Run in Visual Studio 2022

### Prerequisites
- Windows 10 or Windows 11 (x64)
- Visual Studio 2022 (v17.8 or later) with **.NET Desktop Development** workload installed
- .NET 8.0 SDK

### Steps to Build
1. Open `d:\GitHub_Project\SmartDS_Launcher\SmartDSAutoLauncher.sln` in Visual Studio 2022.
2. Select **Release** or **Debug** and **x64** or **Any CPU**.
3. Press **F5** to build and run the application.

### Command Line Build
```powershell
# Restore and build the solution
dotnet build "SmartDSAutoLauncher.sln" -c Release

# Run the launcher
dotnet run --project "SmartDSAutoLauncher\SmartDSAutoLauncher.csproj"
```

---

## 📦 Creating the Installer (`SmartDS_Auto_Launcher_Setup.exe`)

An Inno Setup 6 script is provided in `Installer\SmartDS_Auto_Launcher.iss`.

### Method 1: Using the PowerShell Automation Script
Run the automated packaging script from PowerShell:
```powershell
cd d:\GitHub_Project\SmartDS_Launcher\Installer
.\Build_Installer.ps1
```
The script will:
1. Publish the application binaries into `d:\GitHub_Project\SmartDS_Launcher\publish\`
2. Invoke Inno Setup Compiler (`ISCC.exe`)
3. Output `SmartDS_Auto_Launcher_Setup.exe` into `d:\GitHub_Project\SmartDS_Launcher\Output\`

### Method 2: Using Inno Setup GUI
1. Download and install [Inno Setup 6](https://jrsoftware.org/isdl.php).
2. Publish the release build:
   ```powershell
   dotnet publish SmartDSAutoLauncher\SmartDSAutoLauncher.csproj -c Release -o publish
   ```
3. Open `Installer\SmartDS_Auto_Launcher.iss` in Inno Setup.
4. Click **Build -> Compile** (or press **F9**).
5. The installer `SmartDS_Auto_Launcher_Setup.exe` is created in `Output\`.

---

## 🧪 Testing & Verification Guide

### 1. Test Windows Auto-Start
1. In the SmartDS Auto Launcher UI, go to **Settings**.
2. Enable **Start automatically when I log in to Windows** and click **Save Settings**.
3. Verify the registry key exists using PowerShell:
   ```powershell
   Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "SmartDSAutoLauncher"
   ```
4. Sign out of Windows and sign back in (or restart the computer).
5. Verify that SmartDS Auto Launcher starts minimized to the system tray, waits the configured initial delay (20 seconds), and initiates the startup sequence.

### 2. Safe Process Management Verification
1. Click **Start All** to launch the services.
2. Open Task Manager and verify the processes are running under your user session.
3. Click **Stop All** or **Stop** on a single service.
4. Verify only the application-owned PID and its immediate process tree are terminated. Any existing, independent `node.exe` or `postgres.exe` instances are untouched.

### 3. Safe Uninstallation Verification
1. Run `Output\SmartDS_Auto_Launcher_Setup.exe` to install.
2. Verify shortcuts are created in the Start Menu and Desktop.
3. Uninstall via **Settings -> Apps -> Installed Apps -> SmartDS Auto Launcher -> Uninstall**.
4. The uninstaller removes:
   - Program files in `{autopf}\SmartDS Auto Launcher`
   - HKCU Run auto-start registry entry
   - Start menu and desktop shortcuts
5. The uninstaller asks whether you wish to remove your user settings in `%LocalAppData%\SmartDSAutoLauncher`.
6. **Application files in `C:\SmartDS` are completely preserved and never modified or deleted.**
