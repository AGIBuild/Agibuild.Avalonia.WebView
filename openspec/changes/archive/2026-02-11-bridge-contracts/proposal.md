# bridge-contracts

**Goal**: G1 (Type-Safe Bidirectional Bridge)
**ROADMAP**: Phase 1, Deliverable 1.1a
**Advances current phase**: Establishes the contract surface (`[JsExport]`, `[JsImport]`, `IBridgeService`) that all subsequent Phase 1 deliverables build upon. No Roslyn Source Generator needed — pure C# types + manual registration runtime.

## Problem

The existing `IWebViewRpcService` (F6) provides JSON-RPC 2.0 over WebMessage, but it is untyped — developers register handlers with string method names and manually parse `JsonElement` params. This is error-prone and cannot be validated at compile time.

Phase 1 introduces type-safe bridge communication. This first change establishes the **contract layer** — the attributes, interfaces, and runtime service that define how typed services are exposed to and imported from JavaScript.

## Proposed Change

1. **Core**: Add `[JsExport]` and `[JsImport]` attributes, `IBridgeService` interface, `BridgeOptions` type
2. **Runtime**: Implement `RuntimeBridgeService` that manually registers/routes RPC handlers (pre-Source-Generator)
3. **Runtime**: Add `IBridgeService? Bridge` property to `WebViewCore`; auto-enable bridge on first `Expose<T>()`
4. **WebView control**: Expose `Bridge` property delegating to core
5. **Tests**: Full CT coverage for registration, routing, error handling, lifecycle, security

## Non-goals

- Source Generator (that's deliverable 1.1b)
- TypeScript `.d.ts` generation (deliverable 1.3)
- MockBridge for consumer testing (deliverable 1.5)
- Rate limiting in BridgeOptions (deliverable 1.4)

## Spec

- [bridge-contracts](../../specs/bridge-contracts/spec.md)
