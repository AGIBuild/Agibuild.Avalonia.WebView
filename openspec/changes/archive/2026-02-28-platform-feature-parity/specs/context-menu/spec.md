## MODIFIED Requirements

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
