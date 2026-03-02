## Why

Agibuild.Fulora is currently tied to Avalonia UI as the sole host platform. .NET MAUI is widely used for cross-platform mobile and desktop apps. Developers building MAUI apps cannot use Fulora's type-safe bridge, SPA hosting, or contract-driven testability without a MAUI integration layer.

**Goal alignment**: Expand Fulora's reach to MAUI adopters; prove framework portability via adapter pattern; enable hybrid MAUI apps with the same bridge and hosting capabilities.

## What Changes

- Create an integration layer (`Agibuild.Fulora.Maui` or equivalent) that adapts Fulora's WebView hosting to MAUI's `WebView` control
- Use adapter pattern similar to the existing Avalonia host: implement host-neutral contracts (e.g., `IWebViewHost`) with a MAUI-specific adapter
- Expose the same bridge, SPA hosting, and configuration APIs as the Avalonia host
- Support MAUI's WebView on Android, iOS, Windows, and macOS where available

## Non-goals

- Replacing or deprecating the Avalonia host
- Full feature parity with Avalonia host in initial release (core bridge + SPA hosting first)
- Modifying core bridge or runtime for MAUI-specific behavior

## Capabilities

### New Capabilities
- `maui-host-adapter`: MAUI integration layer allowing Fulora WebView to be used in MAUI apps with bridge and SPA hosting

## Impact

- New package: `Agibuild.Fulora.Maui` (or similar)
- New sample: MAUI hybrid app demonstrating bridge and SPA hosting
- Documentation: MAUI setup and usage guide
