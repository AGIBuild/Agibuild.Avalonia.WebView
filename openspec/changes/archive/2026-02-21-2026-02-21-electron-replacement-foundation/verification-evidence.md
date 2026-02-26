## Web-First Foundation — Verification Evidence

## KPI Checklist (Current Snapshot)

| KPI | Pass Criteria | Current Status | Evidence |
|---|---|---|---|
| Typed IPC safety | Capability outcome uses deterministic `allow/deny/failure` model | ✅ Implemented | `WebViewHostCapabilityCallOutcome` + updated unit/integration assertions |
| Capability governance | Policy executes before provider; deny/policy-failure do not bypass | ✅ Implemented (core path) | Policy-first execution in bridge + external-open deny/failure isolation tests |
| Deterministic diagnostics | Machine-checkable diagnostic payload emitted per capability call | ✅ Implemented (capability path) | `CapabilityCallCompleted` structured event + diagnostics assertions |
| Automation pass-rate | Relevant unit + integration suites green | ✅ Green | Test commands below |
| Template DX time-to-first-feature | Template flow measurable and validated | ✅ Implemented | App-shell template now uses typed shell capability flow + governance assertions |

## Test Commands and Outcomes

### 1) Governance + bridge + capability + lifecycle unit coverage
Command:
`dotnet test "tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj" --filter "FullyQualifiedName~AutomationLaneGovernanceTests|FullyQualifiedName~BridgeIntegrationTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~MultiWindowLifecycleTests"`

Outcome:
- Passed: 36
- Failed: 0

### 2) Host capability automation integration coverage
Command:
`dotnet test "tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj" --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests|FullyQualifiedName~ShellProductionValidationIntegrationTests"`

Outcome:
- Passed: 4
- Failed: 0

## Notes

- External browser strategy no longer uses legacy fallback handler path; it requires typed host capability bridge and reports deterministic policy-domain failures when unavailable.
- Governance coverage includes explicit "no bridge, no fallback navigation" assertion for ExternalBrowser strategy.
- Template app-shell preset now demonstrates: web RPC call -> typed bridge export -> shell capability gateway -> policy decision -> typed result payload.
- Remaining work is tracked in `tasks.md` (notably 6.3 archive-grade evidence packaging).

## Archive-ready KPI Mapping

| KPI | Primary Implementation Evidence | Validation Evidence |
|---|---|---|
| Typed IPC safety | `WebViewHostCapabilityCallOutcome` + typed DTO contracts in template bridge exports | `BridgeIntegrationTests.Web_rpc_call_to_exported_service_*` + `HostCapabilityBridgeTests.Typed_capability_calls_succeed_when_policy_allows` |
| Capability governance | Policy-first execution in `WebViewHostCapabilityBridge.Execute` and external-open enforcement in `WebViewShellExperience` | `HostCapabilityBridgeTests.Denied_policy_skips_provider_and_returns_deny_reason` + `MultiWindowLifecycleTests.External_browser_without_capability_bridge_is_blocked_and_never_falls_back_to_navigation` |
| Deterministic diagnostics | `WebViewHostCapabilityDiagnosticEventArgs` and `CapabilityCallCompleted` event | `HostCapabilityBridgeTests.Capability_diagnostics_are_machine_checkable_with_allow_deny_and_failure_outcomes` + governance schema assertions |
| Automation pass-rate | CI-relevant unit/integration suites for bridge/capability/shell flows | Unit suite (36 pass) + automation integration suite (4 pass) commands listed above |
| Template DX | App-shell template wired with `WebViewShellExperience`, typed host service export, and web demo calls | `AutomationLaneGovernanceTests.Hybrid_template_source_contains_shell_preset_wiring_markers` + template source markers in `index.html` and `MainWindow.AppShellPreset.cs` |
