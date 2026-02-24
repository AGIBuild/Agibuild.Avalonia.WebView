## Verification Evidence

| KPI / Requirement | Implementation Evidence | Verification Command | Outcome |
| --- | --- | --- | --- |
| ShowAbout v2 whitelist path is deterministic | `WebViewSystemAction.ShowAbout` typed contract + whitelist-first gate in `WebViewShellExperience.ExecuteSystemAction()` + policy deny branch test | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests"` | Pass |
| Tray payload v2 core fields are mandatory | `WebViewSystemIntegrationEventRequest` adds `Source` + `OccurredAtUtc`; bridge validates core fields before policy dispatch | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests.Missing_core_fields_are_denied_before_policy_and_dispatch"` | Pass |
| Tray payload v2 extension keys are governed | Bridge enforces `platform.*` namespace + bounded envelope/size + deterministic deny reason codes | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~ShellSystemIntegrationCapabilityTests"` | Pass |
| Structured diagnostics remain machine-checkable | deny taxonomy covered by tests and governance assertions (`core-field-missing`, `metadata-namespace-invalid`, `metadata-budget-exceeded`) | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests|FullyQualifiedName~HostCapabilityBridgeTests"` | Pass |
| Template app-shell demonstrates v2 canonical flow | App-shell emits/consumes `platform.*` metadata, renders ShowAbout allow/deny text, baseline remains free from v2-only wiring | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests.Hybrid_template_source_contains_shell_preset_wiring_markers"` | Pass |
| Automation lane includes tray payload v2 roundtrip scenario | Added `shell-system-integration-v2-tray-payload` scenario and production matrix evidence | `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests"` | Pass |

## Residual Risks

- Template source project direct build still reports pre-existing symbol resolution issues in template context; runtime/unit/integration governance remains green.
- No Electron full API parity work is included in this increment by design.
