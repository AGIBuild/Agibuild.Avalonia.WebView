## Why

打包式桌面栈迁移场景里，菜单/托盘/系统集成是“能不能上线真实桌面产品”的分水岭；当前基线已完成 typed bridge + capability + policy，但这三类壳能力尚未形成统一、可治理、可自动化验证的主干路径。  
现在补齐该路径，可直接承接 `ROADMAP` 中“应用壳能力扩展（菜单/托盘/系统集成）”并延续 Phase 5 的 web-first 痛点闭环目标。

## What Changes

- 新增一个统一的系统集成能力域，覆盖应用菜单、系统托盘与关键系统动作（例如退出、重启、聚焦主窗口）。
- 将菜单/托盘调用纳入现有 typed capability gateway 与 policy-first 执行链，统一 `allow/deny/failure` 结果语义与诊断事件。
- 为模板 `app-shell` 预设提供最小可用 wiring，展示 Web 调用 -> typed bridge -> capability -> policy -> typed result 的完整闭环。
- 增加契约/集成/自动化治理，确保无 policy bypass、无多处重复实现、无非类型化旁路。

## Non-goals

- 不追求打包式桌面栈全 API 对齐（如 auto-update、插件系统、安装器生态）。
- 不以新增 UI 宿主数量（WPF/WinForms/MAUI）作为本变更目标。
- 不把平台特有菜单/托盘全部上浮为公共契约；仅收敛跨平台共性与可治理主路径。

## Capabilities

### New Capabilities
- `shell-system-integration`: 定义菜单/托盘/关键系统动作的统一能力模型、策略治理与诊断契约。

### Modified Capabilities
- `webview-host-capability-bridge`: 扩展 typed capability 操作集合到菜单/托盘/系统动作，并保持统一结果语义。
- `webview-shell-experience`: 扩展 shell experience 对系统集成能力的治理入口与生命周期约束。
- `template-shell-presets`: 扩展 app-shell 模板预设，提供系统集成能力的最小可用演示路径。

## Impact

- **Goal alignment**: 直接推进 `PROJECT.md` 的 `G1`（typed contract）、`G3`（secure by default）、`G4`（contract-driven testability），并增强 `E1/E2` 模板与 DX 价值。
- **Roadmap alignment**: 承接 `ROADMAP` Phase 5（web-first foundation）成果并进入其 Next-phase Outlook 的“应用壳能力扩展（菜单/托盘/系统集成）”。
- **Affected areas**:
  - `src/Agibuild.Avalonia.WebView.Runtime/Shell`
  - `templates/agibuild-hybrid/*`
  - `tests/Agibuild.Avalonia.WebView.UnitTests`
  - `tests/Agibuild.Avalonia.WebView.Integration.Tests.Automation`
