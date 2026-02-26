## Context

`template.json` 已声明 `framework` choice（vanilla/react/vue），但仓库中缺少对应 React/Vue 模板内容。  
当前行为本质是“参数可选但无效”，属于 DX 断层。

## Goals

- 让 `--framework react` 与 `--framework vue` 生成可 `npm install && npm run build` 的 Web 工程。
- 保持 `vanilla` 路径不受影响。
- 把 React/Vue 路径纳入 `TemplateE2E` 自动验证。

## Decisions

### D1. 新增模板 Web 工程目录

- 增加 `HybridApp.Web.Vite.React` 与 `HybridApp.Web.Vite.Vue` 两套模板目录。
- `framework` 选择时仅保留匹配目录。

### D2. 保持 Desktop 嵌入式 wwwroot 主线不变

- Desktop 项目继续以 `wwwroot` 作为默认加载路径。
- React/Vue Web 工程作为并行前端产物用于开发与构建验证。

### D3. TemplateE2E 增加前端构建校验

- 在现有模板 E2E 基础上，分别生成 react/vue 变体。
- 对生成的 Web 工程执行 `npm install` + `npm run build`，作为硬验证步骤。
