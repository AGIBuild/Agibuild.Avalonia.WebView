# Documentation Site Deployment — Tasks

## 1. CI Workflow for Docs Deploy

- [x] 1.1 Create `.github/workflows/docs-deploy.yml` with trigger on push to `main` (optionally path filter `docs/**`, `src/**`, `openspec/**` to avoid runs when only unrelated files change)
- [x] 1.2 Add job to install docfx (via `dotnet tool restore` or `dotnet tool install docfx`) and .NET SDK
- [x] 1.3 Add step to run `docfx build` in `docs/` directory and produce `_site` output
- [x] 1.4 Add step to upload `docs/_site` as pages artifact using `actions/upload-pages-artifact`
- [x] 1.5 Add deploy step using `actions/deploy-pages` with `pages` permission

## 2. docfx Tool Configuration

- [x] 2.1 Add `docfx` to `dotnet-tools.json` (or equivalent) if not already present, so `dotnet tool restore` installs it
- [x] 2.2 Verify docfx build succeeds locally and in CI before enabling deploy

## 3. Documentation and Verification

- [x] 3.1 Document GitHub Pages setup: enable Pages with "GitHub Actions" as source in repository settings
- [x] 3.2 Add or update README/docs link to the deployed site URL once live
- [x] 3.3 Verify deployed site loads correctly and all sections (API, Getting Started, articles) are accessible
