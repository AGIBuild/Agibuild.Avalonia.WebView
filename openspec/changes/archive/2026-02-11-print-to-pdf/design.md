# Print to PDF — Design

## Architecture

```
Consumer
  │
  ▼
IWebView.PrintToPdfAsync(options?) → Task<byte[]> (PDF)
  │
  ▼
WebViewCore (detects IPrintAdapter facet)
  │
  ▼
IPrintAdapter.PrintToPdfAsync(PdfPrintOptions?)
  │
  ├── Windows: CoreWebView2.PrintToPdfAsync(path) → read bytes
  ├── macOS:   WKWebView.createPDF(completionHandler:) → NSData
  ├── iOS:     WKWebView.createPDF(completionHandler:) → NSData
  ├── GTK:     webkit_web_view_print_operation (cairo PDF surface)
  └── Android: PrintManager + PdfDocument (complex) or JS window.print()
```

## Core Contracts

```csharp
// Add to IWebView:
Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null);

// New type in Core:
public sealed class PdfPrintOptions
{
    public bool Landscape { get; set; }
    public double PageWidth { get; set; } = 8.5;   // inches
    public double PageHeight { get; set; } = 11.0;  // inches
    public double MarginTop { get; set; } = 0.4;    // inches
    public double MarginBottom { get; set; } = 0.4;
    public double MarginLeft { get; set; } = 0.4;
    public double MarginRight { get; set; } = 0.4;
    public double Scale { get; set; } = 1.0;
    public bool PrintBackground { get; set; } = true;
}
```

## Adapter Facet

```csharp
public interface IPrintAdapter
{
    Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options);
}
```

## Design Decisions

1. **Return bytes, not file path** — Keeps the API pure; consumer decides where to save.
2. **Inches for page dimensions** — Matches web/print industry standard (Letter = 8.5×11).
3. **Native shim extensions needed** — macOS/iOS need `createPDF` callback; GTK needs print-to-file via cairo.
4. **Android limitation** — Android's `PrintManager` is UI-interactive. We'll use a headless `PdfDocument` approach or note it as limited.
