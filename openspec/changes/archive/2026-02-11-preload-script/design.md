## Context

The project already injects JS at runtime (RPC stub via `InvokeScriptAsync`). All 5 platform engines support "user scripts" that run at document start, before page JS. This is the same mechanism bundled-browser stacks use for `preload`.

## Goals / Non-Goals

**Goals:**
- Allow consumers to register JS scripts that run at document start (before page scripts)
- Support both global (via `WebViewEnvironmentOptions.PreloadScripts`) and per-instance (`AddPreloadScript`)
- Scripts persist across navigations (injected on every page load)
- Support `RemovePreloadScript` to unregister

**Non-Goals:**
- Sandboxed context isolation (bundled-browser `contextBridge` pattern — too complex for v1)
- File-path based preload (string JS only for simplicity)
- Module-style imports in preload scripts

## Decisions

1. **New adapter facet `IPreloadScriptAdapter`**
   ```csharp
   public interface IPreloadScriptAdapter
   {
       string AddPreloadScript(string javaScript);   // returns script ID
       void RemovePreloadScript(string scriptId);
   }
   ```

2. **Script ID** — each `AddPreloadScript` returns an opaque string ID (platform-dependent). Used for `RemovePreloadScript`. Consumers must store the ID if they want to remove later.

3. **Lifecycle** — preload scripts are added after adapter attach and persist until removed or adapter destroyed. Scripts added before attach are queued and applied on attach.

4. **Global vs per-instance:**
   - `WebViewEnvironmentOptions.PreloadScripts` → applied to all new WebViews at construction
   - `WebView.AddPreloadScript(js)` → per-instance, added dynamically

5. **Platform mapping:**
   - Windows: `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync()` → returns script ID
   - macOS/iOS: `WKUserContentController.addUserScript(_, injectionTime: .atDocumentStart)` → generate ID, track for removal
   - Android: stored in adapter, injected in `onPageStarted()` via `evaluateJavascript()`
   - GTK: `webkit_user_content_manager_add_script()` with `WEBKIT_USER_SCRIPT_INJECT_AT_DOCUMENT_START`

6. **RPC stub** — the existing RPC JS stub (`WebViewRpcService.JsStub`) will be refactored to use this mechanism internally, rather than `InvokeScriptAsync` post-navigation.

## Risks / Trade-offs

- WKWebView (macOS/iOS) `addUserScript` doesn't return an ID — we must generate our own and track script ↔ ID mapping for removal. Removal requires rebuilding the user content controller's script list.
- Android has no native "user script" concept — we simulate by injecting in `onPageStarted`, which is slightly later than true document start. Accept as ⚠️ platform difference.
- Queueing scripts before attach adds complexity but is necessary for `WebViewEnvironmentOptions.PreloadScripts`.
