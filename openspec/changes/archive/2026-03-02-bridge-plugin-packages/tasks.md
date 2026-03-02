## 1. Plugin Convention Contracts

- [x] 1.1 Define `IBridgePlugin` interface with `static abstract IEnumerable<BridgePluginServiceDescriptor> GetServices()` in `Agibuild.Fulora.Core`
- [x] 1.2 Define `BridgePluginServiceDescriptor` record: `InterfaceType`, `Factory(IServiceProvider?)`, `BridgeOptions?`
- [x] 1.3 Implement `UsePlugin<TPlugin>(IServiceProvider?)` extension method on `IBridgeService` that iterates descriptors and calls `Expose<T>()`
- [x] 1.4 Add contract tests: UsePlugin registers all services, factory receives service provider, BridgeOptions applied, duplicate plugin registration detected

## 2. Reference Plugin — LocalStorage

- [x] 2.1 Create `plugins/Agibuild.Fulora.Plugin.LocalStorage/` project with `[JsExport] ILocalStorageService` interface
- [x] 2.2 Implement `LocalStorageService` backed by JSON file storage (app data directory)
- [x] 2.3 Implement `LocalStoragePlugin : IBridgePlugin` with service descriptor
- [x] 2.4 Add unit tests: CRUD operations, persistence across service restart, key enumeration, clear
- [x] 2.5 Add source generator reference to ensure TypeScript declarations are generated

## 3. npm Companion Package

- [x] 3.1 Create `packages/bridge-plugin-local-storage/` npm package structure
- [x] 3.2 Add TypeScript declarations from generated `bridge.d.ts` output
- [x] 3.3 Add typed `getLocalStorageService()` helper function using `bridgeClient.getService()`
- [x] 3.4 Add `package.json` with correct peer dependency on `@agibuild/bridge`
- [x] 3.5 Add unit tests for the typed helper function

## 4. Documentation and Integration

- [x] 4.1 Add plugin authoring guide to `docs/` covering NuGet + npm dual-package workflow
- [x] 4.2 Add integration test: install LocalStorage plugin → UsePlugin → call CRUD from JS → verify persistence
- [x] 4.3 Add sample code to `samples/avalonia-react/` demonstrating plugin usage
