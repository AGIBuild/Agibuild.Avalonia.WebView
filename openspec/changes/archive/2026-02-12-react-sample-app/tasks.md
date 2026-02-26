## 1. Project Scaffolding

- [x] 1.1 Create `samples/avalonia-react/` directory structure with `AvaloniReact.sln`, four project directories (Desktop, Bridge, Web, Tests)
- [x] 1.2 Create `AvaloniReact.Bridge.csproj` — references `Agibuild.Fulora.Core` + `Bridge.Generator` (analyzer); define all shared models in `Models/`
- [x] 1.3 Create `AvaloniReact.Desktop.csproj` — Avalonia Desktop app with `Agibuild.Fulora` reference, `Program.cs`, `App.axaml`, `MainWindow.axaml`
- [x] 1.4 Create `AvaloniReact.Tests.csproj` — xUnit test project referencing `AvaloniReact.Bridge` + `Agibuild.Fulora.Testing`
- [x] 1.5 Create `AvaloniReact.Web/` — `npm init`, install React 19 + Vite 6 + TypeScript 5 + Tailwind CSS 4 + React Router 7, configure `vite.config.ts` and `tsconfig.json`

## 2. Bridge Interfaces & Models

- [x] 2.1 Define `IAppShellService` (`[JsExport]`) with `GetPages()` and `GetAppInfo()`, plus `PageDefinition` and `AppInfo` model records
- [x] 2.2 Define `ISystemInfoService` (`[JsExport]`) with `GetSystemInfo()` and `GetRuntimeMetrics()`, plus `SystemInfo` and `RuntimeMetrics` model records
- [x] 2.3 Define `IChatService` (`[JsExport]`) with `SendMessage()`, `GetHistory()`, `ClearHistory()`, plus `ChatRequest`, `ChatResponse`, `ChatMessage` model records
- [x] 2.4 Define `IFileService` (`[JsExport]`) with `ListFiles()`, `ReadTextFile()`, `GetUserDocumentsPath()`, plus `FileEntry` model record
- [x] 2.5 Define `ISettingsService` (`[JsExport]`) with `GetSettings()` and `UpdateSettings()`, plus `AppSettings` model record
- [x] 2.6 Define `IUiNotificationService` (`[JsImport]`) with `ShowNotification(message, type)`
- [x] 2.7 Define `IThemeService` (`[JsImport]`) with `SetTheme(theme)`

## 3. Bridge Service Implementations

- [x] 3.1 Implement `AppShellService` — configurable page registry with default 4 pages (Dashboard, Chat, Files, Settings)
- [x] 3.2 Implement `SystemInfoService` — read OS/runtime info via `Environment`, `RuntimeInformation`, `Process.GetCurrentProcess()`
- [x] 3.3 Implement `ChatService` — in-memory message history, echo-style response generation with simulated delay
- [x] 3.4 Implement `FileService` — directory listing via `Directory.GetFileSystemEntries`, file read via `File.ReadAllTextAsync`, documents path via `Environment.GetFolderPath`
- [x] 3.5 Implement `SettingsService` — in-memory settings store with JSON file persistence to app data directory

## 4. Avalonia Desktop Host

- [x] 4.1 Implement `MainWindow.axaml.cs` — SPA hosting setup (`#if DEBUG` dev proxy / `#else` embedded resources), register all Bridge services via `Bridge.Expose<T>()`
- [x] 4.2 Configure `app.manifest` and `Program.cs` with `UseAgibuildWebView()` initialization
- [x] 4.3 Add MSBuild `BeforeBuild` target for Release mode — run `npm run build` and embed `dist/` as `EmbeddedResource`

## 5. React App — Foundation

- [x] 5.1 Create `src/main.tsx` entry point, `src/App.tsx` with React Router and app shell layout (sidebar + content area)
- [x] 5.2 Create `src/bridge/services.ts` — typed service proxies using `@agibuild/bridge` (`bridge.getService<T>()`) for all 5 JsExport services
- [x] 5.3 Create `src/hooks/useBridge.ts` — custom hook for bridge readiness, and `src/hooks/usePageRegistry.ts` — fetches page list from `IAppShellService`
- [x] 5.4 Create `src/components/Layout.tsx` — responsive sidebar layout with dynamic nav items from page registry, theme toggle, notification toast area
- [x] 5.5 Configure Tailwind CSS 4 with dark mode support (class strategy), define color palette and typography

## 6. React App — Pages

- [x] 6.1 Create `src/pages/Dashboard.tsx` — system info cards (OS, .NET, memory, etc.) with auto-refresh runtime metrics, styled with Tailwind
- [x] 6.2 Create `src/pages/Chat.tsx` — message input + chat history, typewriter animation for responses, clear history button
- [x] 6.3 Create `src/pages/Files.tsx` — file browser with directory listing table, text file preview panel, breadcrumb navigation
- [x] 6.4 Create `src/pages/Settings.tsx` — settings form (theme, font size, sidebar collapse toggle), save button, bridge notification on save

## 7. React App — JsImport Handlers

- [x] 7.1 Register `IUiNotificationService.showNotification` handler — display toast notifications with auto-dismiss
- [x] 7.2 Register `IThemeService.setTheme` handler — update document class for dark/light mode

## 8. Unit Tests

- [x] 8.1 Write `AppShellServiceTests` — verify `GetPages()` returns expected page list, `GetAppInfo()` returns correct metadata
- [x] 8.2 Write `SystemInfoServiceTests` — verify `GetSystemInfo()` returns non-null fields, `GetRuntimeMetrics()` returns positive values
- [x] 8.3 Write `ChatServiceTests` — verify `SendMessage()` returns response, `GetHistory()` tracks messages, `ClearHistory()` resets
- [x] 8.4 Write `FileServiceTests` — verify `ListFiles()` with temp directory, `ReadTextFile()` with temp file, `GetUserDocumentsPath()` returns non-empty
- [x] 8.5 Write `SettingsServiceTests` — verify `GetSettings()` defaults, `UpdateSettings()` merges and persists
- [x] 8.6 Write `MockBridgeIntegrationTests` — verify all services can be registered via `MockBridgeService.Expose<T>()`

## 9. Polish & Validation

- [x] 9.1 Verify dev mode workflow: `cd AvaloniReact.Web && npm run dev`, then `dotnet run --project AvaloniReact.Desktop` — pages load, Bridge calls work, HMR updates reflect
- [x] 9.2 Verify production mode: `dotnet run --project AvaloniReact.Desktop -c Release` — embedded resources serve correctly via `app://`
- [x] 9.3 Review UI polish — responsive layout, dark/light mode, loading states, error states, empty states
- [x] 9.4 Run `dotnet test AvaloniReact.Tests` — all tests pass
