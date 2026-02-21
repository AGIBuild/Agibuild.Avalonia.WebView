## Purpose
Define print-to-pdf contracts for options, adapter support, and WebView surface APIs.

## Requirements

### Requirement: PdfPrintOptions type in Core
The Core assembly SHALL define a `PdfPrintOptions` class with properties:
- `bool Landscape` (default: false)
- `double PageWidth` (default: 8.5, in inches)
- `double PageHeight` (default: 11.0, in inches)
- `double MarginTop`, `MarginBottom`, `MarginLeft`, `MarginRight` (default: 0.4, in inches)
- `double Scale` (default: 1.0)
- `bool PrintBackground` (default: true)

#### Scenario: PdfPrintOptions is resolvable
- **WHEN** a consumer creates `new PdfPrintOptions()`
- **THEN** it has US Letter defaults with 0.4-inch margins

### Requirement: IWebView includes PrintToPdfAsync
The `IWebView` interface SHALL define:
- `Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null)`

The method SHALL return a PDF byte array.

#### Scenario: PrintToPdfAsync returns PDF data
- **WHEN** `PrintToPdfAsync()` is called on a loaded WebView
- **THEN** it returns a non-empty byte array starting with PDF magic bytes (%PDF-)

### Requirement: IPrintAdapter facet for adapters
The adapter abstractions SHALL define an `IPrintAdapter` interface:
- `Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options)`

The runtime SHALL detect `IPrintAdapter` via type check at initialization.

#### Scenario: Adapter implementing IPrintAdapter enables PDF export
- **WHEN** an adapter implements both `IWebViewAdapter` and `IPrintAdapter`
- **THEN** `PrintToPdfAsync()` delegates to the adapter

#### Scenario: Adapter without IPrintAdapter throws
- **WHEN** an adapter does not implement `IPrintAdapter`
- **THEN** `PrintToPdfAsync()` throws `NotSupportedException`

### Requirement: All platform adapters implement IPrintAdapter
All five platform adapters SHALL implement `IPrintAdapter` using native PDF APIs:
- Windows: `CoreWebView2.PrintToPdfAsync`
- macOS: `WKWebView.createPDF`
- iOS: `WKWebView.createPDF`
- GTK: WebKit print operation with cairo PDF surface
- Android: `PdfDocument` or headless print

#### Scenario: Each adapter produces PDF
- **WHEN** `PrintToPdfAsync()` is called on any platform
- **THEN** it returns valid PDF bytes

### Requirement: WebView control exposes PrintToPdfAsync
The `WebView` Avalonia control and `WebDialog` SHALL expose `PrintToPdfAsync(PdfPrintOptions?)`.

#### Scenario: Consumer exports PDF from WebView
- **WHEN** `await webView.PrintToPdfAsync(new PdfPrintOptions { Landscape = true })` is called
- **THEN** it returns PDF bytes in landscape orientation
