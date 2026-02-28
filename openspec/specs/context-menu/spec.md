## Purpose
Define context-menu interception contracts and forwarding behavior for WebView surfaces.

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

### Requirement: GTK adapter raises ContextMenuRequested on right-click
The GTK adapter SHALL connect the WebKitGTK `context-menu` signal and raise the `ContextMenuRequested` event with hit-test data including coordinates, link URI, selection text, media type, media source URI, and editability.

#### Scenario: Right-click on link raises ContextMenuRequested with link URI
- **WHEN** the user right-clicks a link in the GTK WebView
- **THEN** `ContextMenuRequested` is raised with `LinkUri` set to the link's href

#### Scenario: Setting Handled=true suppresses default context menu
- **WHEN** `ContextMenuRequested` is raised and the handler sets `Handled = true`
- **THEN** the native WebKitGTK context menu is suppressed

#### Scenario: Right-click on editable element reports IsEditable
- **WHEN** the user right-clicks an editable input element
- **THEN** `ContextMenuRequested` is raised with `IsEditable = true`

### Requirement: WebView and WebDialog expose event
Both `WebView` and `WebDialog` SHALL expose `ContextMenuRequested` event.

#### Scenario: Event chain
- **WHEN** context menu is triggered
- **THEN** `WebView.ContextMenuRequested` fires, delegated from `WebViewCore`
