## Why

当前应用壳已覆盖多窗口、权限、下载、DevTools 与 host capability，但“快捷键/加速键映射”仍依赖业务侧手写。需要一个可复用、可测试、默认安全的快捷键路由层，减少模板和宿主应用样板代码。

## What Changes

- 新增可复用的 `WebView` 快捷键路由组件，支持标准编辑命令与 DevTools 快捷键映射。
- 提供平台主键（macOS `Meta` / 其他平台 `Control`）的默认 shell 快捷键集合。
- 将 `agibuild-hybrid` 的 `app-shell` preset 接入该路由组件，实现开箱即用的快捷键体验。
- 增加快捷键路由与模板接线的自动化测试与治理校验。

## Non-goals

- 全局系统级热键注册（OS 级全局监听）。
- 自定义快捷键配置 UI 或持久化配置系统。
- 兼容旧版模板中的手写快捷键逻辑分支。

## Capabilities

### New Capabilities
- `webview-shortcut-routing`: 提供与 `IWebView` 对接的快捷键映射与执行路由能力（命令 + DevTools）。

### Modified Capabilities
- `template-shell-presets`: `app-shell` preset 增加快捷键路由接入与生命周期管理。

## Impact

- **Roadmap alignment**: 对齐 Phase 4 的“系统集成（快捷键）”目标，补齐 desktop-grade shell 体验。
- **Goal alignment**: 强化 **G3**（显式能力边界）与 **G4**（可测试、可验证行为）。
- **Affected areas**: `Agibuild.Avalonia.WebView` 公共 API、模板 `MainWindow.AppShellPreset.cs`、自动化测试与治理测试。
