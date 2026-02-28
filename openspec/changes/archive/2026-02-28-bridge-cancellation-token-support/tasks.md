## 1. Remove AGBR004 Diagnostic

- [x] 1.1 Remove CancellationToken validation from `ModelExtractor.ValidateInterface`
- [x] 1.2 Add `IsCancellationToken` flag to `BridgeParameterModel`
- [x] 1.3 Update `ModelExtractor.ExtractParameters` to detect and mark CancellationToken params
- [x] 1.4 Update diagnostic test: CancellationToken no longer reports AGBR004

## 2. RPC Cancellation Infrastructure

- [x] 2.1 Add `ConcurrentDictionary<string, CancellationTokenSource> _activeCancellations` to `WebViewRpcService`
- [x] 2.2 Add `RegisterCancellation(string requestId, CancellationTokenSource cts)` and `UnregisterCancellation(string requestId)` internal methods
- [x] 2.3 Extend `TryProcessMessage` to handle `$/cancelRequest` notifications (messages without `id` that have method `$/cancelRequest`)
- [x] 2.4 Catch `OperationCanceledException` in `DispatchRequestAsync` and return error code -32800
- [x] 2.5 Add CT: `$/cancelRequest` cancels active handler
- [x] 2.6 Add CT: `$/cancelRequest` for unknown ID is silently ignored
- [x] 2.7 Add CT: OperationCanceledException returns -32800

## 3. Source Generator — BridgeHostEmitter

- [x] 3.1 Update `BridgeHostEmitter` to detect CancellationToken parameters and generate CTS-aware handler code
- [x] 3.2 Exclude CancellationToken from params deserialization in generated handler
- [x] 3.3 Register CTS with RPC service at handler start, unregister at completion
- [x] 3.4 Add CT: generated handler creates CTS and passes token to method

## 4. JS Stub and TypeScript

- [x] 4.1 Update generated JS stubs to accept optional `options` parameter with `signal` for cancellable methods
- [x] 4.2 Add abort signal listener that sends `$/cancelRequest` when fired
- [x] 4.3 Update `TypeScriptEmitter` to map CancellationToken to `options?: { signal?: AbortSignal }`
- [x] 4.4 Add CT: TypeScript declaration includes AbortSignal option

## 5. Validation

- [x] 5.1 Run full test suite and verify all pass
- [x] 5.2 Run coverage check
