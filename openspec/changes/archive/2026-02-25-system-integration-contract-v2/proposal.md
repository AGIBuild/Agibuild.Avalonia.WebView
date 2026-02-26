## Why

当前系统集成链路已完成双向 typed 与 policy-first 主线，但仍有 3 个开放决策会影响跨平台一致性和长期演进：`ShowAbout` 是否纳入系统动作白名单、tray 事件是否允许原始平台 payload、菜单裁剪是否与 profile 联动。  
本变更用于完成前两项契约收敛（白名单 v2 + tray 事件契约 v2），直接强化 **G3**（Secure by Default）与 **G4**（Contract-Driven Testability），并延续 `ROADMAP` Phase 5 的 `5.2/5.3/5.4/5.5` 落地质量。

## What Changes

- 将系统动作白名单从“固定枚举”升级为“显式版本化白名单 v2”，决策并固化 `ShowAbout` 的支持策略与 deny taxonomy。
- 引入 tray 事件契约 v2：定义标准字段（跨平台稳定）与扩展字段（可选平台元数据）边界，禁止无结构透传。
- 统一 inbound/outbound 系统集成诊断语义，确保 allow/deny/failure 的 correlation 字段稳定可机读。
- 更新 app-shell 模板演示，展示 v2 tray 事件消费路径与系统动作白名单行为。
- 补齐 CT/IT/治理矩阵，覆盖 whitelist allow/deny、tray payload schema、无旁路约束。

## Non-goals

- 不扩展打包式桌面栈 full API parity（插件生态、自动更新全链路等）。
- 不引入多宿主适配目标（WPF/WinForms/MAUI）。
- 不实现 fallback/dual-path 兼容路径。

## Capabilities

### New Capabilities
- （无）

### Modified Capabilities
- `shell-system-integration`: 增加系统动作白名单 v2 契约与 tray 事件 payload v2 治理要求。
- `webview-host-capability-bridge`: 增加 tray 事件 schema v2 与诊断稳定性要求。
- `webview-shell-experience`: 增加白名单 v2 决策入口与 tray 事件扩展字段治理。
- `template-shell-presets`: 更新 app-shell 模板展示 v2 契约消费与治理标记。

## Impact

- Affected code:
  - `src/Agibuild.Fulora.Runtime/Shell/*`
  - `templates/agibuild-hybrid/*`
  - `tests/Agibuild.Fulora.UnitTests/*`
  - `tests/Agibuild.Fulora.Integration.Tests.Automation/*`
- API/contract impact:
  - 系统动作白名单契约版本提升（v2）
  - tray 事件 payload 模型补充扩展字段约束
- Delivery alignment:
  - 作为 Phase 5 后续硬化增量，重点补强 `5.2`（policy-first）、`5.3`（diagnostics）、`5.4`（template）、`5.5`（governance）证据闭环。
