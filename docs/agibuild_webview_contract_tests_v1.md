# Agibuild.Avalonia.WebView Contract Tests 清单 v1（Baseline 驱动）

> 目标：把 `docs/agibuild_webview_compatibility_matrix_proposal.md` 的 **Baseline** 条目，转成可执行、平台无关的 **CT（Contract Tests）** 最小用例集合。  
> 原则：CT 只验证“契约语义/状态机/事件时序/线程语义”，不验证平台渲染与真实浏览器能力（那是 IT 范畴）。

---

## 1. 测试前提（Harness 约束）

### 1.1 统一测试运行模型

- **CT 必须平台无关**：仅依赖 `Core + Abstractions + MockAdapter`。
- **事件线程不变量**：所有对外事件必须在“测试 UI 线程”触发。
- **时间相关**：CT 禁止用不稳定的 `Thread.Sleep`；需要可控调度器/虚拟时间（若无法提供，至少要以 `TaskCompletionSource` 驱动）。

### 1.2 需要的测试替身（不规定实现，只规定能力）

- `TestDispatcher`：可标记“当前线程为 UI 线程”，并提供 `Post/InvokeAsync` 能力以验证 marshal 行为。
- `MockWebViewAdapter`：可接收 Core 发起的指令（Navigate/Stop/InvokeScript…），并可由测试主动“回放 native 事件”到 Core。
- `EventRecorder`：订阅所有事件，记录（时间、线程、NavigationId、参数）以便断言顺序与 exactly-once。

补充（Full-control baseline）：
- `IWebViewAdapterHost`：adapter 在网页内部触发“即将导航”（含重定向）时，必须回调 host 获取 allow/deny 决策与 `NavigationId`。

---

## 2. 全局契约不变量（所有 Baseline CT 必须覆盖）

- **INV-THREAD-001**：所有事件在 UI 线程触发。
- **INV-NAV-001**：每个 `NavigationId` 必须且仅一次触发 `NavigationCompleted`（Exactly-once）。
- **INV-NAV-002**：若 `NavigationStarted.Cancel=true`，则不得触发 native 导航动作，并必须触发 `NavigationCompleted(Status=Canceled)`。
- **INV-NAV-003**：Latest-wins：新的导航发起后，旧导航必须 `Completed(Status=Superseded)`（v1 固定为 Superseded）。
- **INV-NAV-004**：Native-initiated navigation 可控：网页内部触发的导航（点击/脚本/重定向）必须进入同一套 `NavigationStarted.Cancel` 决策点，允许 deny 并以 `Canceled` 完结。
- **INV-NAV-005**：Redirect correlation：同一逻辑导航链路（redirect chain）必须复用同一 `CorrelationId`，并映射为同一 `NavigationId`，最终 `NavigationCompleted` exactly-once。
- **INV-DISPOSE-001**：Disposed 后 API 行为一致且可预测（见生命周期用例）。
- **INV-OBS-001**：对外“静默失败”禁止：失败必须通过异常/返回值/Completed 状态可观测（Baseline 至少覆盖导航与脚本）。

---

## 3. 用例清单（按能力分组）

每条用例命名采用 `Category_Scenario_Expected`（English），描述与验收点用中文。

### 3.1 Lifecycle / Disposal

- **(TODO) Lifecycle_Create_DefaultState**
  - **Given**：创建 WebView Core（绑定 MockAdapter + TestDispatcher）
  - **Then**：初始状态满足契约（例如 `CanGoBack/CanGoForward=false`，未触发任何事件）

- **Disposed_sync_apis_throw_ObjectDisposedException**
  - **When**：Dispose 后调用 `GoBack/GoForward/Refresh/Stop`
  - **Then**：抛 `ObjectDisposedException`

- **Disposed_async_apis_fault_with_ObjectDisposedException**
  - **When**：Dispose 后调用 `NavigateAsync/InvokeScriptAsync/NavigateToStringAsync`
  - **Then**：任务必须快速 fault 为 `ObjectDisposedException`（不得悬挂）

- **No_events_are_raised_after_dispose**
  - **When**：Dispose 后 adapter 回放任何事件
  - **Then**：对外事件不再触发

### 3.2 Threading / Marshal（契约线程语义）

- **Async_navigation_marshals_to_ui_thread_and_events_are_on_ui_thread**
  - **When**：非 UI 线程调用 `NavigateAsync`（或 `InvokeScriptAsync`）
  - **Then**：adapter 收到指令时必须处于 UI 线程上下文；事件也在 UI 线程

- **Sync_apis_require_ui_thread**
  - **When**：非 UI 线程调用 `GoBack/Refresh/Stop`
  - **Then**：抛 `InvalidOperationException`

- **All_public_events_are_raised_on_ui_thread**
  - **When**：adapter 在非 UI 线程回放“native 事件”
  - **Then**：对外事件仍必须在 UI 线程触发（Core marshal）

### 3.3 Source（`Source` get/set）

- **Source_set_updates_last_requested_uri_and_starts_navigation**
  - **When**：设置 `Source = uri`
  - **Then**：`Source` 返回该 uri（不要求已加载）；并触发一次导航流程（见导航用例的事件断言）

- **Source_set_null_throws_ArgumentNullException**
  - **When**：`Source = null`
  - **Then**：抛 `ArgumentNullException`（若决定允许 null，则此用例改为“clears source without navigation”，但必须写死）

### 3.4 Navigation（`NavigateAsync`, `NavigateToStringAsync`）

- **NavigateAsync_null_throws_ArgumentNullException**
  - **When**：`NavigateAsync(null)`
  - **Then**：抛 `ArgumentNullException`

- **Async_navigation_marshals_to_ui_thread_and_events_are_on_ui_thread**
  - **Given**：adapter 配置为“成功完成”
  - **When**：`NavigateAsync(uri)`
  - **Then**：事件顺序：`NavigationStarted` -> `NavigationCompleted(Success)`；Completed exactly-once；`RequestUri` 一致

- **Cancel_in_NavigationStarted_prevents_adapter_navigation_and_completes_as_canceled**
  - **Given**：订阅 `NavigationStarted` 并设置 `e.Cancel=true`
  - **When**：`NavigateAsync(uri)`
  - **Then**：adapter 不得收到 Navigate 指令；必须触发 `NavigationCompleted(Canceled)`；`NavigateAsync` **正常完成**（不抛），且 Completed 状态可观测

- **Navigation_failure_faults_with_WebViewNavigationException**
  - **Given**：adapter 回放失败（带错误信息/异常）
  - **Then**：必须触发 `NavigationCompleted(Failure)` 且错误可观测；`NavigateAsync` Task 必须 fault 为 `WebViewNavigationException`

- **Stop_cancels_active_navigation_and_completes_as_canceled**
  - **Given**：导航开始但未完成
  - **When**：调用 `Stop()`
  - **Then**：必须最终产生 `NavigationCompleted(Status=Canceled)` exactly-once；被取消的 `NavigateAsync` Task 正常完成（不抛）

- **Stop_returns_false_when_idle**
  - **Given**：当前无活动导航
  - **When**：调用 `Stop()`
  - **Then**：返回 `false`

- **Latest_wins_supersedes_active_navigation**
  - **Given**：发起 `NavigateAsync(uri1)` 尚未完成
  - **When**：立刻发起 `NavigateAsync(uri2)`
  - **Then**：对 `nav1` 必须 `Completed(Superseded)` exactly-once，且 `NavigateAsync(uri1)` Task 正常完成（不抛）；对 `nav2` 正常 Started/Completed；事件不交叉错乱（以 NavigationId 关联）

### 3.4.1 Native-initiated navigation（网页内部跳转/重定向）

- **Native_initiated_navigation_can_be_canceled**
  - **Given**：adapter 通过 host callback 回放一次“即将导航”，订阅 `NavigationStarted` 并设置 `Cancel=true`
  - **Then**：host callback 返回 deny；必须触发 `NavigationCompleted(Canceled)`；不得出现悬挂

- **Redirect_steps_reuse_NavigationId_for_same_CorrelationId**
  - **Given**：adapter 对同一 redirect chain 复用 `CorrelationId`，连续回放两次“即将导航”
  - **Then**：两次对外 `NavigationStarted` 的 `NavigationId` 必须相同；最终 `NavigationCompleted` 对该 `NavigationId` exactly-once

- **NavigateToStringAsync_null_throws_ArgumentNullException**
  - **When**：`NavigateToStringAsync(null)`
  - **Then**：抛 `ArgumentNullException`

- **NavigateToString_sets_Source_to_about_blank_and_Started_uses_about_blank**
  - **When**：`NavigateToStringAsync("<html>...</html>")`
  - **Then**：Started/Completed 顺序与 exactly-once；`NavigationStarted.RequestUri=about:blank`；`Source=about:blank`

### 3.5 Script（`InvokeScriptAsync`）

- **InvokeScriptAsync_null_throws_ArgumentNullException**
  - **When**：`InvokeScriptAsync(null)`
  - **Then**：抛 `ArgumentNullException`

- **Script_InvokeScriptAsync_ReturnsAdapterResult_StringOrNull**
  - **Given**：adapter 预设返回值（例如 `"42"` 或 `null`）
  - **Then**：返回值类型固定为 `string?`，与契约一致

- **Script_failure_faults_with_WebViewScriptException**
  - **Given**：adapter 回放脚本执行失败
  - **Then**：`InvokeScriptAsync` 任务 fault 为 `WebViewScriptException`（或契约约定的固定异常类型）且错误可观测

### 3.6 History flags（`CanGoBack/CanGoForward`）

- **History_DefaultFlags_False**
  - **Then**：初始 `CanGoBack=false`、`CanGoForward=false`

- **History_AfterNavigate_FlagsUpdateViaAdapterState**
  - **Given**：adapter 回放“可后退/前进”状态变更
  - **Then**：Core 对外属性更新可观测（若需要事件通知，则此处补用例；否则仅断言属性）

### 3.7 WebMessage Bridge（`WebMessageReceived` baseline）

- **WebMessage_bridge_is_disabled_by_default**
  - **Given**：未显式启用 bridge
  - **When**：adapter 回放 web message
  - **Then**：`WebMessageReceived` 不触发

- **WebMessage_EnableBridge_AllowsPolicyPassMessage**
  - **Given**：启用 bridge + policy 允许（origin allowlist 命中、protocol version 正确、channel 匹配）
  - **When**：adapter 回放 web message
  - **Then**：触发 `WebMessageReceived`，且事件在 UI 线程

- **WebMessage_drops_are_observable_with_drop_reason**
  - **Given**：policy 拒绝 origin
  - **Then**：不触发 `WebMessageReceived`；同时必须通过“诊断钩子/计数器”可观测（见语义规范 v1 的诊断要求）

- **WebMessage_protocol_mismatch_is_dropped_and_observable**
  - **Given**：protocol/version 不匹配
  - **Then**：不触发 `WebMessageReceived`

- **WebMessage_channel_mismatch_is_dropped_and_observable**
  - **Given**：message channelId 不匹配当前实例
  - **Then**：不触发 `WebMessageReceived`

### 3.8 Dialog（Baseline：仅定义最小契约，CT 覆盖语义部分）

> 注：`IWebDialog` 的 Show/Close 更偏平台 IT；CT 仅覆盖“纯语义/事件”部分（例如 Closing exactly-once 与顺序）。

- **Dialog_Closing_FiresOnce_WhenCloseCalled**
  - **When**：调用 `Close()`
  - **Then**：`Closing` 必须触发一次；不得重复

- **Dialog_Closing_Order_BeforeDisposedOrAfterClose_Contracted**
  - **Then**：Closing 与 Dispose/Close 的顺序必须写死并断言（例如 Closing 先于实际关闭完成）

### 3.9 Auth（`IWebAuthBroker` baseline）

- **Auth_CallbackUriRequired_ThrowsArgumentException**
  - **When**：`AuthenticateAsync` options 缺少 CallbackUri
  - **Then**：抛 `ArgumentException`

- **Auth_UserCancel_ReturnsCanceledResult**
  - **Given**：模拟用户关闭/取消
  - **Then**：返回 `WebAuthResult(Status=UserCancel)`（结果码必须区分）

- **Auth_Timeout_ReturnsTimeoutResult**
  - **Given**：模拟超时
  - **Then**：返回 `WebAuthResult(Status=Timeout)`

- **Auth_StrictCallbackMatch_IgnoresQueryAndFragment**
  - **Given**：回调 URI 严格匹配规则命中
  - **Then**：返回 Success 且携带最终回调 URI

- **Auth_EphemeralDefault_IsRequestedByCore**
  - **Then**：默认必须请求 ephemeral/isolated session（通过对 MockDialog/MockAdapter 的“创建参数/环境请求”断言）

---

## 4. 覆盖映射（Baseline 条目 -> CT）

| Baseline item (matrix) | CT coverage (this doc) |
|---|---|
| `Source` get/set | 3.3 |
| `NavigateAsync(Uri)` | 3.4 |
| `NavigateToStringAsync(string)` | 3.4 |
| `InvokeScriptAsync(string)` | 3.5 |
| `CanGoBack/CanGoForward` | 3.6 |
| `GoBack/GoForward/Refresh/Stop` | 3.2 + 3.4 + 3.6 |
| `NavigationStarted/Completed` | 3.4 + INV-NAV-* |
| `WebMessageReceived` | 3.7 |
| `IWebDialog` Baseline（Closing 语义） | 3.8 |
| `IWebAuthBroker` Baseline | 3.9 |

---

## 5. 不在 v1 覆盖的内容（刻意留给 Extended/IT）

- `WebResourceRequested`（Extended，且多平台能力差异大，优先 IT + 差异条目）
- `NewWindowRequested`（Extended，平台行为差异大，优先 IT）
- `CookieManager/CommandManager/NativeHandleProvider`（Extended/IT 为主）
- DevTools/UA/持久化 profile/private mode（Extended/IT 为主）

