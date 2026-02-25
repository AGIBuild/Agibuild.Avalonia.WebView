## ADDED Requirements

### Requirement: Shell unavailable bridge path SHALL emit stable deny reason code
When host capability bridge is not configured, shell system-integration entry points SHALL return deterministic deny results with a stable reason code (`host-capability-bridge-not-configured`).

#### Scenario: Bridge unavailable deny reason remains stable across system-integration entry points
- **WHEN** host invokes menu/tray/system-action/inbound-event shell entry points without a configured host capability bridge
- **THEN** each result is denied with reason `host-capability-bridge-not-configured` and no provider execution occurs

### Requirement: Product-level capability flow SHALL remain recoverable after permission deny
Shell experience SHALL preserve file and menu capability behavior across permission deny/recover transitions in the same runtime flow.

#### Scenario: Permission recovery does not break file and menu capability path
- **WHEN** permission is denied in one shell scope and later allowed in a recovery shell scope
- **THEN** file dialog and menu model capability paths continue to return deterministic success outcomes
