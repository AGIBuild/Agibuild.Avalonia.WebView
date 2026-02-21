## Context

上一阶段已完成系统集成“命令下发”主链路：`web -> typed bridge -> capability -> policy -> provider`，并落地 menu/tray/system-action 的基础能力。  
当前缺口是：缺少 host -> web 的 typed 事件回流、缺少菜单动态裁剪策略模型、系统动作白名单边界仍需在契约层进一步收敛。

Roadmap 对齐：
- 对齐 Phase 5 deliverables `5.2`（policy-first）、`5.3`（agent-friendly observability）、`5.4`（web-first template flow）、`5.5`（governance）。
- 对齐 `PROJECT.md` 目标：`G1/G3/G4`，并增强 `E1/E2` 的模板与开发体验闭环。

## Goals / Non-Goals

**Goals:**
- 建立系统集成“命令+事件”双向 typed 闭环，避免宿主层分散事件桥接。
- 将菜单动态裁剪纳入 policy/context 决策，保证同输入下结果可确定（deterministic）。
- 将系统动作明确收敛为白名单契约并统一 deny/failure 语义。
- 用 CT/IT/Automation/Governance 验证无旁路、可观测、可回归。

**Non-Goals:**
- 不扩展 Electron 全 API（插件、自动更新、安装器生态）。
- 不做新宿主框架适配目标（WPF/WinForms/MAUI）。
- 不引入 fallback/dual-path 兼容路径。

## Decisions

### Decision 1: Event flow MUST reuse typed bridge/capability contracts
- Choice: tray/menu 事件回流通过现有 typed bridge contract（事件 DTO + typed event channel），并受 policy 约束。
- Why: 避免新增“事件旁路通道”破坏 G3/G4；保证事件也可被 CI/Agent 判定。
- Alternatives:
  - 直接在模板内拼字符串消息分发（reject：不可治理、不可静态验证）。
  - 宿主事件直接操作 web runtime 全局对象（reject：绕过 capability/policy）。

### Decision 2: Dynamic menu pruning is policy-derived, not UI-derived
- Choice: 菜单可见性/可用性由 policy+context 生成，UI 仅消费结果。
- Why: 保证单一职责与可测试性，避免菜单逻辑散落在模板/视图层。
- Alternatives:
  - 前端自己判断 enabled/visible（reject：策略边界外泄）。
  - provider 层硬编码（reject：跨场景复用差、测试矩阵不可控）。

### Decision 3: System action whitelist is explicit contract
- Choice: 仅允许契约定义的 action（如 quit/restart/focus-main-window/show-about* 取决于本期决策），其余 deterministic deny。
- Why: 防止能力面无界扩张，符合 G3。
- Alternatives:
  - 任意 action string passthrough（reject：高风险、不可审计）。

### Decision 4: Diagnostics must cover outbound + inbound paths
- Choice: 同时覆盖 command outbound 与 event inbound 的 correlation/outcome metadata。
- Why: 满足 `5.3/5.5` 的机器可判定证据要求。

## Risks / Trade-offs

- [Risk] 事件风暴导致桥接噪声与测试不稳定 → Mitigation: 定义事件去抖/幂等语义与速率上限。
- [Risk] 菜单动态裁剪规则复杂化 → Mitigation: 约束策略输入模型，禁止跨层状态读取。
- [Risk] 平台托盘能力差异 → Mitigation: 公共契约保留最小集合，平台差异走可选 metadata。
- [Risk] 白名单收敛引发“功能不够用”争议 → Mitigation: 通过 OpenSpec 增量扩展，不开放自由字符串动作。

## Migration Plan

1. 在 bridge/capability 契约新增事件 DTO 与事件分发入口（typed）。
2. 在 shell experience 增加 inbound governance 入口与动态菜单裁剪流程。
3. 将系统动作执行路径改为显式白名单判定（非白名单 deny）。
4. 更新模板 app-shell，演示“web->host 命令 + host->web 事件”闭环。
5. 补齐 CT/IT/Automation/Governance：包含 deny 零执行、failure 隔离、schema 稳定性。
6. 形成 verification evidence 与 KPI 映射。

Rollback:
- 通过配置关闭事件回流域，仅保留已稳定命令路径；
- 不回退结果语义模型（allow/deny/failure）与现有 capability API。

## Open Questions

- 首期白名单是否包含 `show-about`（建议可选但默认关闭）？
- tray click 是否只回流“语义事件”，还是带原始平台 payload？
- 菜单动态裁剪是否允许按 window scope + permission profile 联合决策？

## Testing Strategy

- **CT (MockAdapter/MockBridge):**
  - command outbound 与 event inbound 的 allow/deny/failure 矩阵；
  - 动态菜单裁剪幂等性与确定性断言；
  - 系统动作白名单拒绝分支 provider 零执行断言。
- **IT/Automation:**
  - tray click 回流到 web 的端到端路径；
  - 菜单裁剪策略变更触发 UI 可见性变化的可重复断言；
  - 诊断 payload 机器可判定 schema 断言。
- **Governance:**
  - 防旁路标记（禁止 direct platform dispatch 绕开 capability/policy）；
  - 模板 marker 断言（必须使用 typed 双向路径）。
