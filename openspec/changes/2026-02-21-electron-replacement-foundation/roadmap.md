# Roadmap — Electron Replacement Foundation (Draft)

## Vision

构建面向 AI 时代的软件开发架构：  
让团队使用 Web 技术（TypeScript + modern frontend）开发跨平台桌面应用，并系统性解决 Electron 在类型安全、能力治理、自动化验证和工程复杂度上的痛点。

## Milestones

| Milestone | Window | Exit Criteria |
|---|---|---|
| M0 Objective Reset | Week 1 | 路线从“多宿主复用”切换为“Electron 痛点闭环”并锁定验收指标 |
| M1 Typed Capability Gateway | Week 1-3 | 桌面能力统一入口、统一结果语义、关键能力契约测试通过 |
| M2 Policy-first Runtime | Week 3-4 | 能力调用全部先过策略判定，deny/failure 行为确定且可测试 |
| M3 Agent-friendly Observability | Week 4-5 | 关键路径输出结构化诊断并可被 CI/Agent 自动判定 |
| M4 Web-first Template Flow | Week 5-6 | 模板实现“web call -> capability -> policy -> typed result”最短闭环 |
| M5 Production Governance | Week 6-7 | release checklist 与自动化证据覆盖上述痛点指标 |

## Deliverables by Track

### Architecture Track
- Typed bridge + typed capability gateway
- Policy engine with deterministic decision model
- Runtime diagnostics contracts for automation

### Compatibility Track
- 保持现有 Avalonia 路径稳定可用
- 现有样例和模板回归不破坏
- 避免引入分散重复的 capability 实现

### Validation Track
- Contract tests: capability + policy matrix
- Integration tests: typed bridge + capability E2E
- Automation tests: critical path deterministic diagnostics

## Risks and Gates

| Gate | Risk | Gate Condition |
|---|---|---|
| G1 | 目标偏移到“框架适配数量” | 里程碑验收不包含 WPF/WinForms 作为必要项 |
| G2 | 能力调用语义不一致 | allow/deny/failure 契约一致性测试全通过 |
| G3 | 策略被绕过 | deny 情况下 provider 0 次执行（自动化断言） |
| G4 | 诊断不可自动消费 | 关键路径诊断事件满足机器可判定格式 |

## Next-phase Outlook

完成本路线后，进入下一层 Electron 替代能力：
- 应用壳能力扩展（菜单/托盘/系统集成）
- 打包签名与更新链路
- AI Agent 原生工作流（从 spec 到验证证据的自动闭环）
