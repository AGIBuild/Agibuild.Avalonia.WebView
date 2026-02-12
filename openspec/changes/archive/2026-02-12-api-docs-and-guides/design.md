# API Docs and Guides — Design

**ROADMAP**: Phase 3.6 + 3.7

## docfx.json

Configured for Core, Runtime, WebView projects. Metadata from project files; flattened namespace layout; modern template. Output to `_site`.

## XML Documentation

- `GenerateDocumentationFile` enabled in root `Directory.Build.props`
- Disabled in `tests/Directory.Build.props` and `benchmarks/Directory.Build.props`

## Guide Articles

| Article | Location | Content |
|---------|----------|---------|
| Getting Started | docs/articles/getting-started.md | Prerequisites, Quick Start (template), Manual Setup, Navigation |
| Bridge | docs/articles/bridge-guide.md | [JsExport]/[JsImport], bridge usage |
| SPA Hosting | docs/articles/spa-hosting.md | Embedded resources, dev proxy |
| Architecture | docs/articles/architecture.md | Design overview |

docs/toc.yml and docs/articles/toc.yml for navigation.
