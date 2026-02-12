# Design: API Surface Review

## Scope
- All public classes, interfaces, enums, records, structs in src/
- IWebView interface consistency check
- Naming convention validation
- Experimental attribute audit

## Findings
See docs/API_SURFACE_REVIEW.md for full report.

Key findings:
1. IWebView missing Zoom/Find/Preload/ContextMenu members
2. EnableSpaHosting not on WebView control
3. Duplicate XML doc fixed (ContextMenuMediaType)
4. AGWV004 ready to graduate from Experimental
5. All naming follows .NET conventions ✅

## Testing
No code changes requiring tests (doc fix only).
