## Why

当前 DevTools 治理已有功能性覆盖，但“反复创建/销毁 shell scope”下的稳定性证据仍偏弱。  
如果不做循环稳定性验证，容易在高频操作中出现策略处理器残留或状态漂移。

## What Changes

- 新增 DevTools 策略循环稳定性自动化场景：重复创建/释放 shell，并验证 deny/allow 行为与权限域隔离保持确定性。
- 将该稳定性场景纳入 runtime critical path 与 shell production matrix。
- 强化治理 required IDs，确保后续回归不会漏跑该场景。

## Capabilities

### Modified Capabilities

- `webview-shell-experience`: 增加 DevTools 循环稳定性约束。
- `runtime-automation-validation`: 增加 DevTools 循环稳定性关键路径场景。
- `shell-production-validation`: 增加 DevTools 循环稳定性能力行与证据映射。

## Non-goals

- 不改 DevTools API 形态。
- 不增加兼容旧语义的 fallback 分支。
- 不扩展新 UI 功能。

## Impact

- `tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation/ShellPolicyIntegrationTests.cs`
- `tests/runtime-critical-path.manifest.json`
- `tests/shell-production-matrix.json`
- `tests/Agibuild.Avalonia.WebView.UnitTests/AutomationLaneGovernanceTests.cs`
