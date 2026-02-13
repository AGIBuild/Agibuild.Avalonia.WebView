## ADDED Requirements

### Requirement: Sample project structure
The sample SHALL be organized as a multi-project solution at `samples/avalonia-react/` with four projects: `AvaloniReact.Desktop` (Avalonia host), `AvaloniReact.Bridge` (shared interfaces/models/implementations), `AvaloniReact.Web` (React + Vite + TypeScript), and `AvaloniReact.Tests` (xUnit unit tests).

#### Scenario: Solution builds successfully
- **WHEN** a developer runs `dotnet build AvaloniReact.sln` from `samples/avalonia-react/`
- **THEN** all C# projects (Desktop, Bridge, Tests) SHALL compile without errors

#### Scenario: React app builds successfully
- **WHEN** a developer runs `npm install && npm run build` from `AvaloniReact.Web/`
- **THEN** the Vite build SHALL produce output in `dist/` with `index.html` and bundled assets

### Requirement: Dynamic page registry
The sample SHALL use a dynamic page registry pattern where available pages are defined in C# and queried by the React frontend via Bridge. Pages SHALL NOT be hardcoded in the React router configuration.

#### Scenario: React app loads page list from C# on startup
- **WHEN** the React app initializes and the bridge is ready
- **THEN** the app SHALL call `IAppShellService.GetPages()` to retrieve the list of available pages
- **AND** render navigation items and routes dynamically based on the returned `PageDefinition` list

#### Scenario: Adding a new page requires only registration
- **WHEN** a developer adds a new page component in React and registers a `PageDefinition` in the C# page registry
- **THEN** the new page SHALL appear in navigation without modifying router configuration or layout code

### Requirement: AppShell Bridge service
The sample SHALL expose an `IAppShellService` via `[JsExport]` that provides application metadata and page registry.

#### Scenario: GetPages returns all registered pages
- **WHEN** JavaScript calls `AppShellService.getPages()`
- **THEN** the service SHALL return a list of `PageDefinition` records containing `id`, `title`, `icon`, and `route` for each registered page

#### Scenario: GetAppInfo returns application metadata
- **WHEN** JavaScript calls `AppShellService.getAppInfo()`
- **THEN** the service SHALL return an `AppInfo` record with `name`, `version`, and `description`

### Requirement: SystemInfo Bridge service
The sample SHALL expose an `ISystemInfoService` via `[JsExport]` that provides native system information inaccessible from web content.

#### Scenario: GetSystemInfo returns platform details
- **WHEN** JavaScript calls `SystemInfoService.getSystemInfo()`
- **THEN** the service SHALL return a `SystemInfo` record containing `osName`, `osVersion`, `dotnetVersion`, `avaloniaVersion`, `machineName`, `processorCount`, `totalMemoryMb`, and `webViewEngine`

#### Scenario: GetRuntimeMetrics returns live metrics
- **WHEN** JavaScript calls `SystemInfoService.getRuntimeMetrics()`
- **THEN** the service SHALL return a `RuntimeMetrics` record containing `workingSetMb`, `gcTotalMemoryMb`, `threadCount`, and `uptimeSeconds` reflecting current process state

### Requirement: Chat Bridge service
The sample SHALL expose an `IChatService` via `[JsExport]` that demonstrates bidirectional communication with complex types.

#### Scenario: SendMessage returns a response
- **WHEN** JavaScript calls `ChatService.sendMessage()` with a `ChatRequest` containing `message` text
- **THEN** the service SHALL return a `ChatResponse` with a generated reply `message`, `timestamp`, and the original message echoed in context

#### Scenario: GetHistory returns message history
- **WHEN** JavaScript calls `ChatService.getHistory()`
- **THEN** the service SHALL return a list of `ChatMessage` records ordered by timestamp

#### Scenario: ClearHistory resets conversation
- **WHEN** JavaScript calls `ChatService.clearHistory()`
- **THEN** subsequent calls to `GetHistory()` SHALL return an empty list

### Requirement: File Bridge service
The sample SHALL expose an `IFileService` via `[JsExport]` that demonstrates native file system access through the Bridge.

#### Scenario: ListFiles returns directory contents
- **WHEN** JavaScript calls `FileService.listFiles()` with an optional `path` parameter
- **THEN** the service SHALL return a list of `FileEntry` records containing `name`, `path`, `isDirectory`, `size`, and `lastModified`

#### Scenario: ReadTextFile returns file content
- **WHEN** JavaScript calls `FileService.readTextFile()` with a `path` parameter
- **THEN** the service SHALL return the text content of the file at that path
- **AND** if the file does not exist, SHALL return an error message

#### Scenario: GetUserDocumentsPath returns a known directory
- **WHEN** JavaScript calls `FileService.getUserDocumentsPath()`
- **THEN** the service SHALL return the absolute path to the current user's Documents folder

### Requirement: Settings Bridge service
The sample SHALL expose an `ISettingsService` via `[JsExport]` that demonstrates preferences persistence and bidirectional state synchronization.

#### Scenario: GetSettings returns current settings
- **WHEN** JavaScript calls `SettingsService.getSettings()`
- **THEN** the service SHALL return an `AppSettings` record with `theme` (light/dark/system), `language`, `fontSize`, and `sidebarCollapsed`

#### Scenario: UpdateSettings persists changes
- **WHEN** JavaScript calls `SettingsService.updateSettings()` with a partial `AppSettings` object
- **THEN** the service SHALL merge the changes with existing settings and persist them
- **AND** subsequent calls to `GetSettings()` SHALL reflect the updated values

### Requirement: UI Notification service (JsImport)
The sample SHALL define an `IUiNotificationService` via `[JsImport]` that allows C# to trigger notifications in the React UI.

#### Scenario: C# triggers a toast notification in React
- **WHEN** C# code calls `proxy.ShowNotification()` with a `message` and optional `type` (info/success/warning/error)
- **THEN** the React app SHALL display a toast notification with the given message and type

### Requirement: Theme service (JsImport)
The sample SHALL define an `IThemeService` via `[JsImport]` that allows C# to trigger theme changes in the React UI.

#### Scenario: C# triggers theme change
- **WHEN** C# code calls `proxy.SetTheme()` with a theme value (light/dark)
- **THEN** the React app SHALL update its theme to match the requested value

### Requirement: SPA hosting with dev and production modes
The sample SHALL support two hosting modes for the React frontend.

#### Scenario: Dev mode with Vite HMR
- **WHEN** the Desktop app is launched in Debug configuration
- **THEN** the WebView SHALL proxy requests to `http://localhost:5173` via `SpaHostingOptions.DevServerUrl`
- **AND** React HMR SHALL work for live development

#### Scenario: Production mode with embedded resources
- **WHEN** the Desktop app is built in Release configuration
- **THEN** the Vite build output SHALL be embedded as resources in the Desktop assembly
- **AND** the WebView SHALL serve content via `app://localhost/` from embedded resources

### Requirement: Unit tests for Bridge services
All C# Bridge service implementations SHALL have unit tests using `MockBridgeService` and direct method invocation.

#### Scenario: Each service has at least one test per public method
- **WHEN** a developer runs `dotnet test` on the `AvaloniReact.Tests` project
- **THEN** all tests SHALL pass and cover every public method of every `[JsExport]` service implementation

#### Scenario: MockBridgeService integration test
- **WHEN** tests register services via `MockBridgeService.Expose<T>()`
- **THEN** `WasExposed<T>()` SHALL return true for each registered service
