## Verification Evidence

## Objective Mapping

- **G3 / Phase 5.3**: inbound metadata boundary now supports bounded host-configurable aggregate budget with deterministic validation.
- **G4 / Phase 5.3**: profile revision diagnostics now use canonical normalization (`profileVersion`, `profileHash`) with deterministic null fallback for invalid hash values.
- **E1/E2 / Phase 5.4-5.5**: template and governance markers updated to reflect contract v2 guidance while preserving ShowAbout default deny semantics.

## Commands and Outcomes

1. `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests" | Out-String`  
   - Outcome: **Passed** (`40 passed, 0 failed`)

2. `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests" | Out-String`  
   - Outcome: **Passed** (`5 passed, 0 failed`)

3. `openspec validate shell-governance-contract-v2 --strict | Out-String`  
   - Outcome: **Valid**

## Evidence Highlights

- Configurable budget and bounds:
  - `Inbound_metadata_budget_configuration_outside_bounds_is_rejected_deterministically`
  - `Inbound_metadata_budget_uses_configured_in_range_value_deterministically`
  - `Over_budget_inbound_system_integration_metadata_is_denied_before_policy_and_dispatch`
- Profile revision canonical normalization:
  - `Profile_denied_menu_pruning_short_circuits_policy_stage_and_preserves_state`
  - `Profile_allow_then_policy_deny_keeps_previous_effective_menu_state`
  - `Profile_revision_metadata_is_optional_and_emits_stable_null_fields`
  - `Host_system_integration_federated_roundtrip_enforces_showabout_whitelist_and_metadata_boundary`
- Template/governance contract markers:
  - `Hybrid_template_source_contains_shell_preset_wiring_markers`
  - `Host_capability_diagnostic_contract_and_external_open_path_remain_schema_stable`
