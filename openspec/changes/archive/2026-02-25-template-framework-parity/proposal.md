## Why

模板声明支持 `react/vue`，但当前实际产物仅有 `vanilla` 前端内容，参数存在与生成能力不一致。  
这会直接伤害首次使用体验，降低模板可信度。

## What Changes

- 为模板补齐 React 与 Vue 的 Vite Web 工程骨架。
- 按 `framework` 选择输出对应 Web 工程内容。
- 扩展 `TemplateE2E`：新增 React/Vue 路径的 npm build 验证。

## Capabilities

### Modified Capabilities

- `project-template`
- `template-e2e`

## Non-goals

- 不在本变更中实现 Desktop 自动代理到 Vite dev server。
- 不引入额外前端框架选项。
- 不改模板基础桥接 C# 接口定义。
