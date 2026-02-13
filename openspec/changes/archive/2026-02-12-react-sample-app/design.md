## Context

ROADMAP Phase 2 deliverable 2.6 requires a production-grade Avalonia + React sample. The existing `samples/minimal-hybrid/` is a single `index.html` with vanilla JS — useful as a quick reference but insufficient as a developer adoption vehicle.

The framework already provides all building blocks: `[JsExport]`/`[JsImport]` bridge, SPA hosting with `app://` scheme, Vite HMR dev proxy, `@agibuild/bridge` npm package, and `MockBridgeService` for testing. This sample composes them into a realistic, extensible hybrid application.

Current state:
- `packages/bridge/` — `@agibuild/bridge` npm package (v0.1.0)
- `templates/agibuild-hybrid/` — `dotnet new` template (vanilla JS only)
- `samples/minimal-hybrid/` — single-file vanilla JS demo

## Goals / Non-Goals

**Goals:**
- Demonstrate the full Agibuild hybrid workflow: C# host + React SPA + typed Bridge
- Showcase all Bridge patterns: `[JsExport]`, `[JsImport]`, complex types, bidirectional communication
- Provide a configurable, extensible page architecture (page registry, not hardcoded routes)
- Serve as a reference architecture for users building real hybrid apps
- Include unit tests for all C# Bridge services using `MockBridgeService`
- Support both dev mode (Vite HMR) and production mode (embedded resources)

**Non-Goals:**
- Not a reusable component library or design system
- Not a mobile sample (Desktop only — mobile is deliverable 2.6b if needed)
- No changes to the core framework APIs
- No real external service integrations (AI APIs, databases, etc.)
- No CI pipeline for the sample (manual validation)

## Decisions

### D1: Project Structure — Multi-project Solution

**Choice**: Separate `.sln` with 4 projects: Desktop host, Bridge interfaces/impl, React web app, Tests.

**Alternatives considered**:
- Single project with everything: Simpler but doesn't demonstrate recommended architecture.
- Monorepo workspace (Nx/Turborepo): Overkill for a sample.

**Rationale**: Mirrors the `agibuild-hybrid` template structure. Bridge project is separate so interfaces are reusable across Desktop/Mobile hosts. Tests project validates services independently.

```
samples/avalonia-react/
├── AvaloniReact.sln
├── AvaloniReact.Desktop/          # Avalonia host
├── AvaloniReact.Bridge/           # Shared interfaces + implementations
├── AvaloniReact.Web/              # React + Vite + TypeScript + Tailwind
└── AvaloniReact.Tests/            # xUnit tests with MockBridgeService
```

### D2: Page Architecture — Dynamic Page Registry

**Choice**: Pages are defined as a collection of `PageDefinition` records, registered at startup. The React app reads the page list from a Bridge service and renders navigation dynamically.

**Alternatives considered**:
- Hardcoded routes in React Router: Works but defeats the demo purpose of showing dynamic C#↔JS coordination.
- Plugin system with lazy loading: Over-engineered for a sample.

**Rationale**: Demonstrates a real-world pattern (configurable navigation) while showcasing the Bridge. Adding a new page requires: (1) add a React component, (2) register it in the page registry on C# side. No hardcoded route lists.

```csharp
// C# side — page registry
[JsExport]
public interface IAppShellService
{
    Task<List<PageDefinition>> GetPages();
    Task<AppInfo> GetAppInfo();
}

public record PageDefinition(string Id, string Title, string Icon, string Route);
```

```typescript
// React side — dynamic routing
const pages = await appShellService.getPages();
// Renders <NavLink> + <Route> for each page dynamically
```

### D3: Bridge Services Design — Four Domain Services + One Shell Service

**Choice**: Five `[JsExport]` services and two `[JsImport]` services.

| Service | Direction | Purpose |
|---------|-----------|---------|
| `IAppShellService` | JsExport | Page registry, app info |
| `ISystemInfoService` | JsExport | OS, runtime, platform info |
| `IChatService` | JsExport | Echo/simulated streaming |
| `IFileService` | JsExport | File listing, read/write via native dialogs |
| `ISettingsService` | JsExport | Preferences CRUD, persistence |
| `IUiNotificationService` | JsImport | Toast/notification from C# → JS |
| `IThemeService` | JsImport | Theme change trigger from C# → JS |

**Rationale**: Each service demonstrates a different Bridge pattern:
- `IAppShellService`: Simple RPC with complex return types (List<Record>)
- `ISystemInfoService`: Read-only data from native runtime
- `IChatService`: Simulated streaming via sequential messages (no real AI dependency)
- `IFileService`: Native dialog integration + file I/O
- `ISettingsService`: Bidirectional state sync (read + write + notify on change)
- `IUiNotificationService` / `IThemeService`: JS→C# proxy pattern

### D4: React Tech Stack

**Choice**: React 19 + TypeScript + Vite 6 + Tailwind CSS 4 + React Router 7.

**Alternatives considered**:
- Next.js: SSR focus is wrong for embedded SPA.
- CSS Modules / Styled Components: Heavier, less modern appeal.
- Zustand / Redux: State management library adds complexity; React Context + hooks sufficient for a sample.

**Rationale**: Vite is the de facto standard for React SPA builds, integrates perfectly with `SpaHostingOptions.DevServerUrl`. Tailwind provides modern UI with minimal CSS authoring. React Router handles SPA routing that maps to the `app://` scheme with fallback.

### D5: Dev / Production Mode Switch

**Choice**: Conditional compilation in `MainWindow.axaml.cs` with `#if DEBUG` / `#else`.

```csharp
#if DEBUG
    webView.EnableSpaHosting(new SpaHostingOptions
    {
        DevServerUrl = "http://localhost:5173",
    });
#else
    webView.EnableSpaHosting(new SpaHostingOptions
    {
        EmbeddedResourcePrefix = "wwwroot",
        ResourceAssembly = typeof(MainWindow).Assembly,
    });
#endif
```

**Rationale**: Same pattern as the existing template. Simple, explicit, no runtime configuration needed.

### D6: Chat Streaming Simulation

**Choice**: `IChatService.SendMessage` returns immediately with an acknowledgment. The service then calls back via `IUiNotificationService` (JsImport) to simulate a "typing" effect with a series of partial messages, or alternatively returns the full response and the React side handles the typewriter animation.

**Decision**: Keep it simple — `SendMessage` returns `ChatResponse` with full text. React side renders with a typewriter animation. No actual streaming protocol needed.

**Rationale**: Demonstrates complex types (message history, timestamps) and async patterns without requiring WebSocket or streaming infrastructure.

### D7: Embedded Resource Build Pipeline

**Choice**: MSBuild `BeforeBuild` target runs `npm run build` in `AvaloniReact.Web/`, then includes `AvaloniReact.Web/dist/**` as `EmbeddedResource` in the Desktop project.

```xml
<!-- AvaloniReact.Desktop.csproj -->
<Target Name="BuildWebApp" BeforeTargets="BeforeBuild" Condition="'$(Configuration)' == 'Release'">
    <Exec Command="npm run build" WorkingDirectory="../AvaloniReact.Web" />
</Target>
<ItemGroup Condition="'$(Configuration)' == 'Release'">
    <EmbeddedResource Include="../AvaloniReact.Web/dist/**" LinkBase="wwwroot" />
</ItemGroup>
```

**Rationale**: Automates the web build for Release mode. Debug mode uses Vite dev server directly — no embedded resources needed.

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| React/Vite version churn | Sample may need updates as dependencies evolve | Pin exact major versions in `package.json`; use `^` only for patch updates |
| `app://` routing vs React Router | Potential mismatch between browser-style routing and custom scheme | SPA fallback in `SpaHostingOptions` already handles this; verify with nested routes |
| File operations platform-specific | `IFileService` uses `StorageProvider` which varies by platform | Desktop-only scope; use Avalonia's `IStorageProvider` for native dialogs |
| npm build in MSBuild | CI machines need Node.js installed | Document prerequisite; sample is not in main CI pipeline |

## Testing Strategy

| Layer | Approach | Tool |
|-------|----------|------|
| C# Bridge services | Unit tests via `MockBridgeService` | xUnit + `MockBridgeService` |
| Service logic | Direct method calls on implementations | xUnit |
| React components | Manual verification (not in initial scope) | — |
| Integration | Run desktop app, verify all pages work | Manual E2E |
