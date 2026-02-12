# Design: Template E2E Testing & Sub-package Publishing

## Architecture

### Sub-package Packaging
Five projects made independently packable:
- `Agibuild.Avalonia.WebView.Core` — contract types ([JsExport], [JsImport], IWebView, IBridgeService)
- `Agibuild.Avalonia.WebView.Adapters.Abstractions` — adapter SPI layer
- `Agibuild.Avalonia.WebView.Runtime` — WebViewCore, RuntimeBridgeService, RPC
- `Agibuild.Avalonia.WebView.Bridge.Generator` — Roslyn source generator (analyzer)
- `Agibuild.Avalonia.WebView.Testing` — MockBridgeService, MockWebViewAdapter, TestDispatcher

### Template Pack Project
`templates/Agibuild.Avalonia.WebView.Templates.csproj` uses `<PackageType>Template</PackageType>` with `<ContentTargetFolders>content</ContentTargetFolders>` to package the `agibuild-hybrid` template folder into a NuGet package. Users install via `dotnet new install Agibuild.Avalonia.WebView.Templates`.

### Nuke Build Targets

```
PackAll → packs Core, Abstractions, Runtime, Generator, Testing
PackTemplate → packs template .nupkg
PublishTemplate → pushes template to NuGet (requires NuGetApiKey)
TemplateE2E → depends on Pack + PackAll, runs full E2E cycle
```

### E2E Test Flow
1. Copy .nupkg files from `artifacts/packages/` to a temp NuGet feed
2. `dotnet new install` template from source folder
3. `dotnet new agibuild-hybrid -n SmokeApp`
4. Write `nuget.config` pointing to local feed
5. Patch `Version="*"` → `Version="*-*"` for prerelease resolution
6. Inject `BridgeRpcE2ETests.cs` (8 tests covering JsExport/JsImport/coexistence/source-generator)
7. Build and run tests (3 original + 8 injected = 11 total)
8. Cleanup: uninstall template, delete temp directory

### E2E Test Coverage
- `JsExport_attribute_is_present_on_IGreeterService`
- `JsImport_attribute_is_present_on_INotificationService`
- `JsExport_Greet_implementation_returns_expected_result`
- `JsExport_service_can_be_exposed_via_MockBridge`
- `JsImport_proxy_can_be_configured_and_retrieved`
- `JsImport_proxy_invocation_succeeds`
- `JsExport_and_JsImport_coexist_on_same_bridge`
- `Source_generator_produced_registration_types`
