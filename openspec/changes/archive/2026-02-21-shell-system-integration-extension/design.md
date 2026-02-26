## Context

当前 `WebViewHostCapabilityBridge` 已形成 typed + policy-first + deterministic outcome 主干，并在 `WebViewShellExperience` 与模板 `app-shell` 预设中验证了 clipboard/external-open 等路径。  
下一阶段需要把菜单、托盘、系统动作纳入同一主干，避免出现“模板里直接调平台 API”的旁路实现，保持打包式桌面栈迁移场景下的可治理与可自动化验证。

Roadmap 对齐：
- 基于 `ROADMAP` Phase 4 既有交付 `4.3`（host capability bridge）与 `4.5`（template shell presets）继续演进。
- 对齐 Phase 5 `5.1-5.5`（typed capability / policy-first / observability / template flow / governance）。
- 承接 Phase 5 Next-phase Outlook: 应用壳能力扩展（菜单/托盘/系统集成）。

## Goals / Non-Goals

**Goals:**
- 将菜单、托盘、关键系统动作统一进入 typed capability gateway，结果语义保持 `allow/deny/failure`。
- 在 `WebViewShellExperience` 提供单一系统集成治理入口，保证 policy 先决、provider 后执行。
- 在模板 `app-shell` 提供最小可用示例，保持 Web-first 调用路径与最小 host glue。
- 建立 CT/IT/Automation 测试闭环，覆盖策略拒绝、失败隔离、诊断契约稳定性。

**Non-Goals:**
- 不追求打包式桌面栈全生态能力（auto-update、installer、插件体系）。
- 不引入新的 UI 宿主框架目标。
- 不为平台特性差异做大而全公共抽象，仅定义跨平台共性 contract。

## Decisions

### Decision 1: Capability-first domain for system integration
- Choice: 新增统一 capability domain（菜单/托盘/系统动作）并扩展 `WebViewHostCapabilityOperation`。
- Rationale: 保持单一能力入口，符合 G1/G3，避免多入口导致策略绕过。
- Alternatives considered:
  - 在模板直接调用平台 API：实现快，但破坏治理与测试一致性（reject）。
  - 在 `WebViewShellExperience` 单独新增非 capability API：会形成第二套语义（reject）。

### Decision 2: Policy semantics stay identical to existing capability bridge
- Choice: 系统集成能力沿用统一 policy 评估顺序与 deterministic outcome（allow/deny/failure）。
- Rationale: 降低认知成本，便于 CI 与 Agent 做统一判定。
- Alternatives considered:
  - 菜单/托盘使用 bool-return 轻量策略：表达力不足，无法承载 deny reason/failure taxonomy（reject）。
  - 部分操作允许 policy 后置：存在执行旁路风险（reject）。

### Decision 3: Model-driven menu/tray contracts with versionable payloads
- Choice: 使用 typed DTO（menu model / tray state / system action request）作为桥接负载。
- Rationale: 支持增量演进与 codegen，减少 stringly-typed 指令路由。
- Alternatives considered:
  - 字符串命令总线（`"menu:update"`）：易漂移、难重构、难测试（reject）。
  - 每个平台独立 DTO：会破坏跨平台 contract 统一性（reject）。

### Decision 4: Template demonstrates one canonical Web-first path
- Choice: `app-shell` 仅暴露一个 `IDesktopShellService`（或等价单服务）承载菜单/托盘/系统动作入口。
- Rationale: 避免能力逻辑分散到多个 service/component，符合“同一功能单一实现”约束。
- Alternatives considered:
  - 菜单/托盘各自服务分散暴露：短期灵活，长期重复与治理困难（reject）。

## Risks / Trade-offs

- [Risk] 平台菜单/托盘差异导致公共模型过度抽象 → Mitigation: 公共 contract 只覆盖共性字段，平台扩展走可选 metadata 字段。
- [Risk] 系统动作能力（如 quit/restart）误配置造成误操作 → Mitigation: 默认 deny，必须显式策略放行，并记录诊断事件。
- [Risk] 模板示例膨胀，失去“最小 host glue” → Mitigation: 仅保留最小闭环样例，其余高级能力通过后续 preset/profile 扩展。
- [Risk] 测试矩阵扩大导致 CI 时间上升 → Mitigation: CT 覆盖语义主分支，IT 只保留代表性跨平台路径。

## Migration Plan

1. 扩展 capability operation 与 typed DTO（不破坏现有调用签名，新增为主）。
2. 在 `WebViewHostCapabilityBridge` 增加系统集成路由与诊断事件字段复用。
3. 在 `WebViewShellExperience` 增加系统集成治理入口（保持 opt-in，未配置时行为不变）。
4. 更新 `template-shell-presets` app-shell 预设，提供最小菜单/托盘 Web-first 调用样例。
5. 补齐 CT/IT/Automation 与治理断言（schema stability + no bypass）。
6. 验证回归并按证据矩阵输出 KPI 对应结果。

Rollback strategy:
- 通过 feature-flag/配置回退到不启用系统集成能力域，仅保留既有 capability 集合。
- 不修改已有 capability 的语义与错误码，确保降级可预测。

## Open Questions

- 系统动作最小集合是否仅包含 `quit/restart/focus-main-window`，还是同时纳入 `show-about`？
- 托盘交互事件是否需要首期支持从 host 回推到 web（双向），或先单向命令即可？
- 菜单模型是否在首期支持动态权限裁剪（基于 policy/context）？
