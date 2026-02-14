## MODIFIED Requirements

### Requirement: StopFindInPage clears search state
`WebViewCore.StopFindInPageAsync(bool clearHighlights = true)` SHALL clear match highlights and reset search state.

#### Scenario: Stop clears highlights
- **GIVEN** an active find-in-page search
- **WHEN** `await StopFindInPageAsync()` is called
- **THEN** match highlights are removed from the page

### Requirement: WebView and WebDialog expose find API
Both `WebView` and `WebDialog` SHALL expose `FindInPageAsync` and `StopFindInPageAsync` delegating to `WebViewCore`.

#### Scenario: WebView exposes find
- **WHEN** `webView.FindInPageAsync("text")` is called
- **THEN** it delegates to the underlying `WebViewCore`
