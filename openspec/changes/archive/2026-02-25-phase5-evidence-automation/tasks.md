## 1. Snapshot Target Implementation

- [x] 1.1 Add `PhaseCloseoutSnapshot` target in build pipeline.
- [x] 1.2 Parse TRX and coverage artifacts to produce deterministic JSON payload.

## 2. Pipeline Wiring

- [x] 2.1 Add snapshot target into `Ci` dependency chain.
- [x] 2.2 Add snapshot target into `CiPublish` dependency chain.

## 3. Validation

- [x] 3.1 Run strict OpenSpec validation.
- [x] 3.2 Execute `nuke Coverage` (or `nuke Ci`) and verify snapshot output exists and is populated.
