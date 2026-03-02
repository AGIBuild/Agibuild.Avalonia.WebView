# Documentation Site Deployment

The API reference and guides are built with [docfx](https://dotnet.github.io/docfx/) and deployed automatically to GitHub Pages.

## How it works

1. On every push to `main` that touches `docs/**`, `src/**/*.cs`, `src/**/*.csproj`, or the workflow itself, the **Deploy Documentation** workflow runs.
2. The workflow installs docfx via `dotnet tool restore`, builds the site with `docfx docs/docfx.json`, and deploys `docs/_site` to GitHub Pages using `actions/deploy-pages`.

## GitHub Pages setup (one-time)

1. Go to **Settings → Pages** in the GitHub repository.
2. Under **Build and deployment → Source**, select **GitHub Actions**.
3. No branch selection is needed — the workflow handles deployment via the `github-pages` environment.
4. The site will be available at `https://<org>.github.io/Agibuild.Avalonia.WebView/` after the first successful deploy.

## Building locally

```bash
# Restore tools (includes docfx)
dotnet tool restore

# Build the docs site
dotnet docfx docs/docfx.json

# Serve locally for preview
dotnet docfx serve docs/_site
```

The site output is in `docs/_site/` (gitignored).

## What the site includes

| Section | Source | Description |
|---------|--------|-------------|
| API Reference | XML docs from `src/` projects | Auto-generated from `///` comments |
| Getting Started | `docs/articles/getting-started.md` | Quick start guide |
| Bridge Guide | `docs/articles/bridge-guide.md` | Type-safe bridge usage |
| SPA Hosting | `docs/articles/spa-hosting.md` | Embedded SPA hosting |
| Architecture | `docs/articles/architecture.md` | System architecture overview |

## Workflow file

`.github/workflows/docs-deploy.yml` — triggers on push to `main`, uses `actions/upload-pages-artifact@v3` and `actions/deploy-pages@v4`.

## Troubleshooting

- **docfx metadata warnings**: Some `InvalidFileLink` warnings for `openspec/` links are expected — those files are outside the docs scope.
- **Pages not deploying**: Verify that GitHub Pages source is set to "GitHub Actions" in repository settings.
- **Missing API docs**: Ensure `GenerateDocumentationFile` is enabled in `Directory.Build.props` for product projects.
