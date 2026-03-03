# Plugin Registry & Discovery — Design

## Context

Fulora has a working plugin convention (`IBridgePlugin`, `UsePlugin<T>`, NuGet+npm dual distribution) with one reference plugin (LocalStorage). Developers cannot discover available plugins — there is no search, no catalog, no metadata convention. Without discoverability, the plugin ecosystem cannot grow beyond what the core team builds.

This change introduces plugin discovery by leveraging existing public registries (NuGet.org, npmjs.com) and adding a lightweight metadata convention. No self-hosted infrastructure is required.

## Goals / Non-Goals

### Goals

- Enable developers to search for Fulora plugins via CLI
- Provide a one-step install for both NuGet and npm packages
- Define a machine-readable plugin manifest for richer metadata
- Establish a NuGet tag convention for discoverability
- List installed plugins in the current project

### Non-Goals

- Self-hosted registry server — we leverage NuGet.org and npmjs.com
- Plugin marketplace UI — CLI-first; web catalog can come later
- Plugin quality scoring or certification
- Automatic runtime compatibility verification

## Decisions

### D1: Use NuGet.org as the registry (no self-hosted)

**Decision**: Use NuGet.org as the sole source for plugin discovery. No self-hosted registry.

**Rationale**: NuGet.org is the standard .NET package registry. Fulora plugins are NuGet packages; tagging them with `fulora-plugin` makes them discoverable without additional infrastructure. Reduces operational burden and aligns with ecosystem expectations.

### D2: NuGet tag convention — `fulora-plugin`

**Decision**: Fulora plugins SHALL include the tag `fulora-plugin` in their NuGet package metadata (`PackageTags`). The CLI search command queries NuGet.org using this tag in the search query.

**Rationale**: NuGet Search API uses full-text search on the `q` parameter; tags are indexed. Including `fulora-plugin` in the query returns packages that have this tag. Simple, convention-based, no API changes required.

### D3: Plugin manifest format — `fulora-plugin.json` in NuGet package root

**Decision**: Each Fulora plugin NuGet package SHALL include a `fulora-plugin.json` file at the package root (content file, copied to output). The manifest contains: plugin ID, display name, services list, npm companion package name, minimum Fulora version, and optional platform compatibility.

**Rationale**: NuGet package metadata (description, tags) is limited. A structured manifest enables richer CLI output (service list, install instructions) and future extensibility (compatibility checks, platform filters) without changing the NuGet schema.

### D4: CLI uses NuGet V3 Search API for discovery

**Decision**: The `fulora search` command SHALL call the NuGet V3 Search API (service index → SearchQueryService resource) with `q=fulora-plugin` (and optional user query terms). Parse the JSON response to extract package ID, version, description, tags.

**Rationale**: NuGet V3 is the standard API. No NuGet client SDK required for read-only search; HTTP + JSON is sufficient. Search results can be augmented with manifest data when the package is fetched (e.g., via `dotnet nuget list source` or package content URL) — for MVP, package metadata from search is sufficient.

### D5: `fulora add plugin` runs `dotnet add package` + `npm install`

**Decision**: `fulora add plugin <package-name>` SHALL (1) run `dotnet add package <package-name>` on the host/Bridge project, and (2) resolve the npm companion package name from the plugin manifest (or a convention: `@agibuild/bridge-plugin-{name}`) and run `npm install <npm-package>` in the web project directory.

**Rationale**: Reuses existing tooling. Single command for dual-package install improves DX. Manifest provides the npm package name when it differs from the convention.

### D6: `fulora list plugins` scans csproj PackageReference + fulora-plugin tag

**Decision**: `fulora list plugins` SHALL scan the solution's .csproj files for `PackageReference` items, filter by packages that have the `fulora-plugin` tag (either by querying NuGet.org for each, or by checking a local manifest/cache), and display name, version, and npm companion status.

**Rationale**: Source of truth is the project file. For performance, we can check NuGet package metadata only for packages matching a known pattern (e.g., `Agibuild.Fulora.Plugin.*`) or maintain a small allowlist. Alternatively, require manifest in package and read it from the NuGet cache if available.

**Simplification for MVP**: List all `PackageReference` entries whose ID contains `Fulora.Plugin` or matches a pattern. Optionally call NuGet API to confirm tag — can be deferred to avoid N+1 API calls.

## Risks / Trade-offs

### R1: NuGet search by tag is best-effort

**Risk**: NuGet Search API does not have a dedicated tag filter. Querying `q=fulora-plugin` relies on full-text match; false positives (packages mentioning "fulora-plugin" in description) or false negatives (tag not indexed) are possible.

**Mitigation**: Document the tag requirement clearly. Use `packageType` if NuGet supports custom types; otherwise accept best-effort. Over time, community plugins will adopt the convention.

### R2: Manifest not available until package is installed

**Risk**: Search results show NuGet metadata only. The manifest `fulora-plugin.json` is inside the package; we cannot read it without downloading the package. CLI search will show name, description, version from NuGet; richer metadata (services, npm name) appears after install or when viewing a specific package.

**Mitigation**: For search, NuGet metadata is sufficient (name, description, version). For `fulora add plugin`, we fetch the package and can read the manifest before/after install to get the npm package name. For `fulora list plugins`, we read manifests from the NuGet package cache if available.

### R3: npm package name resolution

**Risk**: Convention `@agibuild/bridge-plugin-{name}` may not hold for third-party plugins (different scope, different naming).

**Mitigation**: Manifest SHALL include `npmPackage` field. If absent, fall back to convention. Document both in plugin authoring guide.

### R4: Project structure detection for `add plugin`

**Risk**: `fulora add plugin` must find the correct .csproj (host or Bridge) and the web project directory for `npm install`. Multi-project solutions may have ambiguous structure.

**Mitigation**: Reuse the same project resolution logic as `fulora add service` (detect Bridge project, infer web dir). Add `--project` and `--web-dir` options for explicit override.
