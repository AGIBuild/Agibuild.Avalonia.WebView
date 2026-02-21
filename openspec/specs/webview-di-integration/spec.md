## Purpose
Define dependency-injection integration contracts for WebView adapter factory registration.

## Requirements

### Requirement: Dependency injection project
The solution SHALL include a project named `Agibuild.Avalonia.WebView.DependencyInjection` targeting `net10.0`.
The project SHALL reference `Agibuild.Avalonia.WebView.Core` and `Agibuild.Avalonia.WebView.Adapters.Abstractions`.
The project SHALL depend only on `Microsoft.Extensions.DependencyInjection.Abstractions` for DI APIs and SHALL NOT reference any platform adapter projects.

#### Scenario: DI project is platform-agnostic
- **WHEN** the DI project is built
- **THEN** it compiles without any platform-specific adapter dependencies

### Requirement: IServiceCollection extension entrypoint
The DI project SHALL provide an extension method:
`IServiceCollection AddWebView(this IServiceCollection services, Func<IServiceProvider, IWebViewAdapter> adapterFactory)`
The method SHALL register the `adapterFactory` so it can be resolved as a factory delegate (NOT as a shared `IWebViewAdapter` instance).
The DI integration SHALL ensure each WebView instance can obtain a fresh adapter instance via the registered factory.

#### Scenario: Adapter factory can be registered
- **WHEN** a consumer calls `AddWebView` with a factory delegate
- **THEN** the factory delegate can be resolved from the service provider and used to create an `IWebViewAdapter`
