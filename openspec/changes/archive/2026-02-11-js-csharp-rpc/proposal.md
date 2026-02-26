# JS ↔ C# RPC

## Problem
The current WebMessage bridge only supports raw string message passing. Consumers must manually serialize/deserialize, match request/response, and handle errors. This is the key gap vs bundled-browser stacks' `ipcMain`/`ipcRenderer`.

## Solution
Build a typed RPC layer on top of the existing WebMessage bridge. C# side registers named handlers; JS side calls them with `window.agWebView.rpc.invoke(method, ...args)` and gets a Promise back. Supports both directions: JS→C# calls and C#→JS calls.

## Scope
- Define `IWebViewRpcService` for registering C# handlers
- Define JS-side `window.agWebView.rpc.invoke(method, args)` → Promise API
- RPC protocol on top of existing WebMessage (JSON envelope with id, method, args, result, error)
- Bidirectional: JS→C# and C#→JS
- Error propagation (C# exceptions → JS rejections, JS errors → C# exceptions)
- Add to `WebView` control and `WebDialog`
- Contract tests for the protocol layer
