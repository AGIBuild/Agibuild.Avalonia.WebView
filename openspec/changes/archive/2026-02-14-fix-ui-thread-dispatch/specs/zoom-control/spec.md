## MODIFIED Requirements

### Requirement: ZoomFactor property get/set
`WebViewCore` SHALL expose asynchronous zoom APIs:
- `Task<double> GetZoomFactorAsync()`
- `Task SetZoomFactorAsync(double zoomFactor)`

`GetZoomFactorAsync()` SHALL return default `1.0` when adapter does not support zoom.
`SetZoomFactorAsync` SHALL clamp values to adapter-supported range (baseline `0.25` to `5.0`).

#### Scenario: Default zoom is 1.0
- **WHEN** a WebViewCore is created and `GetZoomFactorAsync()` is called
- **THEN** the returned value is `1.0`

#### Scenario: Set zoom factor
- **WHEN** `SetZoomFactorAsync(1.5)` is called
- **THEN** the page renders at 150% zoom

#### Scenario: Clamping out-of-range values
- **WHEN** `SetZoomFactorAsync(0.1)` is called
- **THEN** the effective zoom is clamped to minimum supported value
- **WHEN** `SetZoomFactorAsync(10.0)` is called
- **THEN** the effective zoom is clamped to maximum supported value

### Requirement: IZoomAdapter facet detection
`WebViewCore` SHALL detect `IZoomAdapter` on the adapter. If absent:
- `GetZoomFactorAsync()` SHALL return `1.0`
- `SetZoomFactorAsync(...)` SHALL be a no-op

#### Scenario: Adapter supports zoom
- **WHEN** adapter implements `IZoomAdapter`
- **THEN** zoom operations delegate to adapter through runtime operation queue

#### Scenario: Adapter lacks zoom support
- **WHEN** adapter does not implement `IZoomAdapter`
- **THEN** getter returns `1.0` and setter is no-op

## REMOVED Requirements

### Requirement: WebView ZoomFactor as styled property
**Reason**: Public API is reset to async-first operations; sync property-based zoom surface is removed.
**Migration**: Replace `webView.ZoomFactor = x` with `await webView.SetZoomFactorAsync(x)` and replace `var z = webView.ZoomFactor` with `var z = await webView.GetZoomFactorAsync()`.
