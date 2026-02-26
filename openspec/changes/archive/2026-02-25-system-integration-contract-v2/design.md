## Context

`system-integration-event-flow` 已完成双向 typed 主链路，并在归档中留下 3 个开放问题。  
本增量聚焦前两项高优先级问题：  
1) 系统动作白名单是否纳入 `ShowAbout`；  
2) tray 事件是否允许平台原始 payload 透传。  

该变更属于 Phase 5 的后续硬化（`5.2/5.3/5.4/5.5`），目标是减少契约歧义，保持 **G3**（Secure by Default）与 **G4**（Contract-Driven Testability）在跨平台场景下可验证。

## Goals / Non-Goals

**Goals:**
- 定义系统动作白名单 v2，显式纳入 `ShowAbout`，并统一 deny taxonomy（未知/未授权/未启用）。
- 定义 tray 事件 payload v2：核心标准字段 + 可选扩展字段，禁止“无结构原始透传”。
- 保持 inbound/outbound 诊断字段可机读、可回归（correlation/outcome 稳定）。
- 保证模板 app-shell 仍是 canonical 路径，演示 v2 契约消费。
- 用 CT/IT/治理规则验证“无旁路 + 零执行 deny + 跨平台一致性边界”。

**Non-Goals:**
- 不扩展打包式桌面栈全量 API 覆盖面。
- 不引入多宿主兼容目标（WPF/WinForms/MAUI）。
- 不实现 fallback/dual-path 兼容逻辑。
- 不在本期接入 menu pruning 与 session profile 的联合决策（保留到下一增量）。

## Decisions

### Decision 1: `ShowAbout` 纳入系统动作白名单 v2（默认受 policy 控制）
- Choice: 在 typed action 契约中新增 `ShowAbout`，但执行前仍必须过 policy + whitelist 双重判定。
- Why: `ShowAbout` 属于高频且低风险系统动作；纳入标准白名单可减少模板与业务层私有动作分叉。
- Alternatives:
  - 继续不支持 `ShowAbout`：导致模板/业务层绕行自定义命令，增加不一致。
  - 放开任意字符串动作：违背 G3，审计与测试成本不可控。

### Decision 2: tray payload 采用“标准字段 + 扩展字段”模型
- Choice: v2 事件定义稳定核心字段（event kind、itemId、timestamp、correlation）与 `extensions`（key-value）扩展段。
- Why: 既满足跨平台一致性，又允许平台特定能力按受控字段扩展，不破坏主契约。
- Alternatives:
  - 原始 payload 全透传：平台耦合强，测试与治理不可控。
  - 仅标准字段无扩展：可移植性强，但无法承载真实平台信号差异。

### Decision 3: deny/failure taxonomy 统一并纳入诊断约束
- Choice: 对动作与事件统一 deny code（如 `system-action-not-whitelisted`、`tray-payload-schema-invalid`、`policy-denied`），failure 保持可分类。
- Why: 保证 CI/automation/AI agent 可机读判定，避免同类失败多语义。
- Alternatives:
  - 继续自由文本 deny reason：短期快但不可治理。

### Decision 4: 模板只展示 canonical v2 路径
- Choice: app-shell 模板新增 v2 示例；baseline 继续保持无该 wiring。
- Why: 与已有 template-governance 约束一致，避免文档示例与推荐架构偏离。

## Risks / Trade-offs

- [Risk] `ShowAbout` 行为在不同平台语义不一致（系统菜单/自定义窗口）  
  → Mitigation: v2 仅定义动作语义，不定义 UI 形态；平台实现需满足同一 outcome taxonomy。
- [Risk] `extensions` 字段被滥用为“旁路数据通道”  
  → Mitigation: 限定扩展字段命名空间与大小上限，并由 policy 决定允许键集合。
- [Risk] deny taxonomy 扩展过快导致维护复杂  
  → Mitigation: 采用固定枚举集合并在 governance 中做稳定性断言。

## Migration Plan

1. 在 runtime 契约层新增 `ShowAbout` 与 tray payload v2 类型。
2. 在 bridge/shell 中统一白名单 v2 与 payload schema 校验顺序（先 schema，再 policy，再 provider）。
3. 更新模板 app-shell 示例与治理 marker。
4. 补齐 CT/IT/Automation 矩阵与 release evidence。
5. 以 feature-safe 方式升级：旧 payload 不保留兼容路径，统一迁移到 v2（无 dual path）。

Rollback:
- 回滚到 v1 契约提交点；不保留运行时双版本并行。

## Open Questions

- `ShowAbout` 是否默认在 template policy 中 Allow（建议：默认 Deny，由示例按钮演示可配置打开）。
- `extensions` 的保留键是否需要跨平台统一前缀（建议：`platform.*`）。
- `timestamp` 是否由 host 注入为 UTC ISO8601（建议：是，保障审计一致性）。

## Testing Strategy

- **CT (MockAdapter/MockBridge):**
  - `ShowAbout` allow/deny/failure 矩阵；
  - 非白名单动作 provider 零执行；
  - tray payload v2 schema 校验（合法/缺字段/非法扩展键）。
- **IT/Automation:**
  - tray 事件 roundtrip（含 `extensions`）；
  - `ShowAbout` 在 policy allow/deny 下的 deterministic outcome；
  - 诊断字段稳定性断言（correlation/outcome/deny code）。
- **Governance:**
  - 模板 marker 校验 v2 路径存在；
  - baseline 无 app-shell v2 wiring；
  - 禁止 direct platform dispatch bypass 标记。
