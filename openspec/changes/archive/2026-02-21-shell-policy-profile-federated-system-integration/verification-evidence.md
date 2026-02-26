## Verification Evidence

### KPI -> Implementation -> Evidence

| KPI / Goal | Implementation | Evidence Command | Outcome |
| --- | --- | --- | --- |
| G3 Policy-first system action governance | `WebViewSystemAction.ShowAbout` introduced as typed action, deny-by-default unless explicitly allowlisted in `WebViewShellExperienceOptions.SystemActionWhitelist`. | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests|FullyQualifiedName~ShellExperienceTests"` | Pass (57/57) |
| G3 Bounded inbound payload boundary | `WebViewSystemIntegrationEventRequest.Metadata` introduced with bounded envelope validation before dispatch (`system-integration-event-metadata-envelope-invalid`). | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests|FullyQualifiedName~ShellExperienceTests"` | Pass (metadata allow/deny branches asserted) |
| G4 Federated pruning determinism + isolation | Menu pruning executes in deterministic order `profile -> policy -> mutation`; profile-deny short-circuits policy stage; pruning-profile failure remains isolated from permission/download/new-window domains. | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests|FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests|FullyQualifiedName~ShellExperienceTests"` | Pass (order/conflict/isolation branches asserted) |
| E1/E2 Template federated demo + governance markers | Template app-shell preset adds explicit allowlist marker, profile resolver marker, bounded metadata consumption in web demo, and federated pruning stage display. | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests"` | Pass (12/12) |
| E1 Runtime automation roundtrip for combined scenario | Added integration scenario covering ShowAbout whitelist deny + tray metadata boundary deny/allow + federated pruning profile diagnostics in one flow. | `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests|FullyQualifiedName~ShellPolicyIntegrationTests"` | Pass (9/9) |
| E1 Template bridge contract compatibility | Template bridge DTO updates (`DesktopMenuApplyResult` federated fields + event metadata) remain test-consumable. | `dotnet test templates/agibuild-hybrid/HybridApp.Tests/HybridApp.Tests.csproj` | Pass (3/3) |

### Structured Diagnostics / Deterministic Fields

- Host capability diagnostics asserted for stable fields: `CorrelationId`, `Operation`, `Outcome`, `WasAuthorized`, `DenyReason`, `FailureCategory`.
- Federated pruning stage semantics asserted through deterministic deny reasons and template surface fields (`PruningStage`, `ProfileIdentity`, `ProfilePermissionState`).
- Inbound payload boundary asserted with deterministic deny reason: `system-integration-event-metadata-envelope-invalid`.

### Risk / Residual

- Template desktop direct build (`dotnet build templates/agibuild-hybrid/HybridApp.Desktop/HybridApp.Desktop.csproj`) can fail when template package reference lags repository runtime contract shape (`Agibuild.Fulora` prerelease package mismatch).  
  This is a known residual and does not block repository CT/IT/automation evidence lanes.
