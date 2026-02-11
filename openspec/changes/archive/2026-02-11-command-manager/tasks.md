# Command Manager — Tasks

## Core Contracts
- [x] Add `WebViewCommand` enum to `WebViewContracts.cs`
- [x] Update `ICommandManager` interface with `Copy/Cut/Paste/SelectAll/Undo/Redo` methods
- [x] Remove `[Experimental("AGWV002")]` attribute from `ICommandManager`

## Adapter Abstractions
- [x] Add `ICommandAdapter` facet interface to `IWebViewAdapter.cs`

## Runtime
- [x] Update `WebViewCore` to detect `ICommandAdapter` and create `ICommandManager` wrapper
- [x] Implement `CommandManager` internal class that delegates to `ICommandAdapter`
- [x] Update `WebViewCore.TryGetCommandManager()` to return instance when supported

## Platform Adapters
- [x] Windows: implement `ICommandAdapter` using `ExecuteScriptAsync("document.execCommand(...)")`
- [x] macOS: implement `ICommandAdapter` using `evaluateJavaScript`
- [x] iOS: implement `ICommandAdapter` using `evaluateJavaScript`
- [x] GTK: implement `ICommandAdapter` using `webkit_web_view_execute_editing_command()`
- [x] Android: implement `ICommandAdapter` using `evaluateJavascript`

## Consumer Surface
- [x] Update `IWebView.TryGetCommandManager()` documentation
- [x] Update `WebView` control to delegate `TryGetCommandManager()`
- [x] Update `WebDialog` / `AvaloniaWebDialog` to delegate `TryGetCommandManager()`

## Tests
- [x] Add `ICommandManager` contract tests (methods exist, correct signatures)
- [x] Add `ICommandAdapter` facet detection test
- [x] Add `WebViewCommand` enum test
- [x] Add `TryGetCommandManager()` returns non-null with ICommandAdapter adapter test
- [x] Add `TryGetCommandManager()` returns null without ICommandAdapter adapter test

## Build & Coverage
- [x] Verify all tests pass (target: 320+)
- [x] Verify coverage >= 90%
