## Why

项目的主目标不是“支持更多 UI 框架”，而是成为 **Electron 替代方案**：  
用 AI Agent 更擅长的 Web 技术栈（TypeScript/React/Vue + typed contracts）构建跨平台桌面应用，并系统性解决 Electron 在工程实践中的高频痛点。

这会形成两个长期问题：
- 架构目标容易被“框架复用”牵引，偏离“开发体验与交付效率”主线。
- AI Agent 难以稳定协作：缺少统一契约、可预测能力边界、可自动验证的执行路径。

## What Changes

- 将本变更定位为 **Electron 替代架构基线**，以“Web-first + typed capability + policy-first”为主线。
- 保留 Avalonia 作为当前桌面壳承载，不以 WPF/WinForms 可用性作为阶段目标。
- 建立对 AI Agent 友好的架构约束：能力接口类型化、权限策略显式化、自动化验证前置化。
- 以“开发者痛点闭环”而非“宿主数量”作为阶段验收标准。

## Non-goals

- 本变更不追求 WPF/WinForms/MAUI 适配交付。
- 本变更不一次性实现 Electron 全生态能力（installer、auto-update、插件市场等）。
- 不改变现有平台 adapter 的基础行为语义（导航、桥接、权限、下载等）。

## Capabilities

### New Capabilities
- `electron-replacement-foundation`: 面向 Electron 痛点的架构基线能力（typed bridge/capability/policy/automation）。

### Modified Capabilities
- `webview-contract-semantics-v1`：增强为 AI Agent 可消费的类型契约与稳定语义。
- `webview-host-capability-bridge`：能力边界与策略模型向“应用壳能力”演进。

## Impact

- **Architecture**: 形成 `Web-first app model -> typed bridge -> capability gateway -> policy engine -> platform adapters` 的清晰分层。
- **Product direction**: 将“替代 Electron 的开发体验”设为唯一主航道。
- **Affected areas**:
  - `src/Agibuild.Avalonia.WebView.Core`
  - `src/Agibuild.Avalonia.WebView.Runtime`
  - `src/Agibuild.Avalonia.WebView.Adapters.Abstractions`
  - `src/Agibuild.Avalonia.WebView`（桌面壳承载与开发体验收敛）
  - integration / unit / automation tests（痛点闭环验证）
