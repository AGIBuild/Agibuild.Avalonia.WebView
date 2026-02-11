# Command Manager

## Problem
`ICommandManager` is currently an empty placeholder interface (`AGWV002`). Consumers cannot programmatically invoke clipboard, selection, or undo/redo commands on the WebView.

## Solution
Implement `ICommandManager` with standard editing commands that delegate to each platform's native WebView command API. Introduce `ICommandAdapter` as an optional facet interface, following the established facet pattern (like `ICookieAdapter`, `IDownloadAdapter`).

## Scope
- Define `ICommandManager` methods: `Copy`, `Cut`, `Paste`, `SelectAll`, `Undo`, `Redo`
- Define `ICommandAdapter` optional facet interface
- Implement in all 5 platform adapters via native APIs
- Wire `WebViewCore.TryGetCommandManager()` to return a real instance when adapter supports it
- Add contract tests
