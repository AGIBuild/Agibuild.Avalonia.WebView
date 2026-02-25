## Why

Phase 5 has strong runtime contracts, but template onboarding still lacks an interactive strategy view and scenario switch workflow for AI-agent-driven regression. This change advances roadmap deliverable 5.4 (web-first template flow) and 5.5 (governance evidence) while reinforcing E1/E2.

## What Changes

- Upgrade the app-shell demo page with a system-integration strategy panel that visualizes policy/whitelist outcomes in a deterministic format.
- Add one-click scenario switch controls for ShowAbout deny/allow paths without editing host code each time.
- Add a reusable demo regression script entry point so AI agents can run deterministic template checks.
- Add/adjust governance and automation-facing tests to lock markers and behavior.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `template-shell-presets`: expand app-shell demo to include strategy visualization, scenario toggle ergonomics, and reusable regression script marker.

## Impact

- Affected template files under `templates/agibuild-hybrid/HybridApp.Desktop/wwwroot/` and app-shell preset host wiring.
- Affected governance/unit tests for template marker assertions.
- Affected OpenSpec evidence for Phase 5 web-first DX readiness.

## Non-goals

- No new host capability domain or policy engine redesign.
- No Electron parity expansion beyond template DX ergonomics.
- No production telemetry backend; script is local deterministic validation only.
