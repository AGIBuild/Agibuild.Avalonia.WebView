# Print to PDF

## Problem
Consumers cannot export the WebView content as a PDF document. This is a common requirement for report generation, invoice printing, and document export.

## Solution
Add `Task<byte[]> PrintToPdfAsync(PdfPrintOptions? options = null)` to `IWebView` that returns a PDF byte array. Introduce `IPrintAdapter` as an optional facet interface. `PdfPrintOptions` allows specifying page size, margins, orientation, and scale.

## Scope
- Define `PdfPrintOptions` record with landscape, page size, margins, scale
- Define `IPrintAdapter` with `Task<byte[]> PrintToPdfAsync(PdfPrintOptions?)`
- Add `PrintToPdfAsync()` to `IWebView`
- Implement in all 5 platform adapters using native print/PDF APIs
- Wire through `WebViewCore`
- Add contract tests
