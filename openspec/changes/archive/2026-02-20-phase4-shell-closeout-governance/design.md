## Context

- `shell-production-matrix.json` 目前集中在 soak/stress 核心路径，但缺少 DevTools policy 与 shortcut routing 的生产证据项。
- `runtime-critical-path.manifest.json` 也未将上述能力列为 release-critical 场景。
- 这些能力已实现且有自动化测试，缺的是治理层映射与不可回退约束。

## Goals / Non-Goals

**Goals:**
- 将 DevTools policy isolation 与 shortcut routing 纳入生产矩阵和关键路径。
- 通过治理测试锁定必需场景 ID，防止清单漂移。
- 同步 ROADMAP 完成状态，反映阶段交付真实情况。

**Non-Goals:**
- 不修改壳运行时行为。
- 不新增能力 API。

## Decisions

### 1) 以“已有可执行测试”作为证据源
- **Decision:** 仅引用已存在并稳定通过的自动化测试方法作为 evidence。
- **Rationale:** 保持治理与实现一致，避免文档先行但无可执行验证。

### 2) 在治理测试中增加“必需 ID”约束
- **Decision:** 对 runtime-critical-path 增加必需场景 ID；对 shell production matrix 增加必需 capability ID。
- **Rationale:** 将收口结果固化为可回归的 CI 约束。

## Risks / Trade-offs

- **[清单膨胀] →** 只纳入 release-critical 场景，避免把所有测试都塞进关键路径。
- **[测试重命名导致清单失效] →** 继续沿用现有“文件+方法名存在性”治理断言，变更时即时失败。

## Migration Plan

1. 更新 matrix 与 critical path manifest。
2. 更新治理测试必需 ID。
3. 更新 ROADMAP 完成状态。
4. 运行相关测试并归档证据。

Rollback: 回退清单与治理测试改动，不影响运行时能力。
