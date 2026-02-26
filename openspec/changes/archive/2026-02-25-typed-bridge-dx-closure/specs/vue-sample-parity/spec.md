## ADDED Requirements

### Requirement: Vue sample bridge usage SHALL consume typed bridge client
Vue sample SHALL consume `@agibuild/bridge` typed service client instead of raw `window.agWebView.rpc.invoke` calls in service layer.

#### Scenario: Vue sample app info query uses typed service client
- **WHEN** Vue sample queries app info from bridge
- **THEN** service implementation uses `bridgeClient.getService` typed path with deterministic method contract
