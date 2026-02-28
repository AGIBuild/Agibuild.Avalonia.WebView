## 1. GTK ContextMenu Implementation

- [x] 1.1 Add context-menu callback typedef and struct field to WebKitGtkShim.c
- [x] 1.2 Connect `context-menu` signal in ag_gtk_attach, implement handler extracting hit-test data
- [x] 1.3 Add P/Invoke delegate and struct field to GtkWebViewAdapter NativeMethods
- [x] 1.4 Wire up callback in GtkWebViewAdapter.Initialize, raise ContextMenuRequested event
- [x] 1.5 Verify native shim compiles (header-level, no Linux CI required)

## 2. Documentation — Platform Limitations

- [x] 2.1 Update compatibility matrix: GTK PrintToPdf ❌, macOS DevTools ⚠️, IAsyncPreloadScriptAdapter Windows-only
- [x] 2.2 Add inline code comments in macOS adapter clarifying DevTools behavior
- [x] 2.3 Add inline code comments in GTK adapter clarifying PrintToPdf absence

## 3. Tests

- [x] 3.1 Full regression: all existing tests pass
