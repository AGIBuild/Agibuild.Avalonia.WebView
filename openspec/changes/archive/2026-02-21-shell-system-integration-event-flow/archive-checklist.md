## Archive-Ready Checklist

- [x] Runtime typed inbound contracts merged (`tray/menu` events).
- [x] Policy-first inbound routing merged (deny blocks delivery).
- [x] Dynamic menu pruning pipeline merged with deterministic normalization.
- [x] System action whitelist merged with deterministic deny taxonomy.
- [x] Template app-shell 双向演示（command + inbound event drain）完成。
- [x] CT/IT/Governance coverage updated and passing in automation lane commands.
- [x] Verification evidence completed (`verification-evidence.md`).
- [x] Scope boundary notes completed (`roadmap-notes.md`).

## Open Questions / Owners / Decision Gate

1. **是否引入 `ShowAbout` action 到白名单**  
   - Owner: Runtime Maintainer  
   - Decision Gate: 下一次 `shell-system-integration` OpenSpec 增量评审

2. **tray event payload 是否允许平台原始字段透传**  
   - Owner: Template + Runtime Maintainer  
   - Decision Gate: 需要完成跨平台一致性评估后再进入契约

3. **菜单裁剪是否接入 session permission profile 联合决策**  
   - Owner: Shell Governance Maintainer  
   - Decision Gate: 等待 phase 6 profile strategy 定稿
