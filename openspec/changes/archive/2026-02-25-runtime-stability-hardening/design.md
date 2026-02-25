## Context

现有 DevTools 策略测试证明了“单次调用可控”，但对“高频循环与 scope 重建”的稳定性覆盖不足。  
稳定性问题通常出现在重复生命周期中，因此需要一个专门的循环用例作为硬门禁。

## Goals

- 验证 DevTools deny/allow 在多轮 shell 重建中保持确定性。
- 验证 DevTools 域失败不会污染权限域（隔离性保持）。
- 把稳定性场景纳入关键路径与生产矩阵证据链。

## Non-Goals

- 不改动 DevTools runtime 实现路径。
- 不提升到跨进程/真实浏览器性能压测。
- 不引入新配置项。

## Decisions

### D1. 增加 DevTools 循环稳定性自动化场景

- 在 `ShellPolicyIntegrationTests` 新增循环测试（多轮 shell create/dispose）。
- 每轮校验：
  - deny 策略时 DevTools open/close/query 结果稳定为 blocked；
  - allow 策略时行为稳定；
  - 权限域仍按策略确定执行。

### D2. 将循环场景纳入治理制品

- `runtime-critical-path.manifest.json` 增加 `shell-devtools-lifecycle-cycles`。
- `shell-production-matrix.json` 增加同名 capability 行并指向可执行证据。
- 更新治理 required IDs。

## Alternatives Considered

### A1. 仅在现有单次测试里加几次重复调用

拒绝：无法覆盖 shell scope 重建与事件解绑风险。

### A2. 只做单元测试

拒绝：域隔离与生命周期交互需要自动化层验证。

## Rollout

1. 新增循环稳定性测试。
2. 接入 manifest + matrix + governance required IDs。
3. 执行定向自动化和治理测试，OpenSpec 严格校验。

## Risks & Mitigations

- **风险：** 循环次数过高导致测试时间上升。  
  **缓解：** 控制在中等迭代数，保持稳定且成本可接受。
- **风险：** 错误计数断言过严导致误报。  
  **缓解：** 以策略分支和调用次数推导确定性计数规则。
