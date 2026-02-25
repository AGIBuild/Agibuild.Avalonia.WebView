## 1. 平台声明统一

- [x] 1.1 将 shell 生产矩阵平台集合升级到五平台。
- [x] 1.2 为每个 capability 补齐五平台 coverage 字段。

## 2. 语义治理强化

- [x] 2.1 在治理测试中要求五平台都存在。
- [x] 2.2 在治理测试中增加 coverage token 白名单校验。

## 3. 验证

- [x] 3.1 运行 `AutomationLaneGovernanceTests` 定向测试并通过。
- [x] 3.2 运行 `npx --yes @fission-ai/openspec validate --all --strict` 并通过。
