# Documentation Site Deployment

**Goal IDs**: E2 (Dev Tooling), Phase 3 / 3.6 (API reference site), Phase 3 / 3.7 (Getting Started + topic guides)

## Why

The project has an API docs site target using docfx and XML docs (Phase 3 deliverable 3.6), plus Getting Started and topic guides (3.7). These exist in `docs/` with `docfx.json` configured, but the site is not deployed anywhere. Developers cannot discover or browse the documentation online — only locally via `docfx build`. Deploying to GitHub Pages closes the adoption gap and aligns with E2 (Dev Tooling) by making documentation discoverable and always available.

## What Changes

- Add a GitHub Actions workflow (or job) to build the docfx site and deploy it to GitHub Pages
- Configure the docs site to publish from `docs/_site` to the `gh-pages` branch or `github-pages` deployment target
- Ensure the deployed site includes: API reference (auto-generated from XML comments), Getting Started guide, architecture overview, and topic guides
- Add a `docs-deploy` or equivalent CI trigger (e.g., on push to `main`, or on release tag)

## Non-goals

- Changing docfx configuration or content structure — already in place
- Adding new documentation articles — scope is deployment only
- Custom domain or CDN — GitHub Pages default is sufficient for 1.0

## Capabilities

### New Capabilities
- `documentation-site-deploy`: Automated deployment of the documentation site to GitHub Pages, including API reference, Getting Started, architecture overview, and topic guides

### Modified Capabilities
- None (deployment is additive; existing `api-docs` spec governs content generation)

## Impact

- `.github/workflows/` — new workflow or job for docs build and deploy (e.g., `docs-deploy.yml` or addition to `ci.yml`)
- `docs/` — no structural changes; `docfx.json` and `_site` output remain as-is
- GitHub Pages — repository settings may require enabling Pages with source `gh-pages` or `GitHub Actions`
