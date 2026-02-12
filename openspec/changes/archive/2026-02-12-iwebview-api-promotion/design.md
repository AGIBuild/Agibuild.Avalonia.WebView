# IWebView API Promotion — Design

**ROADMAP**: Phase 3.8 action items

## Approach

Additive interface changes only — no breaking changes. All promoted members already existed on adapter implementations.

## Changes

1. **IWebView additions**: ZoomFactor, ZoomFactorChanged, FindInPageAsync, StopFindInPage, AddPreloadScript, RemovePreloadScript, ContextMenuRequested
2. **WebView control**: New `EnableSpaHosting` property for SPA hosting configuration
3. **AGWV004 graduation**: Remove `[Experimental]` from `WebResourceRequestedEventArgs`
4. **XML doc fix**: Resolve duplicate documentation on `ContextMenuMediaType`

## TestWebViewHost

Updated with stub implementations for all new `IWebView` members. All 525 tests passing.
