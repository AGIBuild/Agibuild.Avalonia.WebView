## 1. Runtime 语义收敛

- [x] 1.1 在 `WebViewShellExperience` 收敛桥未配置时的 deny reason 为稳定 code。
- [x] 1.2 更新受影响单元测试断言，确保语义一致。

## 2. 产品闭环自动化

- [x] 2.1 新增“文件 + 菜单 + 权限恢复”代表性产品流自动化测试。
- [x] 2.2 将新场景接入 `runtime-critical-path.manifest.json`。

## 3. 治理接线

- [x] 3.1 在 `shell-production-matrix.json` 增加对应能力行与证据映射。
- [x] 3.2 更新治理测试 required capability/scenario 列表。

## 4. 验证

- [x] 4.1 运行定向单测/自动化测试并通过。
- [x] 4.2 运行 `npx --yes @fission-ai/openspec validate --all --strict` 并通过。
