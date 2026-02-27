## MODIFIED Requirements

### Requirement: Dispatcher contracts are platform-agnostic
The system SHALL define dispatcher contract types in the Core assembly that are platform-agnostic and do not reference platform adapter projects or host-framework-specific types.
Concrete host dispatcher implementations (for example Avalonia UI thread bindings) SHALL be implemented outside `Agibuild.Fulora.Core` and `Agibuild.Fulora.Runtime`.

#### Scenario: Dispatcher contracts compile without host dependencies
- **WHEN** a project references the dispatcher contract types from the Core assembly
- **THEN** it builds without any host-framework-specific package dependencies

#### Scenario: Host dispatcher implementation is isolated from Runtime
- **WHEN** runtime project references are inspected
- **THEN** runtime depends only on `IWebViewDispatcher` contract and not on concrete host dispatcher implementations
