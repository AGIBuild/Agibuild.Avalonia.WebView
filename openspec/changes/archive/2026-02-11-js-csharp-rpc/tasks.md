# JS ↔ C# RPC — Tasks

## Core Contracts
- [x] Add `IWebViewRpcService` interface to `WebViewContracts.cs`
- [x] Add `Rpc` property to `IWebView` interface
- [x] Add `WebViewRpcException` for RPC-specific errors

## Runtime — C# RPC Engine
- [x] Implement `WebViewRpcService` class (handler registry + pending call tracking)
- [x] Implement JS→C# request dispatch (parse JSON-RPC, find handler, call, return result)
- [x] Implement C#→JS call via `InvokeScriptAsync` (send request, await response)
- [x] Implement error propagation (C# exception → JSON-RPC error → JS rejection)
- [x] Wire `WebViewRpcService` into `WebViewCore` (create when bridge is enabled)
- [x] Handle `WebMessageReceived` events with `__rpc` envelope type

## Runtime — JS Stub
- [x] Create JS RPC runtime stub (rpc.invoke, rpc.handle, message routing)
- [x] Auto-inject JS stub when WebMessage bridge is enabled
- [x] Support C#→JS handler registration and dispatch

## Consumer Surface
- [x] Add `Rpc` property to `WebView` control
- [x] Add `Rpc` property to `WebDialog` / `AvaloniaWebDialog`

## Tests — Protocol
- [x] Test JSON-RPC request serialization
- [x] Test JSON-RPC response deserialization
- [x] Test error envelope serialization

## Tests — C# Side
- [x] Test handler registration and removal
- [x] Test JS→C# call dispatch with mock adapter
- [x] Test sync and async handler variants
- [x] Test unknown method returns -32601 error
- [x] Test handler exception propagates as error

## Tests — Integration
- [x] Test Rpc property is null before bridge enabled
- [x] Test Rpc property is non-null after bridge enabled
- [x] Test round-trip JS→C#→JS via mock

## Build & Coverage
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
