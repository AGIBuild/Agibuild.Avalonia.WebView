## 1. 循环稳定性自动化

- [x] 1.1 新增 DevTools 生命周期循环稳定性自动化测试。
- [x] 1.2 校验 deny/allow 分支与权限域隔离的确定性。

## 2. 治理接线

- [x] 2.1 将新场景接入 runtime critical-path manifest。
- [x] 2.2 将新能力接入 shell production matrix。
- [x] 2.3 更新治理 required IDs。

## 3. 验证

- [x] 3.1 运行 `ShellPolicyIntegrationTests` 与 `AutomationLaneGovernanceTests` 定向测试并通过。
- [x] 3.2 运行 `npx --yes @fission-ai/openspec validate --all --strict` 并通过。
