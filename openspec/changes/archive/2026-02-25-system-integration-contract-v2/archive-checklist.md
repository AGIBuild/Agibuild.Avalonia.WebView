## Archive Checklist

## Completion

- [x] Runtime contract hardening implemented (`ShowAbout` whitelist v2 + tray payload v2 schema governance).
- [x] Template app-shell v2 flow updated and governance markers covered.
- [x] CT matrix / runtime-critical-path / shell-production-matrix updated with v2 scenario.
- [x] Unit + integration verification commands executed with pass results.
- [x] Verification evidence and roadmap notes prepared for archive review.

## Open Questions

1. Should template preset expose a toggle to allow `ShowAbout` by default in sample UX?
   - Owner: Runtime + Template maintainers
   - Decision Gate: Before next template UX polish increment
2. Do we need an explicit reserved key registry under `platform.*` (e.g. `platform.profile.*`)?
   - Owner: Shell governance maintainers
   - Decision Gate: Before introducing third-party extension packs
3. Should `OccurredAtUtc` precision/format be normalized to a fixed wire representation in bridge serialization tests?
   - Owner: Bridge contract maintainers
   - Decision Gate: Before public contract freeze for stable release
