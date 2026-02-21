## Purpose
Define find-in-page contracts for search execution, lifecycle, and adapter support detection.

## Requirements

### Requirement: FindInPageAsync initiates text search
`WebViewCore.FindInPageAsync(string text, FindInPageOptions? options)` SHALL search the current page for the given text and return a `FindInPageResult` with match count and active index.

#### Scenario: Basic text search
- **GIVEN** a page containing the text "hello" three times
- **WHEN** `FindInPageAsync("hello")` is called
- **THEN** result contains `TotalMatches >= 1` and `ActiveMatchIndex >= 0`

#### Scenario: Case-sensitive search
- **GIVEN** a page with "Hello" and "hello"
- **WHEN** `FindInPageAsync("hello", new FindInPageOptions { CaseSensitive = true })` is called
- **THEN** only lowercase matches are counted

#### Scenario: Forward/backward navigation
- **WHEN** `FindInPageAsync("hello", new FindInPageOptions { Forward = false })` is called
- **THEN** the active match moves backward through results

### Requirement: StopFindInPage clears search state
`WebViewCore.StopFindInPage()` SHALL clear all match highlights and reset search state.

#### Scenario: Stop clears highlights
- **GIVEN** an active find-in-page search
- **WHEN** `StopFindInPage()` is called
- **THEN** match highlights are removed from the page

### Requirement: IFindInPageAdapter facet detection
`WebViewCore` SHALL detect `IFindInPageAdapter` on the adapter at construction time. If the adapter does not implement `IFindInPageAdapter`, `FindInPageAsync` SHALL throw `NotSupportedException`.

#### Scenario: Adapter supports find
- **WHEN** the adapter implements `IFindInPageAdapter`
- **THEN** `FindInPageAsync` delegates to the adapter

#### Scenario: Adapter lacks find support
- **WHEN** the adapter does not implement `IFindInPageAdapter`
- **THEN** `FindInPageAsync` throws `NotSupportedException`

### Requirement: WebView and WebDialog expose find API
Both `WebView` control and `WebDialog` SHALL expose `FindInPageAsync` and `StopFindInPage` delegating to `WebViewCore`.

#### Scenario: WebView exposes find
- **WHEN** `webView.FindInPageAsync("text")` is called
- **THEN** it delegates to the underlying `WebViewCore`
