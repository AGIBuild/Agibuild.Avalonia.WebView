## Context

Fulora's bridge architecture enables typed, policy-governed C#↔JS communication via `[JsExport]`/`[JsImport]` interfaces and source-generated proxies. Currently, each application must implement its own bridge services. There is no convention for packaging reusable bridge services as distributable NuGet + npm packages.

The existing `Bridge.Expose<T>(implementation)` API and `IBridgeServiceRegistration<T>` source-generated type provide the foundation for automatic registration. The TypeScript `.d.ts` generation pipeline produces per-assembly type declarations.

## Goals / Non-Goals

**Goals:**
- Define a plugin convention: NuGet package containing `[JsExport]`/`[JsImport]` interfaces + implementations + source-generated TS types
- Provide `Bridge.UsePlugin<TPlugin>()` extension for one-call registration of all plugin services
- Define the npm companion package convention (`@agibuild/bridge-plugin-{name}`) with pre-generated TypeScript types
- Create a reference plugin implementation as proof-of-concept
- Support policy governance for plugin-provided services (same as application services)

**Non-Goals:**
- Plugin marketplace or centralized registry (use NuGet + npm)
- Runtime hot-loading/unloading of plugins
- Plugin UI components (bridge services only)
- Versioning enforcement between NuGet and npm packages
- Plugin dependency resolution (each plugin is independent)

## Decisions

### D1: Plugin definition — marker interface vs attribute vs convention

**Choice**: `IBridgePlugin` marker interface with `static abstract IEnumerable<BridgePluginServiceDescriptor> GetServices()` method (leveraging C# static abstract members).

**Rationale**: A marker interface enables `UsePlugin<T>()` to discover and register all services at compile time. Static abstract members avoid runtime reflection (NativeAOT compatible). The plugin class is never instantiated — it's a manifest.

**Alternative considered**:
- Assembly-level attribute scanning — rejected because it requires runtime reflection, not AOT-safe
- Convention-based (`BridgePlugin` suffix) — rejected because it's fragile and invisible to tooling

### D2: Service descriptor model

**Choice**: `BridgePluginServiceDescriptor` record containing:
- `Type InterfaceType` — the `[JsExport]` or `[JsImport]` interface
- `Func<IServiceProvider?, object> Factory` — factory to create the implementation
- `BridgeOptions? Options` — optional bridge options (allowed origins, rate limit)

**Rationale**: Factory pattern supports DI integration. Optional service provider parameter enables plugins to resolve dependencies. `BridgeOptions` allows plugins to declare default security settings.

### D3: Registration API

**Choice**: `Bridge.UsePlugin<TPlugin>(IServiceProvider? serviceProvider = null)` extension method on `IBridgeService`.

**Rationale**: One-call registration keeps it simple. Service provider injection is optional for plugins that don't need DI. The extension method iterates `TPlugin.GetServices()` and calls `Bridge.Expose<T>()` for each.

### D4: npm companion package structure

**Choice**: Each plugin publishes an npm package containing:
- Pre-generated `.d.ts` with service interfaces and DTO types
- Optional thin client wrappers using `bridgeClient.getService<T>()`
- Package name: `@agibuild/bridge-plugin-{name}` or `@{scope}/bridge-plugin-{name}`

**Rationale**: npm package provides TypeScript types that match the C# interfaces. Frontend developers `npm install` the companion and get full IntelliSense. The source generator already produces TypeScript declarations, so the package is generated from build output.

### D5: Reference plugin — LocalStorage

**Choice**: Implement `Agibuild.Fulora.Plugin.LocalStorage` as the reference plugin with:
- `ILocalStorageService` [JsExport]: `Get(key)`, `Set(key, value)`, `Remove(key)`, `Clear()`, `GetKeys()`
- Backed by a simple JSON file or SQLite (decided at implementation time)
- npm companion: `@agibuild/bridge-plugin-local-storage`

**Rationale**: Local storage is universally useful, simple to implement, and clearly demonstrates the plugin pattern.

## Testing Strategy

- **Contract tests**: Plugin registration via `UsePlugin<T>()` → verify all services are exposed, policy applied
- **Unit tests**: `LocalStorageService` CRUD operations, file/db isolation
- **Integration tests**: Install plugin → call from JS → verify response with types

## Risks / Trade-offs

- **[Static abstract members]** Requires C# 11+ / .NET 7+ → Mitigation: project targets .NET 10, so this is available. For netstandard2.0 consumers, provide a non-generic overload
- **[TS type distribution]** Ensuring generated TS types stay in sync with NuGet package version → Mitigation: document the publish workflow; future CI automation
- **[Plugin isolation]** Plugins share the same bridge namespace, potential service name conflicts → Mitigation: convention of prefixed service names (`LocalStorage.get` not just `get`); document convention
