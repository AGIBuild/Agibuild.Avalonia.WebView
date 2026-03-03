# Interactive Playground — Design

## Context

New developers evaluating Fulora need a quick way to experiment with bridge interface definitions without creating a full project. A web-based playground hosted on the docs site allows users to write C# interface definitions, see generated TypeScript live, and test simulated bridge calls. This reduces "time to first experience" and accelerates adoption. Goal: E1 (template DX).

**Existing contracts**: Bridge source generator (emits TypeScript from `[JsExport]`/`[JsImport]` interfaces), `@agibuild/bridge` client API, JSON-RPC protocol semantics.

## Goals / Non-Goals

### Goals

- Web-based playground at docs.agibuild.dev/playground
- Monaco editor for C# bridge interface definition (syntax highlighting, basic validation)
- Live preview of generated TypeScript types as the user types
- Simulated bridge call testing with mock responses
- Share playground state via URL (base64 or compressed encoding)
- No backend required for core flow (client-side only where possible)

### Non-Goals

- Full C# compilation in browser
- Connecting to running Fulora apps
- Mobile playground (web-only for v1)
- Real bridge RPC to host

## Decisions

### D1: Monaco editor for C# editing

**Decision**: Use Monaco Editor (same engine as VS Code) for the C# interface editor. Provide C# syntax highlighting and basic bracket matching. No full C# language service (no IntelliSense, no real compilation).

**Rationale**: Monaco is industry-standard for web-based code editors. C# syntax highlighting is available via Monarch grammar. Full C# compilation would require Blazor/WASM or a backend; out of scope for v1.

### D2: TypeScript generation — server-side or WASM

**Decision**: Evaluate two options: (A) Server-side API that accepts C# text and returns generated TypeScript; (B) WASM-based C# parser + TypeScript emitter running in browser. For v1, prefer server-side API for correctness and maintainability. The playground calls a lightweight API (e.g., POST /api/playground/generate) that runs the bridge source generator logic or a simplified parser/emitter.

**Rationale**: The bridge source generator is Roslyn-based; running it in browser would require Blazor or a custom WASM port. A server-side endpoint reuses existing generator logic or a shared TypeScript emitter. Simpler to implement and more accurate. Trade-off: requires backend; can be a serverless function (e.g., Azure Functions, Vercel) to minimize ops.

### D3: Simplified C# parser for client-side fallback

**Decision**: If server-side is unavailable (e.g., offline docs), provide a client-side fallback: a lightweight regex/state-machine parser that extracts interface names, method names, and parameter types from C# text. Emit a best-effort TypeScript declaration. Clearly label as "approximate" and recommend server for accurate output.

**Rationale**: Graceful degradation. Users can still experiment offline. The fallback need not handle all edge cases (generics, overloads); it covers the common `Task<T> Method(params)` pattern.

### D4: Mock bridge runtime

**Decision**: Implement a mock bridge runtime in the playground that intercepts `invoke()` calls and returns configurable mock responses. Users can define mock responses per method (e.g., `AppService.GetData` → `{ id: 1, name: "Test" }`). The mock runtime is a simple JS object keyed by `serviceName.methodName`.

**Rationale**: Enables "try it" without a running app. Users see the typed API in action. Mock responses are editable in a side panel or JSON editor. Default mocks for common types (string, number, object) reduce setup.

### D5: URL state encoding

**Decision**: Encode playground state (C# source, mock responses, selected tab) into the URL. Use base64url encoding of a JSON payload, or a compressed format (e.g., pako/lz-string) if payload exceeds URL length limits (~2000 chars). Hash fragment (`#...`) preferred over query to avoid server round-trips.

**Rationale**: Shareable links enable "try this" demos and support requests. Hash fragment keeps state client-side. Compression allows longer snippets. Document max length and truncation behavior.

### D6: Integration with docs site

**Decision**: The playground SHALL be embedded in the docs site (docs.agibuild.dev) as a route `/playground`. It MAY be a separate React app built and deployed alongside the docs, or a sub-route within the docs SPA. Deployment pipeline includes playground build.

**Rationale**: Single docs domain for discoverability. Docs site already has build/deploy; add playground as a build artifact. Same CDN, same domain for CORS simplicity if server-side generation is used.

## Risks / Trade-offs

### R1: Server-side generation adds backend dependency

**Risk**: Playground depends on a backend for accurate TypeScript. Backend downtime or rate limits affect playground usability.

**Mitigation**: Client-side fallback for basic cases. Cache generated output. Consider edge/serverless for low latency and high availability.

### R2: C# parsing accuracy

**Risk**: Simplified parser may misparse complex interfaces (nested types, attributes, comments).

**Mitigation**: Document supported subset. Server-side path uses real generator when available. Fallback clearly labeled as approximate.

### R3: Monaco bundle size

**Risk**: Monaco adds ~2–3 MB to the playground bundle.

**Mitigation**: Lazy-load Monaco. Use CDN. Consider lighter alternatives (CodeMirror 6) if bundle size is critical; Monaco preferred for familiarity and C# support.
