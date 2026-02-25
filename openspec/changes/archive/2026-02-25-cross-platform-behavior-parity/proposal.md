## Why

当前产线矩阵虽然覆盖桌面三平台，但“平台声明”与“覆盖语义”尚未统一到完整跨平台口径（含 iOS/Android）。  
这会导致“支持声明”与“实际证据”之间难以做机器化一致性校验。

## What Changes

- 将 shell 生产矩阵平台集合统一为五平台：`windows` / `macos` / `linux` / `ios` / `android`。
- 对所有 capability 行补齐五平台 coverage 字段；当前未落地平台显式声明 `["n/a"]`。
- 强化治理测试：校验 coverage token 仅允许受控集合，避免自由文本漂移。

## Capabilities

### Modified Capabilities

- `shell-production-validation`: 增加五平台声明与 coverage token 受控语义。
- `webview-compatibility-matrix`: 对齐跨平台声明边界，保证“可支持/暂不支持”都可机读。

## Non-goals

- 不伪造 iOS/Android 的可运行自动化证据。
- 不引入兼容性分支逻辑。
- 不修改运行时行为。

## Impact

- `tests/shell-production-matrix.json`
- `tests/Agibuild.Avalonia.WebView.UnitTests/AutomationLaneGovernanceTests.cs`
- OpenSpec spec deltas for parity governance
