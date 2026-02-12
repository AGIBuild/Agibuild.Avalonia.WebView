# IWebView API Promotion

**Goal**: E2, E3
**ROADMAP**: Phase 3.8 action items

## Problem

The `IWebView` interface was incomplete compared to concrete implementations. Some stable APIs remained marked `[Experimental]`. Developers programming against `IWebView` could not use Zoom, Find-in-page, Preload scripts, or Context menu without downcasting.

## Solution

Promote ZoomFactor/ZoomFactorChanged, FindInPageAsync/StopFindInPage, AddPreloadScript/RemovePreloadScript, and ContextMenuRequested to `IWebView`. Add `EnableSpaHosting` to the WebView control. Remove `[Experimental("AGWV004")]` from `WebResourceRequestedEventArgs` (graduated). Fix duplicate XML doc on `ContextMenuMediaType`.

## Non-goals

Adding `EnableWebMessageBridge`/`DisableWebMessageBridge` to `IWebView` — advanced APIs stay on concrete types.

## References

E2, E3, ROADMAP 3.8 action items, API_SURFACE_REVIEW.md
