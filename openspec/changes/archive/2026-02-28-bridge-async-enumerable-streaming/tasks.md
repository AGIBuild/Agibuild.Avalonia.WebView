## 1. Remove AGBR005 and Detect IAsyncEnumerable

- [x] 1.1 Remove IAsyncEnumerable validation from `ModelExtractor.ValidateInterface`
- [x] 1.2 Add `IsAsyncEnumerable` and `AsyncEnumerableInnerType` to `BridgeMethodModel`
- [x] 1.3 Update `ModelExtractor.ExtractMethods` to detect IAsyncEnumerable return type
- [x] 1.4 Update diagnostic test: IAsyncEnumerable no longer reports AGBR005

## 2. RPC Enumerator Protocol

- [x] 2.1 Add active enumerator tracking (`ConcurrentDictionary<string, ActiveEnumerator>`) to `WebViewRpcService`
- [x] 2.2 Add `RegisterEnumerator` and `DisposeEnumerator` internal methods
- [x] 2.3 Handle `$/enumerator/next` as a regular request in `TryProcessMessage`
- [x] 2.4 Handle `$/enumerator/abort` as a notification in `TryProcessMessage`
- [x] 2.5 Add CT: `$/enumerator/next` returns items
- [x] 2.6 Add CT: `$/enumerator/next` returns finished=true when done
- [x] 2.7 Add CT: `$/enumerator/abort` disposes enumerator

## 3. Source Generator — Streaming Handler

- [x] 3.1 Update `BridgeHostEmitter` to generate streaming handlers for IAsyncEnumerable methods
- [x] 3.2 Generated handler: call impl, get enumerator, prefetch first item, return token
- [x] 3.3 Register enumerator with RPC service for pull-based consumption
- [x] 3.4 Add CT: generated handler creates enumerator and returns token

## 4. JS AsyncIterator and TypeScript

- [x] 4.1 Add JS `createAsyncIterable` wrapper to RPC stub that implements `Symbol.asyncIterator`
- [x] 4.2 Update generated service stubs for streaming methods to return AsyncIterable
- [x] 4.3 Update `TypeScriptEmitter` to map IAsyncEnumerable<T> to AsyncIterable<T>
- [x] 4.4 Add CT: TypeScript declaration includes AsyncIterable return type

## 5. Validation

- [x] 5.1 Run full test suite and verify all pass
