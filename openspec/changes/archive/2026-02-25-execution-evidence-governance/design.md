## Context

manifest 与 matrix 已形成能力声明，但治理仍偏静态，无法反映“最近一次执行结果”。  
尤其是 critical-path 场景，需要从“声明映射”升级为“执行可证实”。

## Goals

- 用 TRX 结果验证 critical-path 场景执行状态。
- 保证 runtime manifest 与 shell production matrix 双向一致。
- 明确部分场景仅在 `CiPublish` 执行，避免错误期望。

## Decisions

### D1. 新增 Build 目标：验证 critical-path 执行证据

- 读取 `runtime-critical-path.manifest.json`。
- 解析 `unit-tests.trx` 与 `integration-tests.trx`。
- 对非 Build target 场景校验：在对应 TRX 中存在并且结果为 Passed。

### D2. 增加 scenario 执行上下文字段

- 在 manifest 场景中支持 `ciContext`（默认 `Ci`）。
- 对 `package-consumption-smoke` 显式标记 `CiPublish`。
- Build 校验根据当前目标上下文决定是否强制检查。

### D3. 双向一致性治理

- 单测中补上 matrix -> manifest 的反向断言。
- shell capability 的声明和关键路径场景保持双向完整。
