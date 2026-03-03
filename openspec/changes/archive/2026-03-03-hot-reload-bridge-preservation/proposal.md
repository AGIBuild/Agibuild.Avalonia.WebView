## Why
During SPA development with HMR (Hot Module Replacement), page reloads cause bridge client state to reset. Pending calls are lost, event subscriptions are dropped, and the developer must manually re-trigger state. Preserving bridge state across HMR reloads would improve the dev loop. Goal: E3 (Hot Reload Integration).

## What Changes
- Bridge client (@agibuild/bridge) detects HMR reload events (Vite, webpack)
- On HMR: serialize pending call queue and event subscriptions to sessionStorage
- On reconnect: restore pending calls and re-subscribe events
- Host-side bridge service preserves registration state (services don't need re-expose)
- BridgeOptions.PreserveStateOnReload = true (default in dev mode)

## Capabilities
### New Capabilities
- `hot-reload-bridge-preservation`: Bridge state preservation across HMR reloads

### Modified Capabilities
- `bridge-npm-distribution`: Add HMR state preservation logic

## Non-goals
- Full page reload preservation (only HMR), C# hot reload integration

## Impact
- Modified: packages/bridge/ (HMR detection, state serialization)
- Modified: RuntimeBridgeService (reconnect handling)
