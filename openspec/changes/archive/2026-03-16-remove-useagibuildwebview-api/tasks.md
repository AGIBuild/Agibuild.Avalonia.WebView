## 1. Public API Canonicalization (Roadmap M9.2 API Surface Freeze)

- [x] 1.1 Remove `UseAgibuildWebView` extension methods from Avalonia and DI startup extension classes while preserving existing `UseFulora` initialization behavior.
- [x] 1.2 Run repository-wide replacement for first-party startup call sites so templates/samples/tests no longer call `UseAgibuildWebView`.

## 2. Template and Reference Surface Alignment (Goals E1/E2)

- [x] 2.1 Update template startup code and related reference comments/docs to use canonical `UseFulora` naming only.
- [x] 2.2 Verify no governed first-party asset still contains `UseAgibuildWebView` references (excluding archived historical records).

## 3. Regression Validation (Goal G4)

- [x] 3.1 Update/add unit/integration smoke tests to assert canonical `UseFulora` bootstrap still initializes `WebViewEnvironment` correctly.
- [x] 3.2 Execute targeted affected test projects and confirm pass; record final verification in change summary.
