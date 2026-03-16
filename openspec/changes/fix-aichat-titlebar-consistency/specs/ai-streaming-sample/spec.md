## ADDED Requirements

### Requirement: AI chat sample window title SHALL remain semantically consistent across host and web shell
The `avalonia-ai-chat` sample SHALL expose a consistent user-facing product title between native host window chrome and the web shell header, so users do not see conflicting title identity across layers.

#### Scenario: Host and web title identity are aligned
- **WHEN** the desktop sample window is opened and the web shell header is rendered
- **THEN** the host window title and web shell title SHALL represent the same product identity
- **AND** no conflicting naming variants SHALL be shown simultaneously in the sample default UI

### Requirement: AI chat sample titlebar fallback height SHALL match host drag-region contract
The web shell bootstrap fallback titlebar height SHALL match the host drag-region height configured by the sample, so layout remains stable before bridge metrics are hydrated.

#### Scenario: Bridge state is not yet available
- **WHEN** the web shell calculates titlebar height before `windowShellBridgeService.getWindowShellState()` resolves
- **THEN** the fallback value SHALL equal the host drag-region height configured by the desktop sample
- **AND** the rendered top spacing SHALL avoid bootstrap-time `titlebar`/drag-area mismatch

#### Scenario: Bridge state becomes available
- **WHEN** host `chromeMetrics.titleBarHeight` is streamed to the web shell
- **THEN** the web shell SHALL use host-provided `chromeMetrics.titleBarHeight` as the primary value
- **AND** fallback constants SHALL no longer drive steady-state titlebar layout
