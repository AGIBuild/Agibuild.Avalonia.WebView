## Why

当前系统集成能力已完成“命令下发”主链路（web -> typed bridge -> capability -> policy -> provider），但缺少“事件回流 + 动态策略裁剪”的闭环，导致真实桌面壳场景（托盘点击、菜单状态联动）仍需宿主侧分散实现。  
现在补齐这 3 个子任务，可直接推进 `ROADMAP` Phase 5 的 `5.2/5.3/5.4/5.5`，并继续强化 `G1/G3/G4` 与 `E1/E2` 的落地质量。

## What Changes

- 增加托盘事件从 host 到 web 的 typed 回流通道，并纳入 policy 与 diagnostics。
- 增加菜单动态裁剪能力（基于 policy/context 的 enabled/visible 计算），保证同上下文结果可确定。
- 收敛系统动作白名单并强化 deny 语义（非白名单动作统一 deterministic deny）。
- 更新模板 app-shell 展示“命令下发 + 事件回流”的 canonical Web-first 流程。
- 补齐 CT/IT/Automation 与 governance 断言，避免新增旁路。

## Non-goals

- 不扩展到 Electron 全量 API（如插件市场、自动更新全链路）。
- 不引入多宿主兼容目标（WPF/WinForms/MAUI）。
- 不引入临时 fallback/dual-path 兼容实现。

## Capabilities

### New Capabilities
- 无

### Modified Capabilities
- `shell-system-integration`: 增加托盘事件回流、菜单动态裁剪、系统动作白名单约束。
- `webview-host-capability-bridge`: 增加系统集成事件回流的 typed 契约与诊断覆盖要求。
- `webview-shell-experience`: 增加系统集成“命令+事件”双向治理入口与隔离约束。
- `template-shell-presets`: app-shell 模板增加事件回流演示与治理标记。

## Impact

- **Goal IDs**: 强化 `G1`（typed contracts）、`G3`（policy-first security）、`G4`（contract-driven testability），并提升 `E1/E2`。
- **Roadmap**: 对齐 Phase 5 deliverables `5.2`（policy-first）、`5.3`（observability）、`5.4`（template flow）、`5.5`（governance）。
- **Affected Areas**:
  - `src/Agibuild.Avalonia.WebView.Runtime/Shell`
  - `templates/agibuild-hybrid/*`
  - `tests/Agibuild.Avalonia.WebView.UnitTests`
  - `tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation`
