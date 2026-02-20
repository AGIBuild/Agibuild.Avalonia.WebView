## Context

- 现有壳能力已具备命令执行与 DevTools API，但缺少“按键手势 -> 壳动作”的统一路由。
- 模板项目目前对快捷键没有可复用抽象，不利于扩展和一致性治理。
- 目标是提供 public API 级别的快捷键路由，避免模板依赖 runtime 内部实现。

## Goals / Non-Goals

**Goals:**
- 在 `Agibuild.Avalonia.WebView` 中新增可复用快捷键路由组件。
- 提供跨平台默认 shell 快捷键集合（主键随平台切换）。
- 将模板 `app-shell` preset 接入该组件并确保可释放、可治理。
- 提供自动化测试覆盖主路径、未命中路径、能力缺失路径。

**Non-Goals:**
- OS 级全局热键服务。
- 用户可配置快捷键持久化模型。
- 引入 runtime shell 内部类型到模板。

## Decisions

### 1) 组件放在 public `Agibuild.Avalonia.WebView` 而非 runtime shell
- **Decision:** 新增 `WebViewShortcutRouter` 于公共包，直接基于 `IWebView`、`ICommandManager`、`OpenDevToolsAsync`。
- **Rationale:** 模板只引用主包即可使用，避免再次引入 runtime 内部耦合。
- **Alternative A:** 仅在模板内手写快捷键处理（拒绝，重复实现、不可复用）。
- **Alternative B:** 放在 runtime shell（拒绝，模板接入风险高、边界不清晰）。

### 2) 默认绑定采用“平台主键 + 标准编辑命令”
- **Decision:** 默认提供 `Copy/Cut/Paste/SelectAll/Undo/Redo` 与 `OpenDevTools`，主键在 macOS 用 `Meta`，其他平台用 `Control`。
- **Rationale:** 与桌面应用主流习惯一致，降低迁移成本。

### 3) 路由结果使用布尔 handled 语义
- **Decision:** `TryExecuteAsync` 返回 `bool`（是否已匹配且执行成功）。
- **Rationale:** 与 UI KeyDown `e.Handled` 语义对齐，宿主接线简单可控。

## Risks / Trade-offs

- **[快捷键冲突] →** 通过可注入自定义 bindings 覆盖默认映射。
- **[命令管理器不可用] →** 返回 false，不吞异常，不做隐式兜底路径。
- **[模板耦合事件生命周期] →** 明确注册/反注册 `KeyDown`，避免事件泄漏。

## Migration Plan

1. 新增快捷键路由类型与默认绑定。
2. 模板 `app-shell` preset 接入路由。
3. 新增路由测试与模板治理断言。
4. 运行目标测试与回归测试。

Rollback: 移除路由组件与模板接线，保留现有命令/API 基线能力。

## Open Questions

- 后续是否增加“宿主菜单命令 -> 同一路由动作”的统一入口（当前不在本变更范围）。
