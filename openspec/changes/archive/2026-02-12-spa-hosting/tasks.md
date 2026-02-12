# spa-hosting — Tasks

## Task 1: Create SpaHostingOptions in Core
**Acceptance**: `SpaHostingOptions` with `Scheme`, `Host`, `FallbackDocument`, `EmbeddedResourcePrefix`, `ResourceAssembly`, `DevServerUrl`, `AutoInjectBridgeScript`, `DefaultHeaders`.

## Task 2: Create SpaHostingService in Runtime
**Acceptance**: `SpaHostingService` handles `WebResourceRequested`; embedded mode (GetManifestResourceStream); dev proxy mode (HttpClient); MIME detection (40+ types); hashed filename cache; SPA fallback.

## Task 3: Create SpaHostingExtensions
**Acceptance**: `AddEmbeddedFileProvider` and `AddDevServerProxy` convenience methods.

## Task 4: Add EnableSpaHosting to WebViewCore + Dispose cleanup
**Acceptance**: `WebViewCore.EnableSpaHosting()` registers scheme, subscribes to `WebResourceRequested`, auto-enables bridge; proper `Dispose` cleanup.

## Task 5: Write 28 tests
**Acceptance**: 28 CTs covering options, MIME, caching, fallback, embedded serving, 404, WebViewCore integration.

## Task 6: Add EmbeddedResource test fixture
**Acceptance**: Test fixture for embedded resource serving in unit tests.
