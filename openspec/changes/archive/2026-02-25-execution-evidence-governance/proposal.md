## Why

当前关键路径治理主要验证“文件存在 + 方法存在”，但无法证明场景在最近一次流水线中真实执行并通过。  
需要把治理升级到执行证据层，避免“声明已覆盖、实际未跑”的风险。

## What Changes

- 新增 Build 级关键路径 TRX 校验：关键场景必须在对应测试结果中为 Passed。
- 强化 manifest/matrix 双向一致性治理。
- 明确 `Ci` 与 `CiPublish` 场景上下文（如 package smoke）并纳入校验。

## Capabilities

### Modified Capabilities

- `runtime-automation-validation`
- `shell-production-validation`
- `build-pipeline-resilience`

## Non-goals

- 不改变业务功能实现。
- 不引入外部测试平台依赖。
- 不放宽现有治理门禁。
