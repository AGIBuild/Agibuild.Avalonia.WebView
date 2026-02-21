## Purpose
Define zoom-control contracts for WebView runtime, adapter facets, and UI surfaces.

## Requirements

### Requirement: ZoomFactor property get/set
`WebViewCore` SHALL expose a `ZoomFactor` property (double, default 1.0) that gets and sets the page zoom level. 1.0 = 100%, 2.0 = 200%, etc.

#### Scenario: Default zoom is 1.0
- **WHEN** a WebViewCore is created
- **THEN** `ZoomFactor` is `1.0`

#### Scenario: Set zoom factor
- **WHEN** `ZoomFactor` is set to `1.5`
- **THEN** the page renders at 150% zoom

#### Scenario: Clamping out-of-range values
- **WHEN** `ZoomFactor` is set to `0.1` (below minimum 0.25)
- **THEN** `ZoomFactor` is clamped to `0.25`
- **WHEN** `ZoomFactor` is set to `10.0` (above maximum 5.0)
- **THEN** `ZoomFactor` is clamped to `5.0`

### Requirement: ZoomFactorChanged event
`WebViewCore` SHALL fire `ZoomFactorChanged` when the zoom level changes, whether programmatically or by user action.

#### Scenario: Event fires on programmatic change
- **WHEN** `ZoomFactor` is set to `2.0`
- **THEN** `ZoomFactorChanged` fires with value `2.0`

### Requirement: IZoomAdapter facet detection
`WebViewCore` SHALL detect `IZoomAdapter` on the adapter. If absent, `ZoomFactor` setter SHALL be a no-op and getter SHALL return `1.0`.

#### Scenario: Adapter supports zoom
- **WHEN** adapter implements `IZoomAdapter`
- **THEN** zoom operations delegate to the adapter

#### Scenario: Adapter lacks zoom support
- **WHEN** adapter does not implement `IZoomAdapter`
- **THEN** `ZoomFactor` getter returns `1.0`, setter is no-op

### Requirement: WebView ZoomFactor as styled property
`WebView` control SHALL expose `ZoomFactor` as a `StyledProperty<double>` enabling XAML binding.

#### Scenario: XAML binding
- **WHEN** `<webview:WebView ZoomFactor="1.5" />` is used
- **THEN** the WebView renders at 150% zoom
