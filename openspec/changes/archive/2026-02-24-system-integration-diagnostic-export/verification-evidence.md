## Verification Evidence — system-integration-diagnostic-export

| Goal | Implementation Evidence | Verification Command | Result |
|---|---|---|---|
| Runtime exposes stable diagnostic export protocol | `WebViewHostCapabilityDiagnosticExportRecord` + `ToExportRecord()` mapping in bridge diagnostics | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests"` | Pass |
| Deny/failure taxonomy remains machine-readable | Export records assert deny reason and failure category semantics | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests"` | Pass |
| Integration path validates export protocol | New integration test `System_integration_diagnostic_export_protocol_is_machine_checkable` | `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests"` | Pass |
| Long-term lane registration is complete | `runtime-critical-path.manifest.json` + `shell-production-matrix.json` + governance id assertions updated | `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests"` | Pass |
| OpenSpec remains strict-valid | change/spec graph validated | `openspec validate --all --strict` | Pass |
| Full quality gate remains green | Repository-wide tests and coverage pass after diagnostics export changes | `nuke Test` and `nuke Coverage` | Pass (Unit 765 / Integration 149 / Total 914, Line coverage 95.87%) |
