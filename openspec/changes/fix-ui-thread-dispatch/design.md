## Context

目标是把 WebView 线程模型从“调用方自担责任”改为“框架兜底”，即开发者在任意线程调用 WebView API 都能得到确定行为。  
结合当前问题和红队评审，旧方案的核心缺陷是：仍保留同步 API 与多种线程路径，导致边界不闭合、并发语义不完整、失败模式不可证伪。

在“项目刚起步、无需兼容历史 API”的前提下，本设计采用**全异步公共 API + 单串行执行器（UI actor）**，彻底移除同步跨线程派发与隐式阻塞风险。

与 ROADMAP 的关系：该设计属于 Phase 3 `3.8 API surface review + breaking change audit` 的收敛项，直接提升 F4（feature reliability）和 G4（contract-driven testability）。

## Goals / Non-Goals

**Goals:**
1. 开发者无需关心是否在 UI 线程调用 WebView API
2. 线程安全边界完全闭合（包括 manager/facade 返回对象）
3. 并发调用具备可验证的顺序与失败语义
4. 不依赖 Avalonia 具体类型，支持未来 WPF/WinForms/MAUI 集成
5. 一次性修复当前 E2E 线程相关失败并防止同类回归

**Non-Goals:**
- 不做兼容层、适配旧签名、双轨 API
- 不在 adapter 层实现线程兜底（adapter 只负责平台能力）
- 不在本次变更中引入新的业务能力（只重塑调用与并发语义）

## Decisions

### D1: 公共 API 改为全异步（不保留同步 API）

**决策**：WebView 对外接口统一为 `Task/Task<T>/IAsyncEnumerable` 形态；同步属性和同步命令全部替换为异步方法。  
示例（语义级，不是最终命名约束）：
- `GoBack()` -> `Task<bool> GoBackAsync()`
- `Refresh()` -> `Task RefreshAsync()`
- `ZoomFactor { get; set; }` -> `Task<double> GetZoomFactorAsync()` / `Task SetZoomFactorAsync(double)`
- `IsDevToolsOpen` -> `Task<bool> IsDevToolsOpenAsync()`
- `AddPreloadScript(string)` -> `Task<string> AddPreloadScriptAsync(string)`
- `ICommandManager.Copy()` -> `Task ExecuteCommandAsync(WebViewCommand.Copy)`

**理由**：
1. 消除同步跨线程调度的活性风险（卡死/死锁）
2. 统一失败语义，避免 sync/async 双模型分叉
3. 让“线程无感”成为契约级保证，而不是调用约定

### D2: WebViewCore 内部采用单串行执行器（UI actor）

**决策**：所有对 adapter 的调用都必须通过 WebViewCore 内部 `OperationQueue` 入队，按 FIFO 串行执行。每个 operation 执行时统一切到 UI dispatcher。

执行链路：
1. API 调用 -> 创建 `WebViewOperation`（带 opId、类型、参数、TCS）
2. 入队到单消费者队列
3. 执行器逐条取出，在 UI 线程执行 adapter 调用
4. 完成后回填 result/exception 到调用方 Task

**关键不变量**：
1. 任一时刻最多 1 个 adapter operation 在执行
2. adapter 永远只在 UI 线程被调用
3. 外部可观察行为按入队顺序线性化
4. 同一 operation 只完成一次（success/fault/cancel 三选一）

### D3: 生命周期状态机显式化（防止悬挂与不确定失败）

**决策**：WebViewCore 定义严格状态机：`Created -> Attaching -> Ready -> Detaching -> Disposed`。  
每个 API 在各状态下有确定行为：
- `Ready`: 正常入队执行
- `Created/Attaching`: 可排队等待（仅允许白名单操作）
- `Detaching/Disposed`: 快速失败，返回统一异常（不入队）

**结果**：避免“销毁过程中卡住”“部分操作悄悄丢失”“偶发挂起”。

### D4: 边界闭合：所有子接口都走同一执行器

**决策**：`ICookieManager`、`ICommandManager`、Bridge 相关调用、DevTools、Find、Preload、Zoom、Screenshot、Print 等全部走同一 `OperationQueue`。  
不允许任何 manager 直接绕过 core 调 adapter。

**结果**：修复红队指出的“通过 `TryGetCommandManager()` 绕过线程边界”问题。

### D5: 统一失败语义与可观测性

**决策**：
1. 每个 operation 都有 `operationId`、`operationType`、`enqueueTs`、`startTs`、`endTs`
2. 失败分层：`Disposed` / `NotReady` / `DispatchFailed` / `AdapterFailed`
3. 所有异常从 operation task 直接返回，不允许 fire-and-forget
4. 结构化日志必须包含线程 ID、状态机状态、operationId

**结果**：可测试、可定位、可审计，避免“说能保证但无法证明”。

### D6: Dispatcher 契约保持异步最小面

**决策**：`IWebViewDispatcher` 保持异步调度能力（`InvokeAsync`），不新增同步 `Invoke`。  
因为公共 API 已全异步，不再需要同步跨线程调度。

**结果**：降低跨框架实现复杂度，避免引入同步阻塞语义。

### D7: 根因闭环要求（PrintToPdf 特殊错误）

**决策**：本变更验收不只看“UI 线程异常消失”，还必须覆盖 Runtime 版本矩阵下的 `PrintToPdf` 结果。  
若线程修复后仍存在 `ICoreWebView2_2` 转换失败，该问题独立建项，不在本设计中被“顺带假设修好”。

### D8: 清除同步兼容入口并补齐异步适配器能力

**决策**：
1. 在 `WebViewCore` / `WebView` / `WebDialog` / `AvaloniaWebDialog` 中移除所有同步兼容入口（包含 obsolete 同步方法/属性）。
2. 将 `WindowsWebViewAdapter` 的 preload 脚本注册改为异步能力，避免 `Task.Wait()/Result` 阻塞 UI 线程。
3. Runtime 在 preload 路径优先使用异步适配器能力，只有在适配器不支持时才回退同步适配器能力。

**结果**：
- “对外 async、内部 sync-blocking” 的设计裂缝被关闭。
- preload script 场景不会再因为 UI 线程阻塞而卡死。
- API 形态与线程模型契约一致（真正 async-first，而非兼容层伪 async）。

### D9: 阻塞等待审计与白名单治理（GetAwaiter().GetResult）

**决策**：
1. `Bridge` 导入代理（`[JsImport]`）不再支持同步返回签名，方法必须返回 `Task/Task<T>`。
2. 在 `src/` 目录引入阻塞等待白名单守卫测试：`GetAwaiter().GetResult()` 仅允许出现在已审计的同步边界（平台回调/适配器同步桥接）。
3. 任何新增阻塞等待都必须先收敛到异步设计；无法避免时需明确落入白名单并附原因。

**结果**：
- 消除可避免的阻塞等待路径（例如 Bridge 导入代理同步路径）。
- 将“严格审查”落成可执行约束，避免后续回归式滥用。

## Risks / Trade-offs

1. **API 全异步导致调用方写法变化**：这是有意成本，换取线程模型与失败语义统一。
2. **串行执行器降低并行度**：这是设计选择；WebView native 调用本身强 UI 线程约束，串行化换来确定性。
3. **队列堆积风险**：通过 operation 监控（队列长度、执行时长分布、超时告警）治理，不以并发执行规避。
4. **生命周期门控复杂度上升**：通过显式状态机和状态转移测试保证可维护性。

## Migration Plan

前提：无历史用户、无兼容要求。

1. 重定义 Core 公共契约为全异步接口（一次性 breaking）
2. 重写 WebViewCore：引入 `OperationQueue + StateMachine + OperationContext`
3. 重写 manager 契约与实现，移除所有同步命令路径
4. 调整 WebView 控件层仅做 API 转发，不做线程判断
5. 更新测试基线（CT/IT/E2E）到新契约
6. 文档与示例同步到全异步模型
7. 移除 WebView/UI 层同步兼容入口，收敛为单一 async API 面
8. 对 `GetAwaiter().GetResult()` 建立生产代码白名单守卫并纳入 CI

## Open Questions

1. `Created/Attaching` 阶段允许排队的操作白名单是否仅限导航与配置，还是全量允许？
2. 是否需要为 operation 提供可选 `CancellationToken`（先不做也可，但需明确）？
3. `OperationQueue` 的队列容量策略是无界还是有界（背压）？
4. 是否公开 operation 诊断事件给上层（仅日志 vs 事件+日志）？

## Testing Strategy

### Contract Tests (CT)
1. 任意线程调用全部公共 API，断言 adapter 调用线程恒为 UI 线程
2. 并发 1000 次混合调用，断言执行顺序与结果线性化一致
3. 生命周期各状态下调用行为符合状态机定义
4. manager/facade 路径调用不可绕过 `OperationQueue`
5. 异常分类与传播一致（无 fire-and-forget）

### Integration Tests (IT/E2E)
1. 复现并验证当前失败项：Screenshot、PrintToPdf、FindInPage、PreloadScript
2. 增加多线程压力 E2E：后台线程高频调用 + UI 交互并发
3. Windows Runtime 版本矩阵验证 `PrintToPdf`，避免伪通过

### Acceptance Criteria
1. 开发者在非 UI 线程调用 API 不再触发“must be called on UI thread”错误
2. 不存在可达的 adapter 直连路径（100% 经由 WebViewCore operation queue）
3. 并发测试结果稳定可复现（无随机失败）
