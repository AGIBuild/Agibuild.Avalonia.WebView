## Verification Evidence

### KPI -> Implementation -> Evidence

| KPI / Goal | Implementation | Evidence Command | Outcome |
| --- | --- | --- | --- |
| G1 Typed 双向契约（outbound + inbound） | `WebViewHostCapabilityBridge` 新增 `WebViewSystemIntegrationEventRequest`、`DispatchSystemIntegrationEvent`、typed event enum/operation。模板新增 `DrainSystemIntegrationEvents` typed DTO。 | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~HostCapabilityBridgeTests|FullyQualifiedName~AutomationLaneGovernanceTests"` | Pass（28/28） |
| G3 Policy-first + 白名单治理 | `WebViewShellExperience` 新增系统动作白名单 `SystemActionWhitelist`（默认显式集合），非白名单 `system-action-not-whitelisted` deny；入站事件同样走 capability policy。 | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~ShellSystemIntegrationCapabilityTests"` | Pass（含 deny 零执行断言） |
| G4 Deterministic + 可回归 | 菜单裁剪策略接口 `IWebViewShellMenuPruningPolicy`、规范化去重与“仅在 allow 后更新 effective state”；新增 CT 矩阵文件与治理校验。 | `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "FullyQualifiedName~AutomationLaneGovernanceTests"` | Pass |
| E1/E2 模板可演示可自动化 | 模板 `MainWindow.AppShellPreset.cs` 增加 inbound 事件回流排队 + `index.html` 命令下发后 drain host events；新增自动化场景验证 tray round-trip + pruning determinism。 | `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj --filter "FullyQualifiedName~HostCapabilityBridgeIntegrationTests"` | Pass（4/4） |

### Structured Diagnostics Coverage

- Outbound diagnostics: `MenuApplyModel`, `TrayUpdateState`, `SystemActionExecute`.
- Inbound diagnostics: `TrayInteractionEventDispatch`, `MenuInteractionEventDispatch`.
- Stable fields asserted: `CorrelationId`, `Operation`, `Outcome`, `WasAuthorized`, `DenyReason`, `FailureCategory`.

### Risk / Residual

- 模板工程直接 `dotnet build templates/agibuild-hybrid/HybridApp.Desktop/HybridApp.Desktop.csproj` 依赖预发布包版本，存在与仓内最新 runtime 契约不同步风险；不影响仓内 CT/IT 自动化通道结果。
