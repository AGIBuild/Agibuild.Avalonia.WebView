## Archive-Ready Checklist

- [x] `ShowAbout` typed action integrated into explicit whitelist contract (deny-by-default unless allowlisted).
- [x] Inbound tray event metadata bounded envelope implemented and validated before dispatch.
- [x] Federated menu pruning order implemented (`profile -> policy -> mutation`) with deterministic deny semantics.
- [x] Pruning profile/policy failure isolation verified against permission/download/new-window domains.
- [x] Template app-shell preset updated with explicit whitelist marker, profile resolver marker, and federated pruning result surface.
- [x] Template web demo updated to consume bounded metadata keys only (no raw payload passthrough path).
- [x] CT matrix rows updated for ShowAbout whitelist, metadata boundary, and federated pruning failure branch.
- [x] Unit/integration/template evidence commands executed and passing (`verification-evidence.md`).
- [x] Scope boundary notes captured (`roadmap-notes.md`).

## Open Questions / Owners / Decision Gate

1. **是否为 metadata envelope 增加全局 payload size budget（跨平台统一阈值）**  
   - Owner: Runtime Maintainer  
   - Decision Gate: 下一次 `shell-policy-profile-federated-system-integration` follow-up 评审

2. **是否在联邦 pruning 诊断中引入 profile version/hash 字段以支持审计追踪**  
   - Owner: Shell Governance Maintainer  
   - Decision Gate: Phase 6 observability schema review

3. **模板是否默认展示 `ShowAbout` allowlist 开关示例（当前为注释+marker，不自动启用）**  
   - Owner: Template Maintainer  
   - Decision Gate: template DX review before archive
