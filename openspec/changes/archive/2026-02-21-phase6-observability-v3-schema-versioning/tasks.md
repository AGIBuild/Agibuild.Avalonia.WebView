## 1. Runtime Diagnostic Contract Upgrade

- [x] 1.1 Add `DiagnosticSchemaVersion` + `CurrentDiagnosticSchemaVersion` to host capability diagnostic event args and wire deterministic emission.
- [x] 1.2 Add `DiagnosticSchemaVersion` + `CurrentDiagnosticSchemaVersion` to session profile diagnostic event args and wire deterministic emission.

## 2. Cross-Lane Assertion Unification

- [x] 2.1 Add shared diagnostic schema assertion helper in `Agibuild.Fulora.Testing`.
- [x] 2.2 Migrate diagnostics-focused CT/IT tests to use the shared helper and assert schema-version stability.
- [x] 2.3 Update governance tests to validate schema contract continuity with the shared expectation source.

## 3. Verification and Closeout

- [x] 3.1 Run targeted unit and integration automation tests covering host capability and session profile diagnostics.
- [x] 3.2 Run `openspec validate --all --strict` and confirm zero failures.
- [x] 3.3 Mark tasks complete and prepare change for sync/archive.
