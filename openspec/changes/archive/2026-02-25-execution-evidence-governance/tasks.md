## 1. Build 治理升级

- [x] 1.1 增加 critical-path TRX 执行证据校验目标。
- [x] 1.2 将校验接入 CI 目标链路。

## 2. 清单语义升级

- [x] 2.1 在 runtime manifest 增加 `ciContext` 字段并标记 package smoke。
- [x] 2.2 补充治理测试，校验 ciContext 与 Build 目标关系。

## 3. 一致性治理强化

- [x] 3.1 增加 matrix -> manifest 反向一致性断言。

## 4. 验证

- [x] 4.1 运行治理单测通过。
- [x] 4.2 运行 `nuke Test` 通过。
- [x] 4.3 运行 `openspec validate --all --strict` 通过。
