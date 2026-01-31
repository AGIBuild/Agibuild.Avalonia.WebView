## ADDED Requirements

### Requirement: Platform adapter project skeletons
The solution SHALL include the following platform adapter projects with the specified target frameworks:
- `Agibuild.Avalonia.WebView.Adapters.Windows` targeting `net10.0-windows`
- `Agibuild.Avalonia.WebView.Adapters.MacOS` targeting `net10.0-macos`
- `Agibuild.Avalonia.WebView.Adapters.Android` targeting `net10.0-android`
- `Agibuild.Avalonia.WebView.Adapters.Gtk` targeting `net10.0`

Each project SHALL reference `Agibuild.Avalonia.WebView.Core` and `Agibuild.Avalonia.WebView.Adapters.Abstractions`.

#### Scenario: Platform adapter projects exist and compile
- **WHEN** the solution is built with default settings
- **THEN** only the current OS adapter project is built successfully

#### Scenario: Optional adapters can be enabled by parameters
- **WHEN** the solution is built with an explicit build parameter enabling Android and/or Gtk adapters
- **THEN** the enabled adapter projects are included in the build

### Requirement: Platform adapter class placeholders
Each platform adapter project SHALL include a public adapter class implementing `IWebViewAdapter`:
- `WindowsWebViewAdapter`
- `MacOSWebViewAdapter`
- `AndroidWebViewAdapter`
- `GtkWebViewAdapter`

The adapter classes MAY throw `NotSupportedException` for unimplemented behavior.

#### Scenario: Adapter classes are discoverable
- **WHEN** a consumer loads the platform adapter assembly
- **THEN** the corresponding adapter class exists and implements `IWebViewAdapter`
