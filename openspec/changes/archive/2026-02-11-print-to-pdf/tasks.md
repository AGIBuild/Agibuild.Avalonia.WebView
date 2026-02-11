# Print to PDF — Tasks

## Core Contracts
- [x] Add `PdfPrintOptions` class to `WebViewContracts.cs`
- [x] Add `PrintToPdfAsync(PdfPrintOptions?)` to `IWebView` interface

## Adapter Abstractions
- [x] Add `IPrintAdapter` facet interface

## Runtime
- [x] Update `WebViewCore` to detect `IPrintAdapter` and store reference
- [x] Implement `WebViewCore.PrintToPdfAsync()` delegating to adapter or throwing `NotSupportedException`

## Platform Adapters — Windows
- [x] Implement `IPrintAdapter` using `CoreWebView2.PrintToPdfAsync`

## Platform Adapters — macOS
- [x] Add native shim function `ag_wk_print_to_pdf` in `WkWebViewShim.mm`
- [x] Add P/Invoke + C# handler in `MacOSWebViewAdapter`

## Platform Adapters — iOS
- [x] Add native shim function `ag_wk_print_to_pdf` in `WkWebViewShim.iOS.mm`
- [x] Add P/Invoke + C# handler in `iOSWebViewAdapter`
- [x] Rebuild iOS native libraries

## Platform Adapters — GTK
- [x] ~~Add native shim function~~ GTK WebKitGTK lacks direct PDF export; not implementing `IPrintAdapter`
- [x] ~~Add P/Invoke~~ Skipped — runtime throws `NotSupportedException`

## Platform Adapters — Android
- [x] ~~Implement `IPrintAdapter`~~ Android lacks headless PDF export; not implementing `IPrintAdapter`

## Consumer Surface
- [x] Add `PrintToPdfAsync()` to `WebView` control
- [x] Add `PrintToPdfAsync()` to `WebDialog` / `AvaloniaWebDialog`

## Tests
- [x] Add `PdfPrintOptions` constructor defaults test
- [x] Add `IPrintAdapter` facet detection test
- [x] Add `PrintToPdfAsync()` contract test (throws when unsupported)
- [x] Add mock adapter with print support test

## Build & Coverage
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
