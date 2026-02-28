## Context

Phase 8 M8.5. The framework has 5 platform adapters (Windows, macOS, iOS, Android, GTK). Several features are no-op or missing on specific platforms without clear documentation.

## Goals / Non-Goals

**Goals:**
- GTK ContextMenu: functional, event-driven context menu interception
- Explicit documentation of platform limitations for PrintToPdf, DevTools toggle, and async preload
- Updated compatibility matrix reflecting actual feature parity

**Non-Goals:**
- GTK PrintToPdf implementation
- macOS DevTools programmatic toggle
- Cross-platform IAsyncPreloadScriptAdapter

## Decisions

### D1: GTK ContextMenu — native signal handler

**Decision**: Add `context-menu` signal handler to the C shim that extracts hit-test data and passes it to managed code via a new callback in `ag_gtk_callbacks`.

Callback signature:
```c
typedef bool (*ag_gtk_context_menu_cb)(
    void* user_data,
    double x, double y,
    const char* link_uri,       // NULL if no link
    const char* selection_text,  // NULL if no selection
    int media_type,             // 0=None, 1=Image, 2=Video, 3=Audio
    const char* media_source_uri,// NULL if no media
    bool is_editable);
```

Return `true` to suppress the default context menu, `false` to allow it.

**Rationale**: Follows existing callback pattern (policy, download, permission). Minimal data transfer across FFI boundary.

### D2: GTK PrintToPdf — document as limitation

**Decision**: Do not implement. WebKitGTK's `webkit_web_view_get_snapshot` returns a Cairo surface (raster), not a PDF. There is no `webkit_web_view_print_to_pdf` API. GTK print dialog (`webkit_print_operation_run_dialog`) requires a display, unsuitable for headless PDF generation.

### D3: macOS DevTools — document existing behavior

**Decision**: Document that `OpenDevTools()` and `CloseDevTools()` are no-ops on macOS. The Web Inspector IS available when `EnableDevTools` is set, accessible via right-click → Inspect Element. This is a WKWebView limitation (no public API for programmatic inspector control).

### D4: IAsyncPreloadScriptAdapter — Windows-only

**Decision**: Document as Windows-only. WKWebView's `addUserScript` is synchronous. Android's `evaluateJavascript` doesn't provide async completion for script injection. WebKitGTK's `webkit_user_content_manager_add_script` is synchronous. The runtime fallback (`IPreloadScriptAdapter` wrapped in `Task.FromResult`) provides adequate functionality.

## Testing Strategy

- GTK ContextMenu: Cannot test natively on Windows CI. Add unit test verifying `ContextMenuRequested` event args construction. Integration test would require Linux with WebKitGTK.
- Documentation changes: Review only, no automated tests needed.
