## Context

`@agibuild/bridge` is the JavaScript client SDK for the Fulora WebView bridge. It already has:
- ESM-only package at `packages/bridge/` with `dist/index.js` + `dist/index.d.ts`
- `BridgeDistributionGovernance` target validating build, pnpm/yarn parity, and Node LTS import
- Package-manager consumer smoke tests in CI

Missing for publication: npm registry metadata and a publish automation target.

## Goals / Non-Goals

**Goals:**
- Make the package publishable to npmjs.com with correct metadata
- Add nuke build target for `npm publish` with token-based auth
- Keep publication gated behind explicit `NPM_TOKEN` environment variable

**Non-Goals:**
- Dual CJS/ESM output (ESM-only is the project's intentional choice)
- Automated version bumping (manual, aligned with NuGet release cycle)
- npm provenance/attestation (post-1.0 enhancement)

## Decisions

### D1: npm authentication strategy

**Choice**: Use `NPM_TOKEN` environment variable with `.npmrc` token injection at publish time via `npm publish --//registry.npmjs.org/:_authToken=${NPM_TOKEN}`.

**Rationale**: Standard approach for CI-based npm publication. No persistent `.npmrc` in repo. Token is injected only during the publish target.

### D2: Package version strategy

**Choice**: Keep version at `0.1.0` for now. The version will be bumped to `1.0.0` as part of M9.6 (Stable Release Gate) when the NuGet version also transitions to 1.0.0.

**Rationale**: npm and NuGet versions should be aligned at GA. Premature 1.0.0 on npm while NuGet is still preview creates confusion.

### D3: NpmPublish target placement

**Choice**: Add as a standalone target in `Build.Packaging.cs`, gated by `NPM_TOKEN` parameter. The existing `Publish` target (NuGet) remains separate — both are invoked by CI but independently.

**Rationale**: npm and NuGet publishing have different authentication, failure modes, and retry semantics. Keeping them separate avoids coupling failures.

### D4: Access level

**Choice**: Publish with `--access public` since `@agibuild/bridge` is a scoped package and scoped packages default to restricted on npmjs.com.

**Rationale**: The package is open-source (MIT license) and intended for public consumption.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| npm token leak | Token is never stored in repo; injected via environment variable |
| Version drift between npm and NuGet | Documented alignment policy; both bumped in M9.6 |
| Package name squatting | `@agibuild` scope is organization-controlled |

## Testing Strategy

- **Governance**: `BridgeDistributionGovernance` already validates the package builds and imports correctly
- **Publish dry-run**: `NpmPublish` target includes `--dry-run` mode for local testing
- **CI**: `nuke Test` must pass after changes
