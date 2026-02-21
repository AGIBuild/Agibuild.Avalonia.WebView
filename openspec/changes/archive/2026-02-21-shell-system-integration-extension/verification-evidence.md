## Shell System Integration Extension — Verification Evidence

## KPI Checklist (Current Snapshot)

| KPI | Pass Criteria | Current Status | Evidence |
|---|---|---|---|
| Typed capability safety | Menu/tray/system-action use typed DTOs and deterministic outcome model | ✅ Implemented | `WebViewHostCapabilityOperation` + DTO contracts + `WebViewHostCapabilityCallOutcome` |
| Policy-first governance | Policy evaluates before provider, deny path executes zero provider side effects | ✅ Implemented | `WebViewHostCapabilityBridge.Execute` + deny-path tests for system integration operations |
| Deterministic diagnostics | Capability calls emit machine-checkable outcome metadata | ✅ Implemented | `CapabilityCallCompleted` assertions for menu/tray/system-action in unit/integration tests |
| Shell-domain isolation | System integration failure does not break other shell domains | ✅ Implemented | `ShellSystemIntegrationCapabilityTests` isolation assertions |
| Template web-first DX | Template demonstrates web call -> typed bridge -> capability -> policy -> typed result | ✅ Implemented | `MainWindow.AppShellPreset.cs` + `index.html` path markers + governance assertions |

## Test Commands and Outcomes

### 1) Unit coverage — capability bridge and shell system integration branches
Command:
`dotnet test "tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj" --filter "HostCapabilityBridgeTests|ShellSystemIntegrationCapabilityTests|BridgeIntegrationTests|MultiWindowLifecycleTests|AutomationLaneGovernanceTests"`

Outcome:
- Passed: 40
- Failed: 0

### 2) Focused branch coverage — new system integration deny/failure matrices
Command:
`dotnet test "tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj" --filter "HostCapabilityBridgeTests|ShellSystemIntegrationCapabilityTests"`

Outcome:
- Passed: 11
- Failed: 0

### 3) Automation integration coverage — representative runtime flows
Command:
`dotnet test "tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj" --filter "HostCapabilityBridgeIntegrationTests|ShellProductionValidationIntegrationTests"`

Outcome:
- Passed: 4
- Failed: 0

### 4) Template tests
Command:
`dotnet test "templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj"`

Outcome:
- Passed: 3
- Failed: 0

## Implementation Boundary Notes

- In-scope:
  - Typed menu/tray/system-action capabilities in runtime and shell.
  - Policy-first and deterministic outcome semantics.
  - Template `app-shell` wiring and web demo markers for typed system integration calls.
- Out-of-scope:
  - Auto-update / installer / plugin ecosystem.
  - Non-Avalonia host framework support goals.
  - Full Electron API parity.

## Archive-ready Checklist

- [x] Typed contracts introduced and verified by compile + tests.
- [x] Policy deny path confirmed provider zero-execution for system integration operations.
- [x] Failure isolation verified for provider and policy failures.
- [x] Diagnostics schema stability assertions updated.
- [x] Template governance markers updated for new typed flow.
- [x] Evidence commands recorded with pass/fail outcomes.

## Residual Risks and Owners

| Risk | Impact | Owner | Mitigation / Next Step |
|---|---|---|---|
| Platform-specific menu/tray capability gaps | Behavior drift across OS | Runtime Shell Maintainers | Keep cross-platform contract minimal and extend via optional metadata in follow-up changes |
| System action surface expansion pressure | Scope creep toward Electron full parity | Product/Architecture | Preserve explicit non-goals and gate additions through OpenSpec review |
| Template sample growth | Host glue complexity rises | Template Maintainers | Keep one canonical typed service entry and avoid split capability services |
