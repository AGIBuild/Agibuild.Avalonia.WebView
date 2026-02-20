## Why

Phase 4 已实现 DevTools policy 与 shortcut routing，但当前生产矩阵与关键路径清单尚未完整覆盖这两项壳能力，导致 release-readiness 证据不完整，回归防线存在空洞。

## What Changes

- 将 DevTools policy 隔离验证与 shortcut routing 验证纳入 `shell-production-matrix.json`。
- 将上述场景纳入 `runtime-critical-path.manifest.json`。
- 扩展治理测试，确保关键场景 ID 与矩阵能力项不可被误删。
- 同步更新 `ROADMAP.md` 的完成状态（Phase 3.5 / Phase 4）。

## Non-goals

- 引入新的运行时壳功能。
- 调整现有 shell 策略执行语义。
- 新增平台适配器能力范围。

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `shell-production-validation`: 扩展生产验证覆盖面，纳入 DevTools policy 与 shortcut routing 的治理证据。

## Impact

- **Roadmap alignment**: 收口 Phase 4 交付并补齐可审计证据链。
- **Goal alignment**: 强化 **G4**（contract-driven testability）并巩固 **G3** 下的策略回归防线。
- **Affected areas**: `tests/shell-production-matrix.json`、`tests/runtime-critical-path.manifest.json`、`AutomationLaneGovernanceTests`、`openspec/ROADMAP.md`。
