## 1. Artifact and Delta Preparation

- [x] 1.1 Draft proposal aligned to Phase 3 deliverable 3.8 and Goal G4 (Deliverable: clear scope and non-goals for spec-sync closure).
- [x] 1.2 Draft design with explicit decision rationale and sync-closure command path (Deliverable: implementation-ready governance design).
- [x] 1.3 Create delta spec for `webview-contract-semantics-v1` to make requirement body normative with SHALL/MUST (Deliverable: validator-compatible requirement text).

## 2. Validation and Archive/Sync Closure

- [x] 2.1 Run `openspec validate sync-spec-deltas-after-archive` and confirm change validity before archive (Deliverable: pre-archive validation evidence).
- [x] 2.2 Archive with sync enabled (`openspec archive sync-spec-deltas-after-archive -y`) and verify no `--skip-specs` fallback is needed (Deliverable: successful synced archive).
- [x] 2.3 Verify post-archive state (`openspec list --json`, `openspec validate --type spec webview-contract-semantics-v1`) to confirm closure (Deliverable: sync-closure evidence).
