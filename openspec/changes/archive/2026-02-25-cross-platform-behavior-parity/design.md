## Context

当前 shell 生产矩阵对桌面平台已有覆盖，但在“平台全集声明”与“coverage 语义规范”上仍不够严格。  
如果不收敛 token 和平台键，后续很容易出现矩阵字段漂移、治理误判或人工解释成本上升。

## Goals

- 将平台声明标准化为五平台统一口径。
- 所有 capability 必须显式给出五平台 coverage。
- coverage token 必须受控（机器可判定）。

## Non-Goals

- 不在本变更中新增 iOS/Android 自动化跑道。
- 不调整 runtime 能力执行逻辑。
- 不增加多套矩阵文件。

## Decisions

### D1. 平台全集固定为五平台

- 在 `shell-production-matrix.json` 的 `platforms` 中固定声明：
  `windows`, `macos`, `linux`, `ios`, `android`。
- 治理测试同步要求这五个平台全部存在。

### D2. 未覆盖平台显式 `n/a`

- 每个 capability 的 coverage 必须包含五个平台键。
- iOS/Android 当前统一使用 `["n/a"]`，明确“已声明但未覆盖”而不是遗漏字段。

### D3. coverage token 白名单治理

- 仅允许 `ct` / `it-smoke` / `it-soak` / `n/a`。
- 治理测试逐 token 校验，防止自由文本。

## Alternatives Considered

### A1. 继续保持三平台矩阵

拒绝：无法表达完整跨平台策略边界，后续会重复返工。

### A2. iOS/Android 先留空数组

拒绝：空数组无法表达语义，且会削弱治理可读性。

## Rollout

1. 更新 `shell-production-matrix.json` 五平台 coverage。
2. 更新治理测试平台与 token 校验逻辑。
3. 运行治理测试与 OpenSpec 严格校验。

## Risks & Mitigations

- **风险：** 未来新增 token 时触发失败。  
  **缓解：** 明确由 spec 驱动扩展白名单，不允许隐式扩散。
- **风险：** 五平台声明被误解为“已全部支持”。  
  **缓解：** 通过 `n/a` 明确标记“声明但当前不覆盖”。
