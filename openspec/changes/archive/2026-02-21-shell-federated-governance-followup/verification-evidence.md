## Verification Evidence

## Objective Mapping

- **G3 / Phase 5.3**: metadata envelope boundary now enforces aggregate payload budget with deterministic deny-before-policy behavior.
- **G4 / Phase 5.3**: profile-governed pruning diagnostics now carry optional revision attribution (`ProfileVersion`, `ProfileHash`) and remain deterministic when omitted.
- **E1/E2 / Phase 5.4-5.5**: template app-shell exposes explicit ShowAbout opt-in marker while preserving default deny path; governance evidence updated accordingly.

## Commands and Outcomes

1. `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests" | Out-String`  
   - Outcome: **Passed** (`38 passed, 0 failed`)

2. `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests" | Out-String`  
   - Outcome: **Passed** (`5 passed, 0 failed`)

3. `openspec validate shell-federated-governance-followup --strict | Out-String`  
   - Outcome: **Valid**

## Evidence Highlights

- Metadata budget boundary:
  - `Exact_budget_inbound_system_integration_metadata_is_allowed_and_dispatched`
  - `Over_budget_inbound_system_integration_metadata_is_denied_before_policy_and_dispatch`
  - `Over_budget_inbound_tray_metadata_is_denied_before_web_delivery`
- Federated pruning revision diagnostics:
  - `Profile_denied_menu_pruning_short_circuits_policy_stage_and_preserves_state`
  - `Profile_allow_then_policy_deny_keeps_previous_effective_menu_state`
  - `Profile_revision_metadata_is_optional_and_emits_stable_null_fields`
  - `Host_system_integration_federated_roundtrip_enforces_showabout_whitelist_and_metadata_boundary`
- Template/governance markers:
  - `Hybrid_template_source_contains_shell_preset_wiring_markers`
  - `Host_capability_diagnostic_contract_and_external_open_path_remain_schema_stable`
