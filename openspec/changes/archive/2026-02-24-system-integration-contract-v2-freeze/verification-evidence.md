## Verification Evidence — system-integration-contract-v2-freeze

| Goal | Implementation Evidence | Verification Command | Result |
|---|---|---|---|
| Reserved metadata key registry is deterministic | `WebViewHostCapabilityBridge` enforces reserved keys + `platform.extension.*` lane with deny `system-integration-event-metadata-key-unregistered` | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~ShellSystemIntegrationCapabilityTests"` | Pass |
| Inbound timestamp wire is canonicalized | Bridge normalizes `OccurredAtUtc` to UTC millisecond precision before dispatch | `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests"` | Pass |
| Template ShowAbout remains default-deny with explicit runtime opt-in | Template uses `AGIBUILD_TEMPLATE_ENABLE_SHOWABOUT` marker and runtime helper; governance assertions validate marker presence | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests"` | Pass |
| CT matrix reflects new freeze coverage branches | `shell-system-integration-ct-matrix.json` includes unregistered-key and timestamp normalization evidence rows | `openspec validate --all --strict` | Pass |
| Full quality gate (tests + coverage) is satisfied | Repository-wide test and coverage gates pass | `nuke Test` and `nuke Coverage` | Pass (Unit 764 / Integration 148 / Total 912, Line coverage 95.96%) |

## Residual Risks

- Reserved-key registry may need future additions as new template metadata fields are introduced.
- Timestamp canonicalization uses millisecond precision; any future higher-resolution protocol update must be explicitly versioned.
