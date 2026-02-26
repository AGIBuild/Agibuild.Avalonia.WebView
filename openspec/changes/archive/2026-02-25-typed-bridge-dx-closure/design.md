## Context

示例项目是开发者第一触点。当前 Vue 使用 raw `rpc.invoke`，React 使用本地 helper，导致 DX 不一致。  
仓库内已有 `@agibuild/bridge` 包，应作为统一 typed client 基线。

## Goals

- 建立统一 typed bridge 调用入口。
- 保持调用语义清晰：仅允许无参或单个 object 参数。
- 用治理测试防止样例回退到 raw 调用。

## Decisions

### D1. 使用 `bridgeClient.getService<T>()` 统一服务调用

- React/Vue service 文件都改为 `@agibuild/bridge` typed service 模式。
- 业务层保持现有 `appShellService/fileService/...` 导出，避免页面层大改。

### D2. 收敛 bridge service method 参数语义

- `packages/bridge` 中服务方法参数限定为“无参或单个 object 参数”。
- 非 object 参数直接抛错，避免隐式参数映射歧义。

### D3. DX 治理改为检查 typed 客户端用法

- 治理测试不再依赖 `AppShellService.getAppInfo` raw 字符串。
- 改为断言示例 bridge 服务中存在 `bridgeClient.getService` 与 `@agibuild/bridge` import。
