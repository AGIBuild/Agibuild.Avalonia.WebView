## Verification Summary

### Commands

1. `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj`
   - Result: Passed (`699/699`)

2. `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~WebView2TeardownStabilityIntegrationTests"`
   - Result: Passed (`1/1`)

3. `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj`
   - Result: Passed (`126/126`)

### Retry Log (for unstable WV2 teardown lane)

- Retry #1
  - Failure reason: Full automation run timed out in `WebView2TeardownStabilityIntegrationTests` (`TaskCanceledException`, 90s budget in nested desktop run).
  - Adjustment: Isolated and reran the failing test only to remove cold-build noise.
  - Result: Passed (`1/1`, ~76s).

- Retry #2
  - Failure reason: Potential suite-level cold-start interference.
  - Adjustment: Reran the full automation suite after warm-up.
  - Result: Passed (`126/126`, ~77s).

## Requirements Traceability

### `webview-shell-experience` (modified capability)

- Opt-in/non-breaking
  - `Shell_experience_with_empty_options_is_non_breaking_for_all_domains` (CT)
- New-window policy and fallback
  - `NavigateInPlace_policy_preserves_v1_fallback_navigation` (CT)
  - `Delegate_policy_can_handle_new_window_and_suppress_fallback` (CT)
- Deterministic policy order
  - `Download_policy_runs_before_delegate_handler_in_deterministic_order` (CT)
  - `Permission_policy_runs_before_delegate_handler_in_deterministic_order` (CT)
- UI-thread execution
  - `New_window_policy_executes_on_ui_thread` (CT)
  - `Download_policy_executes_on_ui_thread` (CT)
  - `Permission_policy_executes_on_ui_thread` (CT)
- Failure isolation/reporting
  - `Policy_failure_isolated_and_reported_without_breaking_other_domains` (CT)
- Desktop runtime representative flow + stress
  - `Shell_policy_representative_flow_applies_all_domains_end_to_end` (IT)
  - `Shell_policy_stress_cycle_preserves_fallback_and_handler_isolation` (IT)

### `shell-session-policy` (new capability)

- Session policy explicit and deterministic
  - `Session_policy_resolution_is_deterministic_and_propagates_scope_identity` (CT)
- Session integration in shell context
  - `Shell_policy_representative_flow_applies_all_domains_end_to_end` (IT)
