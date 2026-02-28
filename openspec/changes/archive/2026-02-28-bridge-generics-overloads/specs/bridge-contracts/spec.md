## ADDED Requirements

### Requirement: Overloaded export methods register with unique RPC names
When `Bridge.Expose<T>(impl)` is called with an interface that has overloaded methods, each overload SHALL be registered with a unique RPC method name based on its visible parameter count.

#### Scenario: Overloaded method RPC handlers are individually addressable
- **WHEN** `Bridge.Expose<ISearchService>(impl)` is called where `ISearchService` has `Search(string q)` and `Search(string q, int limit)`
- **THEN** RPC handlers for `SearchService.search` and `SearchService.search$2` are registered
- **AND** each handler correctly deserializes and invokes its respective overload

### Requirement: Overloaded import proxy dispatches to correct RPC name
When `Bridge.GetProxy<T>()` is called with an interface that has overloaded methods, each proxy method SHALL invoke its specific RPC method name.

#### Scenario: Proxy overload calls correct RPC endpoint
- **WHEN** a proxy for an overloaded import interface is obtained
- **AND** the 2-param overload is called
- **THEN** the proxy sends an RPC request to `Service.method$2`
