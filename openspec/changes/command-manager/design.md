# Command Manager — Design

## Architecture

```
Consumer
  │
  ▼
IWebView.TryGetCommandManager() → ICommandManager
  │                                    │
  │                                    ├── Copy()
  │                                    ├── Cut()
  │                                    ├── Paste()
  │                                    ├── SelectAll()
  │                                    ├── Undo()
  │                                    └── Redo()
  │
  ▼
WebViewCore (detects ICommandAdapter facet)
  │
  ▼
ICommandAdapter (optional facet on IWebViewAdapter)
  │
  ├── Windows: WebView2 ExecuteScriptAsync("document.execCommand(...)")
  ├── macOS:   WKWebView evaluateJavaScript / NSResponder performSelector
  ├── iOS:     WKWebView evaluateJavaScript
  ├── GTK:     webkit_web_view_execute_editing_command()
  └── Android: AWebView evaluateJavascript("document.execCommand(...)")
```

## Core Contracts

```csharp
// Update existing placeholder:
public interface ICommandManager
{
    void Copy();
    void Cut();
    void Paste();
    void SelectAll();
    void Undo();
    void Redo();
}
```

## Adapter Facet

```csharp
// New facet in Adapters.Abstractions:
public interface ICommandAdapter
{
    void ExecuteCommand(WebViewCommand command);
}

public enum WebViewCommand
{
    Copy, Cut, Paste, SelectAll, Undo, Redo
}
```

## Design Decisions

1. **Enum-based commands** — Use `WebViewCommand` enum rather than individual methods on the adapter. This keeps the adapter surface minimal and extensible.
2. **document.execCommand fallback** — Most platforms can use `document.execCommand()` via JS as a universal fallback. Platform-native commands are preferred where available.
3. **Synchronous API** — Editing commands are fire-and-forget; no async needed.
4. **No native shim changes** — All commands can be dispatched via existing `InvokeScriptAsync` or platform-level APIs. No .mm/.c changes needed.
