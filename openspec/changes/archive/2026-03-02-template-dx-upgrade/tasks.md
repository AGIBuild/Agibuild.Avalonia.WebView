## 1. Bridge Client Setup (All Variants)

- [x] 1.1 Add `@agibuild/bridge` dependency to all template `package.json` files (React, Vue, Vanilla)
- [x] 1.2 Create `src/bridge/client.ts` in each variant: configure `bridgeClient` with `withLogging()` in dev mode
- [x] 1.3 Create `src/bridge/services.ts` in each variant: typed service proxies using `bridgeClient.getService<T>()`
- [x] 1.4 Configure `BridgeTypeScriptOutputDir` in Bridge `.csproj` to output `bridge.d.ts` to Web `src/bridge/`
- [x] 1.5 Update `tsconfig.json` in each variant to include `src/bridge/bridge.d.ts`

## 2. React Template

- [x] 2.1 Create `src/hooks/useBridge.ts` with `useBridgeReady()` hook
- [x] 2.2 Update `App.tsx` to use `useBridgeReady()` and display bridge connection status
- [x] 2.3 Add baseline service demo: call a bridge service method and display result
- [x] 2.4 Add app-shell conditional: theme display component reacting to `ThemeChangedEvent`
- [x] 2.5 Add app-shell conditional: menu control component calling `ApplyMenuModel`

## 3. Vue Template

- [x] 3.1 Create `src/composables/useBridge.ts` with `useBridgeReady()` composable (Composition API)
- [x] 3.2 Update `App.vue` to use bridge readiness composable and display connection status
- [x] 3.3 Add baseline service demo and app-shell conditional theme/menu components

## 4. Vanilla Template

- [x] 4.1 Update `src/main.ts` to use `bridgeClient.ready()` and typed `getService<T>()`
- [x] 4.2 Add simple bridge call demonstration with typed response

## 5. Desktop Host Updates (app-shell preset)

- [x] 5.1 Wire `AvaloniaHostCapabilityProvider` in app-shell preset startup (replacing no-op provider)
- [x] 5.2 Register `IThemeService` via `Bridge.Expose<IThemeService>()` in app-shell preset
- [x] 5.3 Configure default tray icon (app icon, app name tooltip, visible)
- [x] 5.4 Configure default native menu model (File → Exit, Help → About)
- [x] 5.5 Add `IDesktopHostService` bridge service exposing tray/menu control to JS

## 6. Bridge Project Updates

- [x] 6.1 Add `IThemeService` interface with `[JsExport]` to template Bridge project (app-shell conditional)
- [x] 6.2 Add shell service interfaces for tray/menu control (app-shell conditional)
- [x] 6.3 Verify source generator produces `bridge.d.ts` including all service types

## 7. Testing and Verification

- [x] 7.1 Update template E2E test: `dotnet new agibuild-hybrid` → `dotnet build` → verify bridge.d.ts generated
- [x] 7.2 Update template E2E test: `npm install && npm run build` → verify frontend builds with @agibuild/bridge
- [x] 7.3 Add template E2E test: app-shell preset → verify tray/menu/theme service registration markers present
- [x] 7.4 Update governance tests to validate @agibuild/bridge dependency in template package.json
