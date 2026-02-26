## Why

当前壳层能力已经具备，但“产品体验闭环”还缺一个可执行、可回归、可追责的代表性用户场景：文件能力 + 菜单能力 + 权限恢复路径。  
如果不把这个闭环固化为自动化与关键路径，后续迭代容易出现“局部通过、整体体验退化”。

## What Changes

- 新增产品级自动化场景：在同一条用户流中覆盖文件对话框、菜单应用、权限拒绝与恢复，验证主线体验可恢复且不串扰。
- 将壳层“桥未配置”拒绝语义从不稳定文案收敛为稳定 reason code，避免跨平台/文案改动导致诊断漂移。
- 将该产品场景纳入 runtime critical path 与 shell production matrix 的可追踪证据链。

## Capabilities

### Modified Capabilities

- `webview-shell-experience`: 增加系统能力不可用时的稳定 deny reason 语义与恢复路径约束。
- `runtime-automation-validation`: 增加产品闭环场景 ID，纳入关键路径治理。
- `shell-production-validation`: 增加产品闭环能力行与证据映射。

## Non-goals

- 不引入兼容旧设计的双路径。
- 不修改公共 API 形态。
- 不做平台适配器重构。

## Impact

- Runtime shell implementation: `src/Agibuild.Fulora.Runtime/Shell/WebViewShellExperience.cs`
- Automation tests: `tests/Agibuild.Fulora.Integration.Tests.Automation/HostCapabilityBridgeIntegrationTests.cs`
- Governance artifacts: `tests/runtime-critical-path.manifest.json`, `tests/shell-production-matrix.json`, `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`
