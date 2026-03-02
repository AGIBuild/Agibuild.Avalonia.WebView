## 1. Release Workflow — npm Publish Job

- [x] 1.1 Add `npm-publish` job to `.github/workflows/release.yml` with Node.js 22.x setup, `npm ci`, `npm run build`, version sync from git tag, and `npm publish --access public`
- [x] 1.2 Add pre-release tag detection: publish with `--tag preview` for pre-release versions, `--tag latest` for stable
- [x] 1.3 Add idempotent handling: treat npm 409 (version already exists) as success via `|| true` with logged warning

## 2. Package Metadata Verification

- [x] 2.1 Verify `packages/bridge/package.json` metadata: license, repository URL, description, keywords are complete and accurate
- [x] 2.2 Verify NuGet `.csproj` files have complete metadata for stable release: license expression, project URL, description without "preview" language

## 3. Documentation

- [x] 3.1 Create release checklist document (`docs/release-checklist.md`) covering: NPM_TOKEN setup, tag creation, post-publish verification steps
