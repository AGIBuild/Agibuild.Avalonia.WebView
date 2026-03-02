# Documentation Site Deployment — Design

**Phase alignment**: E2 (Dev Tooling), Phase 3 / 3.6 (API reference site), Phase 3 / 3.7 (Getting Started + topic guides)

## Context

The repository already has:
- `docs/docfx.json` — configured for Core, Runtime, Avalonia projects; metadata from XML docs; output to `_site`
- `docs/articles/` — Getting Started, Bridge, SPA Hosting, Architecture guides
- `docs/toc.yml` and `docs/articles/toc.yml` — navigation structure
- `Directory.Build.props` — `GenerateDocumentationFile` enabled for product assemblies

**Gap**: No CI job builds or deploys the docs site. The `_site` directory is gitignored; developers must run `docfx build` locally. There is no public URL for the documentation.

## Goals / Non-Goals

**Goals:**
- Deploy the documentation site to GitHub Pages automatically
- Include API reference (from XML docs), Getting Started, architecture overview, and topic guides
- Trigger deployment on push to `main` (or configurable branch) so docs stay current
- Use standard GitHub Actions + `actions/upload-pages-artifact` + `actions/deploy-pages` pattern

**Non-Goals:**
- Changing docfx configuration, content, or article structure
- Custom domain or CDN
- Versioned docs (e.g., per-release snapshots) — single latest version is sufficient for 1.0

## Decisions

### D1: Deployment trigger

**Choice**: Deploy on push to `main` (and optionally `docs/**` path filter to avoid unnecessary runs).

**Alternatives considered:**
- Deploy only on release tag: Docs would lag behind; developers expect docs to reflect latest `main`
- Manual workflow_dispatch only: Adds friction; docs can go stale

**Rationale**: `main` is the canonical source of truth. Deploying on push keeps docs in sync with the codebase.

### D2: GitHub Pages deployment method

**Choice**: Use `actions/upload-pages-artifact` + `actions/deploy-pages` (GitHub Actions deployment).

**Alternatives considered:**
- Push to `gh-pages` branch: Requires extra permissions, branch management, and can conflict with other workflows
- `peaceiris/actions-gh-pages`: Third-party; `actions/deploy-pages` is first-party and recommended by GitHub

**Rationale**: First-party Actions are maintained, well-documented, and integrate with repository Pages settings.

### D3: Workflow structure

**Choice**: Separate `docs-deploy.yml` workflow for clarity and independent triggers.

**Alternatives considered:**
- Add job to `ci.yml`: Mixes build/test with deploy; different trigger semantics
- Add job to `release.yml`: Docs would only deploy on release; too infrequent

**Rationale**: Dedicated workflow keeps concerns separated and allows path-based triggers (e.g., `docs/**`) without affecting CI.

### D4: docfx installation

**Choice**: Use `dotnet tool install docfx` or `docfx-json` in CI, or rely on `docfx` as a .NET tool if already in the repo.

**Alternatives considered:**
- Pre-built docfx Docker image: Heavier; docfx .NET tool is sufficient
- Chocolatey/winget: Platform-specific; .NET tool works cross-platform

**Rationale**: `docfx` is a .NET tool; `dotnet tool restore` (if in `dotnet-tools.json`) or explicit install keeps CI simple and cross-platform.

## Risks / Trade-offs

- **[Risk] docfx not in dotnet-tools.json** → Add it or use `dotnet tool install -g docfx` in the workflow. Verify docfx version compatibility.
- **[Risk] GitHub Pages not enabled** → Repository settings must enable Pages with "GitHub Actions" as source. Document in setup.
- **[Trade-off] Deploy on every push to main** → Slight CI cost; acceptable for docs freshness. Path filter on `docs/**` can reduce runs when only code changes.
- **[Trade-off] Single latest version** → No versioned docs per release. Acceptable for 1.0; can add later if needed.

## Testing Strategy

- **Local validation**: Run `docfx build` in `docs/` and verify `_site` output; ensure no broken links.
- **CI dry-run**: Add a `docs-build` job (build only, no deploy) to validate docfx succeeds in CI before enabling deploy.
- **Post-deploy**: After first deploy, verify site loads at `https://<org>.github.io/<repo>/` and all sections (API, Getting Started, articles) are accessible.
