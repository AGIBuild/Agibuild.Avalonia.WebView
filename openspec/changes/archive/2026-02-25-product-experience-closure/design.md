## Context

现有壳层系统能力在“单项能力”维度已有测试，但缺少“产品主线”视角下的闭环验证。  
尤其是权限拒绝后的恢复路径，若未与文件/菜单能力同流验证，容易在真实使用中出现状态污染。

## Goals

- 固化一个跨能力产品闭环：文件能力、菜单能力、权限拒绝与恢复。
- 让系统能力不可用的拒绝语义使用稳定 reason code，便于诊断和治理。
- 将闭环场景接入关键路径与生产矩阵，形成可追踪证据。

## Non-Goals

- 不增加兼容旧语义的 fallback 分支。
- 不扩展新的业务能力类型。
- 不引入新的运行时服务层。

## Decisions

### D1. 系统能力不可用语义统一为稳定 reason code

- 在 `WebViewShellExperience` 内部定义统一常量：`host-capability-bridge-not-configured`。
- 文件、菜单、托盘、系统动作、入站事件在桥未配置时统一返回该 reason code。
- 保留 `ReportSystemIntegrationOutcome` 主时序，不新增防御性旁路。

理由：提升诊断稳定性，避免文案改动触发测试/治理误报。

### D2. 新增产品闭环自动化场景

- 在 `HostCapabilityBridgeIntegrationTests` 增加单一场景：
  - 文件打开/保存成功；
  - 菜单应用成功；
  - 第一次权限请求被拒绝；
  - 切换到恢复策略后权限允许；
  - 已有文件/菜单能力保持可用。

理由：用一个用户可感知流覆盖“核心能力 + 恢复路径”。

### D3. 关键路径与生产矩阵同步接入

- `runtime-critical-path.manifest.json` 增加场景 ID；
- `shell-production-matrix.json` 增加能力行并映射证据；
- 更新治理测试 required IDs，确保变更不会只改一处。

理由：防止“测试有了但治理没接上”的半完成状态。

## Alternatives Considered

### A1. 仅新增单元测试，不加自动化闭环

拒绝：无法验证跨域时序和事件恢复链路。

### A2. 保持英文错误文案，靠文档约定

拒绝：文案天然不稳定，机器治理成本高。

## Rollout

1. 先改 runtime reason code 与对应测试。
2. 增加产品闭环自动化测试。
3. 同步关键路径、生产矩阵与治理测试。
4. 跑定向测试与 OpenSpec 严格校验。

## Risks & Mitigations

- **风险：** reason code 改动引发旧断言失败。  
  **缓解：** 同步更新相关单测断言，保持语义一致。
- **风险：** 新场景导致测试耗时上升。  
  **缓解：** 复用现有 Mock 组件，不引入额外外部依赖。
