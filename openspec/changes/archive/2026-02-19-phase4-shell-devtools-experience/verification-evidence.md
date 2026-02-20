## Verification Summary

### Commands

1. `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj --filter "FullyQualifiedName~ShellExperienceTests"`
   - Result: Passed (`20/20`)

2. `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~ShellPolicyIntegrationTests"`
   - Result: Passed (`3/3`)

3. `dotnet test tests/Agibuild.Avalonia.WebView.UnitTests/Agibuild.Avalonia.WebView.UnitTests.csproj`
   - Result: Passed (`722/722`)

4. `dotnet test tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/Agibuild.Avalonia.WebView.Integration.Tests.Automation.csproj`
   - Result: Passed (`134/134`)

## Retry Log

- No failing command remained after implementation; a single compile-time type mismatch in test scaffold (`IPlatformHandle` namespace qualification) was fixed immediately before rerun.

## Requirements Traceability (`webview-shell-experience` delta)

- **DevTools operation executes when policy allows**
  - `DevTools_policy_allow_executes_open_close_and_query_operations`
    (`tests/Agibuild.Avalonia.WebView.UnitTests/ShellExperienceTests.cs`)

- **DevTools operation is blocked when policy denies**
  - `DevTools_policy_deny_blocks_operation_and_reports_error`
    (`tests/Agibuild.Avalonia.WebView.UnitTests/ShellExperienceTests.cs`)

- **DevTools deny/failure does not break other shell domains**
  - `DevTools_policy_deny_is_isolated_and_permission_domain_remains_deterministic`
    (`tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/ShellPolicyIntegrationTests.cs`)
