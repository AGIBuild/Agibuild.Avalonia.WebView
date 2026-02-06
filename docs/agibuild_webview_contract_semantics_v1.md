# Agibuild.Avalonia.WebView 契约语义规范 v1

> 本文用于“把语义写死”，支撑：
> - `docs/agibuild_webview_compatibility_matrix_proposal.md`（Baseline 可验证）
> - `docs/agibuild_webview_contract_tests_v1.md`（CT 可执行且稳定）

---

## 1. 适用范围

- 本规范定义 **Baseline** 的必达语义（Extended 允许差异但必须记录差异条目）。
- 本规范对外约束对象为 `IWebView` / `IWebDialog` / `IWebAuthBroker` 及其事件参数类型。

---

## 2. 线程与调度语义（硬规则）

### 2.1 术语

- **UI 线程**：宿主（Avalonia）约定的 UI 线程上下文。
- **Dispatcher**：Core 持有的调度抽象，用于把执行与事件回发到 UI 线程（测试中可替换）。

### 2.2 API 调用线程规则

- **Async API（允许任意线程调用，必须 marshal）**
  - `NavigateAsync`
  - `NavigateToStringAsync`
  - `InvokeScriptAsync`
  - `IWebAuthBroker.AuthenticateAsync`
  - 规则：调用方线程不限；实现必须把实际执行（含 adapter 调用）marshal 到 UI 线程。

- **Sync API（必须 UI 线程调用）**
  - `GoBack/GoForward/Refresh/Stop`
  - 规则：若非 UI 线程调用，抛 `InvalidOperationException`；不得隐式切线程后返回值（避免不确定同步语义）。

### 2.3 事件线程规则

- 所有对外事件（`NavigationStarted/NavigationCompleted/...`）必须在 UI 线程触发。
- 若 adapter 在非 UI 线程上报“native 事件”，Core 必须 marshal 到 UI 线程再触发对外事件。

---

## 3. 生命周期与资源语义（硬规则）

### 3.1 状态（概念模型）

- `New`：实例已创建但未初始化 native
- `Ready`：native 初始化完成，可接受导航/脚本等请求
- `Navigating`：存在“当前活动导航”（Active Navigation）
- `Disposed`：资源释放完成

> 具体内部状态机可更细，但对外必须满足本规范的可观测行为。

### 3.2 Disposed 后行为

- **Sync API**：抛 `ObjectDisposedException`
- **Async API**：返回的 `Task` 必须快速 fault，为 `ObjectDisposedException`（不得悬挂）
- **事件**：Disposed 后不得再对外触发任何事件（adapter 上报应被忽略）

---

## 4. 导航语义（核心不变量）

### 4.1 NavigationId（强制引入）

- 每次“导航请求”都生成一个唯一 `NavigationId`（GUID 或递增序号均可，但必须单实例内唯一）。
- 以下操作均属于“导航请求”，必须产生新的 `NavigationId` 并遵守事件序列：
  - `NavigateAsync(uri)`
  - `NavigateToStringAsync(html)`
  - `GoBack()/GoForward()/Refresh()`
  - `Source = uri`（等价于发起 `NavigateAsync(uri)`）
- **Native-initiated navigation（网页内部触发）**同样属于“导航请求”，例如：
  - 用户在页面内点击链接 `<a href=...>`
  - 重定向（302 / meta refresh）
  - 脚本触发（`window.location=...`）
  - history 导航（back/forward）

> v1 目标：不允许出现“某些跳转场景无法控制任由网页自我执行”的 case。所有主 frame 的导航都必须可被拦截与取消。

### 4.2 并发/重入规则（Latest-wins）

- 若存在活动导航 `nav1` 未完成，又发起新导航 `nav2`：
  - `nav1` 必须最终触发 `NavigationCompleted(Status=Superseded)`（或 `Canceled`，二选一，v1 固定为 **Superseded**）
  - `nav2` 正常进行 Started/Completed
- 禁止出现：某个 `NavigationId` 永远不 Completed（悬挂）。

### 4.3 事件序列与取消（Exactly-once）

对每个 `NavigationId` 必须满足：

1. 触发 `NavigationStarted(NavigationId, RequestUri, Cancel=false)`  
2. 若 `Cancel=true`：
   - 对“由 API 发起”的导航：**不得**调用 adapter 执行 native 导航
   - 对“Native-initiated”导航：必须通过 adapter-host 回调返回 deny，并要求 adapter 取消该 native 导航
   - 必须触发 `NavigationCompleted(NavigationId, Status=Canceled)`
   - 相关 Task 必须完成（不抛也可，但必须可观测；v1 约定 `NavigateAsync` 以“正常完成但 Completed 状态为 Canceled”结束）
3. 若未取消：
   - adapter 执行 native 导航并回报结果
   - 必须触发 `NavigationCompleted(NavigationId, Status=Success|Failure|Superseded)`

**Exactly-once 不变量**：
- `NavigationCompleted`：对每个 `NavigationId` 必须且仅触发一次。

#### 4.3.1 `NavigateAsync` / `NavigateToStringAsync` 的 Task 语义（v1）

为避免“静默失败”，v1 约定：

- 这两个方法返回的 `Task` **必须等待**对应 `NavigationId` 的 `NavigationCompleted` 后才完成。
- `NavigationCompleted.Status` 与 Task 的完成方式：
  - `Success`：Task 正常完成
  - `Failure`：Task fault 为 `WebViewNavigationException`
  - `Canceled`：Task 正常完成（不抛）
  - `Superseded`：Task 正常完成（不抛）

### 4.4 Stop 语义

- `Stop()` 用于终止当前活动导航：
  - 若存在活动导航 `nav`：必须导致 `nav` 最终 Completed(Status=Canceled)
  - 若无活动导航：返回 `false`（或 `true` 但无副作用；v1 固定为 **false**）

### 4.5 Native-initiated navigation 拦截（Full-control, v1）

#### 4.5.1 适配器必须回调 Runtime 做决策

为拦截网页内部触发的任何主 frame 导航，v1 要求平台 adapter 在导航发生前回调 Runtime（adapter-host callback），由 Runtime 统一触发对外 `NavigationStarted` 并做 allow/deny 决策。

- adapter 在 native 引擎触发“即将导航”时调用：
  - `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- Runtime 在该回调中：
  - 生成（或复用）对应 `NavigationId`
  - 触发对外 `IWebView.NavigationStarted`
  - 根据 `Cancel` 返回 allow/deny
- adapter 必须遵守返回值：
  - deny：取消 native 导航
  - allow：继续 native 导航

#### 4.5.2 Redirect 的关联（CorrelationId）

重定向会导致“多次即将导航”回调。为保证 `NavigationId` 稳定与 `NavigationCompleted` exactly-once，adapter 必须为同一个逻辑导航链路提供稳定的 `CorrelationId`（redirect 复用同一 correlation）。

Runtime 使用 `CorrelationId` 将多次 native 回调关联到同一个 `NavigationId`，并允许对外多次触发 `NavigationStarted`（同一 `NavigationId`，不同 `RequestUri`）以支持精细控制每一次跳转。

---

## 5. `Source` 与 `NavigateToStringAsync` 语义

### 5.1 `Source` 属性

- `Source` 表示“最后一次请求的导航目标”：
  - `NavigateAsync(uri)` 或 `Source=uri`：`Source` 设置为该 `uri`
  - `NavigateToStringAsync(html)`：`Source` 设置为 `about:blank`

### 5.2 `Source=null`

- v1 固定：`Source` 不允许为 null；设置 null 抛 `ArgumentNullException`。

### 5.3 `NavigateToStringAsync(html)`

- `html` 不能为空；null 抛 `ArgumentNullException`
- 必须触发 `NavigationStarted/Completed`（与普通导航一致）
- `NavigationStarted.RequestUri` 固定为 `about:blank`

---

## 6. 脚本执行语义（`InvokeScriptAsync`）

- `script` 不能为空；null 抛 `ArgumentNullException`
- 结果类型：v1 固定为 `string?`
  - `null` 表示“脚本返回 null/undefined/无返回值”（由 adapter 做归一化）
  - 非 null 为脚本结果的字符串表示（具体序列化规则由 adapter 归一化，但必须稳定）
- 失败语义：
  - Task fault，异常类型为 `WebViewScriptException`（v1 约定名；实现可用派生自 `Exception` 的固定类型）
  - 异常必须包含可诊断信息（至少 message）

---

## 7. 历史能力标志（`CanGoBack/CanGoForward`）

- 初始必须为 `false/false`
- 属性更新必须是可观测的、与 adapter 状态一致
- v1 不强制“能力变化事件”，但禁止出现“返回 true 但 GoBack 实际无效且无可观测失败”的静默不一致

---

## 8. WebMessage Bridge（Baseline 安全语义）

### 8.1 默认关闭

- 未显式启用 bridge 时：
  - adapter 上报的任何 web message 都不得触发 `WebMessageReceived`

### 8.2 启用方式（v1 规范要求）

v1 约定：启用 bridge 必须是**显式 opt-in**，并通过“环境/选项”或“策略对象”注入到 Core（以下为规范要求，不限定具体 API 形态）：
- `EnableWebMessageBridge = true`
- `WebMessagePolicy` 必须存在（至少包含 origin allowlist、protocol/version、channel 绑定）

### 8.3 策略校验（必须通过才对外抛事件）

对每条 message：
- origin 不在 allowlist：丢弃，不触发事件
- protocol/version 不匹配：丢弃，不触发事件
- channelId 不匹配当前实例：丢弃，不触发事件

#### 8.3.1 丢弃可观测性（v1 最小诊断要求）

v1 要求：每次 message 被丢弃时，必须通过一个“可测试的诊断钩子”产生可观测信号。  
为保证 CT 可执行，v1 约定实现必须提供以下能力之一（至少一种）：

- **Diagnostics Sink（推荐）**：Core 在丢弃时调用一个可注入的诊断接收器（接口形态由实现决定），至少暴露：
  - 丢弃原因（OriginNotAllowed / ProtocolMismatch / ChannelMismatch）
  - 原始 origin
  - 当前实例标识（或 channelId）
- **Counter/Metric**：可在测试中读取到的计数器（按丢弃原因分桶）

> 仅写日志但测试无法可靠断言不满足 v1；必须“可测”。

---

## 9. Dialog 语义（Baseline）

- `Closing`：
  - `Close()` 调用后必须触发一次 `Closing`
  - 不得重复触发
  - `Closing` 与实际资源释放的顺序必须固定并可测（v1 约定：Closing 先触发，再进入关闭/释放流程）

> `Show/Show(owner)` 的平台行为差异较大，Baseline 语义放在 IT 冒烟，不纳入 CT 的强约束。

---

## 10. Auth 语义（`IWebAuthBroker` Baseline）

### 10.1 输入校验

- `CallbackUri` 必填；缺失则抛 `ArgumentException`（或返回固定失败结果；v1 固定为 **抛异常**）

### 10.2 回调匹配规则（Strict Match, v1）

当导航到 `callbackUri` 时视为认证完成（Success）的充分条件：
- `scheme`、`host`、`port`、`absolute path` 与期望 `CallbackUri` **完全一致**
- query/fragment 允许不同（作为返回内容的一部分保留）

### 10.3 会话隔离（默认 Ephemeral）

- v1 固定：默认必须使用 ephemeral/isolated session（不共享 cookies/storage）
- 若实现支持共享会话，只能作为 Extended opt-in

### 10.4 结果码（必须区分）

`WebAuthResult.Status` 至少包含：
- `Success`
- `UserCancel`
- `Timeout`
- `Error`（回调不匹配、导航失败、内部错误等）

并保证：
- UserCancel 与 Timeout 必须可区分（不得统一为 Error）

---

## 11. 异常类型约定（v1）

- `ArgumentNullException`：所有 null 参数校验
- `ArgumentException`：Auth 缺失 CallbackUri
- `InvalidOperationException`：非 UI 线程调用 Sync API
- `ObjectDisposedException`：Disposed 后调用
- `WebViewNavigationException`：导航失败（`NavigateAsync`/`NavigateToStringAsync` Task fault）
- `WebViewScriptException`：脚本执行失败（Task fault）

> 具体异常类命名可调整，但“可区分且稳定”的约束必须满足，CT 将按此验收。

---

## 12. 数据契约（事件参数 / 枚举 / 诊断）

> 目的：让 Abstractions 层的类型一开始就“字段齐全且可测”，避免后续频繁破坏性调整。

### 12.1 标识与类型约定

- `NavigationId`：`Guid`（单实例内唯一）
- `ChannelId`：`Guid`（WebMessage 通道，单实例唯一）
- `Origin`：`string`（例如 `https://example.com`；是否包含端口由实现按标准 URI 归一化）

### 12.2 枚举（v1）

- `NavigationCompletedStatus`
  - `Success`
  - `Failure`
  - `Canceled`
  - `Superseded`

- `WebAuthStatus`
  - `Success`
  - `UserCancel`
  - `Timeout`
  - `Error`

- `WebMessageDropReason`
  - `OriginNotAllowed`
  - `ProtocolMismatch`
  - `ChannelMismatch`

### 12.3 事件参数字段（v1 最小集合）

> 字段命名以英文为准；是否使用 record/class 由实现决定，但字段语义必须一致。

- `NavigationStartingEventArgs`
  - `Guid NavigationId`
  - `Uri RequestUri`
  - `bool Cancel`（可写；默认 false）

- `NavigationCompletedEventArgs`
  - `Guid NavigationId`
  - `Uri RequestUri`
  - `NavigationCompletedStatus Status`
  - `Exception? Error`（仅当 Status=Failure 时要求非 null；其余必须为 null）

- `WebMessageReceivedEventArgs`
  - `string Body`
  - `string Origin`
  - `Guid ChannelId`

### 12.4 WebMessage 丢弃诊断（v1）

若提供 Diagnostics Sink（推荐），其最小输入字段为：
- `WebMessageDropReason Reason`
- `string Origin`
- `Guid ChannelId`


