## Requirements

### Requirement: ContextMenuRequested event fires on right-click
`WebViewCore` SHALL fire `ContextMenuRequested` before the native context menu is shown.

#### Scenario: Event fires with position
- **WHEN** user right-clicks at position (100, 200)
- **THEN** `ContextMenuRequested` fires with `X=100, Y=200`

#### Scenario: Event includes link info
- **WHEN** user right-clicks on a hyperlink
- **THEN** `ContextMenuRequested.LinkUri` is non-null and contains the link target

#### Scenario: Event includes selection text
- **WHEN** user selects text and right-clicks
- **THEN** `ContextMenuRequested.SelectionText` contains the selected text

#### Scenario: Event includes editable state
- **WHEN** user right-clicks in an input or contenteditable element
- **THEN** `ContextMenuRequested.IsEditable` is `true`

### Requirement: Handled suppresses native menu
Setting `ContextMenuRequested.Handled = true` SHALL prevent the native context menu from appearing.

#### Scenario: Suppress native menu
- **GIVEN** a handler sets `e.Handled = true`
- **WHEN** the event processing completes
- **THEN** no native context menu is shown

#### Scenario: Default allows native menu
- **GIVEN** no handler or handler does not set `Handled`
- **WHEN** the event processing completes
- **THEN** the native context menu appears normally

### Requirement: IContextMenuAdapter facet detection
`WebViewCore` SHALL detect `IContextMenuAdapter` on the adapter and subscribe to its events. If absent, no `ContextMenuRequested` events fire on `WebViewCore`.

#### Scenario: Adapter supports context menu
- **WHEN** adapter implements `IContextMenuAdapter`
- **THEN** `ContextMenuRequested` events are forwarded from adapter to `WebViewCore`

#### Scenario: Adapter lacks context menu support
- **WHEN** adapter does not implement `IContextMenuAdapter`
- **THEN** no `ContextMenuRequested` events fire (native menu behavior unchanged)

### Requirement: Media type information
`ContextMenuRequestedEventArgs.MediaType` SHALL indicate the type of media element right-clicked.

#### Scenario: Image context menu
- **WHEN** user right-clicks on an `<img>` element
- **THEN** `MediaType` is `Image` and `MediaSourceUri` contains the image URL

### Requirement: WebView and WebDialog expose event
Both `WebView` and `WebDialog` SHALL expose `ContextMenuRequested` event.

#### Scenario: Event chain
- **WHEN** context menu is triggered
- **THEN** `WebView.ContextMenuRequested` fires, delegated from `WebViewCore`
