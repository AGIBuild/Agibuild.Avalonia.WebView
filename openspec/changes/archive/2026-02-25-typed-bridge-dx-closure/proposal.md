## Why

当前 React/Vue 示例在 bridge 调用上仍混用 raw `rpc.invoke` 与手写 helper，类型体验不统一，难以形成可复制的开发入口。  
需要把 `@agibuild/bridge` 作为统一调用入口，建立稳定的 typed bridge 使用路径。

## What Changes

- 统一 React/Vue 示例的 bridge 调用到 `@agibuild/bridge` 的 typed service client。
- 增强 `packages/bridge` 的 typed service contract 语义，明确参数约束。
- 更新 DX 治理断言，要求样例 bridge 代码出现 typed client 用法而非 raw 调用串。

## Capabilities

### Modified Capabilities

- `bridge-npm-distribution`
- `bridge-typescript-generation`
- `vue-sample-parity`

## Non-goals

- 不做 npm 正式发布流程。
- 不改后端 Bridge 服务接口。
- 不引入兼容旧调用路径的双实现。
