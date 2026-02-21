## Context

- 产品北极星是 **替代 Electron 并解决其开发痛点**，而不是追求更多 UI 宿主框架数量。
- AI 时代的核心诉求是：让 Agent 能稳定生成、修改、验证桌面应用代码，而非在复杂宿主细节中反复试错。
- 现有工程已具备 bridge/runtime/adapter 基础，但在“能力建模、策略治理、开发闭环”层面仍可体系化升级。

## Goals / Non-Goals

**Goals**
- 建立面向 Electron 痛点的架构主线：`typed bridge + typed capability + policy-first + automation-first`。
- 提供对 AI Agent 友好的开发边界：强类型契约、可组合能力、确定性错误语义、可验证运行路径。
- 保持 Avalonia 作为当前桌面壳承载，不在本期引入跨 UI 框架扩展目标。
- 降低桌面应用开发复杂度，让 Web 技术团队在同一模型下覆盖 Windows/macOS/Linux。

**Non-Goals**
- 不将 WPF/WinForms 可用性作为阶段验收目标。
- 不在本期交付 Electron 全生态替代（安装器、自动更新、插件市场）。
- 不引入缺乏设计的兼容性分支逻辑。

## Pain Points to Solve (Electron-oriented)

1. **Stringly IPC**：频道名/消息体无类型约束，回归风险高。  
2. **权限边界模糊**：渲染进程可访问能力面过宽，安全治理成本高。  
3. **桌面能力调用分散**：文件、剪贴板、外链、通知等接口缺少统一语义。  
4. **诊断与自动化困难**：运行时行为难以被 CI 和 Agent 稳定验证。  
5. **模板与工程组织复杂**：团队启动成本高，AI 生成代码容易偏离架构。

## Target Architecture

```text
Web App (React/Vue/Svelte + TypeScript)
        |
Typed Bridge (contracts + source-generated stubs/proxies)
        |
Desktop Runtime Kernel
  - Window/Session Lifecycle
  - Capability Gateway
  - Policy Engine
  - Deterministic Error Model
  - Diagnostics Event Stream
        |
Platform Adapters (Windows/Gtk/MacOS/Android/iOS)
        |
Avalonia Desktop Shell Host (current host)
```

## Developer-Friendly Design Principles

### 1) Contract-first, not convention-first
- 所有 bridge/capability 调用必须有编译期类型约束。
- 对 AI Agent 暴露“可推断、可补全、可静态检查”的接口，而不是字符串协议。

### 2) Capability-first, not OS-API-first
- 桌面能力以业务语义建模（Clipboard/FileDialog/ExternalOpen/Notification）。
- 平台差异在 adapter 内吸收，应用层不感知平台分歧。

### 3) Policy-first security
- 能力调用先过策略判定，再进入 provider 执行。
- Deny/Allow/Failure 语义固定化，避免“失败后兜底绕过策略”。

### 4) Automation-first lifecycle
- 关键路径必须可被 contract/integration/automation 三层测试覆盖。
- 事件和错误模型保证可观测、可回放、可归因。

### 5) AI-Agent operability
- 任务路径可声明、可组合、可验证（spec -> tasks -> tests）。
- 工程结构保持单一职责，避免同类能力在多个组件重复实现。

## Decisions

### 1) 保留 Avalonia 作为当前宿主承载
- **Decision:** 本阶段不把“支持 WPF/WinForms”作为目标或验收项。
- **Rationale:** 资源聚焦在 Electron 痛点闭环和开发体验提升。
- **Consequence:** “宿主抽象”仅作为内部架构治理手段，而非对外主卖点。

### 2) 能力网关统一入口（Capability Gateway）
- **Decision:** 桌面能力统一经 capability gateway 暴露，禁止分散直连平台 API。
- **Rationale:** 降低调用心智成本，方便策略统一治理与 Agent 自动生成。
- **Consequence:** 现有散点调用需逐步收敛到统一能力层。

### 3) 策略语义固定化
- **Decision:** 所有能力调用采用一致的 allow/deny/failure 结果模型。
- **Rationale:** 让测试、日志、Agent 推理路径稳定。
- **Consequence:** 需要为历史能力补齐统一错误分类和诊断字段。

### 4) 以“痛点闭环指标”替代“宿主数量指标”
- **Decision:** 阶段成功由开发效率、安全治理、自动化验证覆盖衡量。
- **Rationale:** 直接对应 Electron 迁移价值。
- **Consequence:** roadmap 与任务清单全部改为结果导向指标。

## Risks / Trade-offs

- **风险：过度工程化导致上手变慢**
  - 对策：模板预置最佳路径，隐藏复杂度；文档聚焦“最短可运行路径”。
- **风险：策略层引入调用摩擦**
  - 对策：提供默认策略 profile 与清晰 deny reason。
- **风险：能力抽象不当导致平台特性损失**
  - 对策：核心语义统一，平台扩展能力以可选扩展点提供。

## Migration Plan

1. 重新对齐 capability 与 policy 的统一入口。  
2. 规范错误语义与诊断事件（便于 CI/Agent 自动判定）。  
3. 以模板和集成场景验证开发闭环（web code -> desktop capability -> automation pass）。  
4. 分阶段收敛历史接口到统一架构边界。  

Rollback 策略：若某能力收敛导致开发效率下降，优先回退到上一个稳定网关版本，保持应用层 API 不破坏。
