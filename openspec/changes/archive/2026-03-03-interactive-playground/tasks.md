# Interactive Playground — Tasks

## 1. Scaffold

- [x] 1.1 Create `tools/playground/` directory with React app (Vite + TypeScript)
- [x] 1.2 Add dependencies: Monaco Editor (`@monaco-editor/react`), React, React DOM
- [x] 1.3 Configure build output for docs site integration (e.g., static export to `docs/playground/` or docs build step)
- [x] 1.4 Add routing so playground is served at `/playground` when docs site is deployed
- [x] 1.5 Create basic layout: editor pane (left), preview pane (right), optional mock config panel

## 2. Editor

- [x] 2.1 Integrate Monaco Editor with C# language configuration (syntax highlighting via Monarch grammar)
- [x] 2.2 Add default C# interface example (minimal `[JsExport]` interface)
- [x] 2.3 Implement debounced change handler (e.g., 500 ms) to trigger generation on edit
- [x] 2.4 Add editor state persistence in React state; wire to URL encoding
- [x] 2.5 (Optional) Add basic validation feedback (e.g., bracket matching, brace balance)

## 3. TypeScript Generation

- [x] 3.1 Implement or integrate server-side generation API: accepts C# source, returns TypeScript (reuse bridge generator logic or simplified emitter)
- [x] 3.2 Add playground client call to generation API (POST with source, parse response)
- [x] 3.3 Implement client-side fallback parser: extract interface names, methods, params from C# text (regex/state-machine)
- [x] 3.4 Implement client-side TypeScript emitter for fallback (emit typed service proxy and method signatures)
- [x] 3.5 Add "approximate" label when fallback is used; handle API errors gracefully
- [x] 3.6 Display generated TypeScript in preview pane with syntax highlighting (Monaco or CodeMirror)

## 4. Mock Bridge

- [x] 4.1 Create mock bridge runtime: `invoke(service, method, params)` returns configured mock or default
- [x] 4.2 Define mock config structure: `Record<"serviceName.methodName", unknown>` for mock responses
- [x] 4.3 Add UI for editing mock responses (JSON editor or key-value form)
- [x] 4.4 Implement default mocks for `Task<string>`, `Task<int>`, `Task<bool>`, `Task<T>` (return `{}` for object)
- [x] 4.5 Wire generated TypeScript service to mock runtime so "Run" invokes mock
- [x] 4.6 Add "Run" or "Test" button to invoke selected method and display result/error

## 5. URL State

- [x] 5.1 Implement state serialization: C# source + mock config + selected tab → JSON
- [x] 5.2 Implement URL encoding: JSON → base64url (or compressed if needed) → hash fragment
- [x] 5.3 Implement URL decoding on load: parse hash, decode, restore state
- [x] 5.4 Add "Share" button that copies URL with current state to clipboard
- [x] 5.5 Update URL on state change (debounced) for shareable links without explicit Share click
- [x] 5.6 Document max URL length and truncation/compression behavior

## 6. Deployment

- [x] 6.1 Integrate playground build into docs site build pipeline (e.g., `npm run build:playground` → output to docs static assets)
- [x] 6.2 Configure docs site routing to serve playground at `/playground`
- [x] 6.3 Deploy generation API if server-side path is used (e.g., serverless function, Azure Functions, Vercel)
- [x] 6.4 Add playground link to docs navigation or landing page
- [x] 6.5 Verify playground works in production (CORS, API URL config)

## 7. Tests

- [x] 7.1 Unit tests: Client-side parser extracts interface and method names correctly from sample C# snippets
- [x] 7.2 Unit tests: URL encode/decode round-trip preserves state
- [x] 7.3 Unit tests: Mock runtime returns configured mocks and defaults
- [x] 7.4 E2E or manual test: Full flow — edit C#, see TS, configure mock, run, share URL, restore
