## 1. Bridge binary payload ergonomics
- [x] 1.1 Update TypeScript byte-array mapping to `Uint8Array` and adjust generator tests.
- [x] 1.2 Update generated JS bridge stub emission to encode `Uint8Array` params and decode binary returns.
- [x] 1.3 Add runtime-level JS stub helpers for deterministic base64 conversion and keep JSON-RPC envelope unchanged.

## 2. Shell deep-link + single-instance activation orchestration
- [x] 2.1 Add shell activation coordinator runtime service for primary/secondary registration and forwarding.
- [x] 2.2 Add deterministic deep-link activation payload model and validation flow.
- [x] 2.3 Add unit tests for ownership lifecycle, forwarding, and failure paths.

## 3. SPA asset hot update with signature + rollback
- [x] 3.1 Add signed package installer service with digest/signature validation and versioned staging.
- [x] 3.2 Add atomic active-version switch and rollback support.
- [x] 3.3 Integrate optional active external asset path with SPA hosting resolution and add unit tests.

## 4. Verification
- [x] 4.1 Run targeted unit tests for bridge/shell/spa tracks.
- [x] 4.2 Run OpenSpec strict validation.
