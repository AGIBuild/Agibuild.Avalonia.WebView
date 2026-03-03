# Plugin Registry & Discovery — Tasks

## 1. Plugin Manifest Convention

- [x] 1.1 Define `fulora-plugin.json` schema (id, displayName, services, npmPackage, minFuloraVersion?, platforms?)
- [x] 1.2 Document schema in plugin authoring guide and OpenSpec
- [x] 1.3 Add `fulora-plugin.json` to LocalStorage plugin project with correct content
- [x] 1.4 Configure LocalStorage .csproj to pack manifest at package root (`PackagePath="/"`)

## 2. NuGet Tag Convention

- [x] 2.1 Add `fulora-plugin` to LocalStorage plugin `PackageTags` in .csproj
- [x] 2.2 Update plugin authoring guide to require `fulora-plugin` tag
- [x] 2.3 Verify CI/build does not strip or override the tag

## 3. CLI Search Command

- [x] 3.1 Create `SearchCommand.cs` with `fulora search [query]` subcommand
- [x] 3.2 Implement NuGet V3 Search API client (HTTP, service index → query)
- [x] 3.3 Parse search results and display package ID, version, description
- [x] 3.4 Add install hint per result: `fulora add plugin <id>`
- [x] 3.5 Handle API errors and network failures gracefully
- [x] 3.6 Register SearchCommand in Program.cs

## 4. CLI Add Plugin Command

- [x] 4.1 Add `plugin` subcommand under `add` (alongside `service`): `fulora add plugin <package-name>`
- [x] 4.2 Implement project detection (Bridge, web dir) reusing AddCommand logic
- [x] 4.3 Run `dotnet add package <package-name>` on target project
- [x] 4.4 Resolve npm package name: from manifest (if available) or convention
- [x] 4.5 Run `npm install <npm-package>` in web directory
- [x] 4.6 Add `--project` and `--web-dir` options for override
- [x] 4.7 Handle manifest fetch from NuGet package (post-install or from cache)

## 5. CLI List Plugins Command

- [x] 5.1 Create `ListCommand.cs` with `fulora list plugins` (or `list` → `plugins` subcommand)
- [x] 5.2 Scan solution .csproj files for PackageReference
- [x] 5.3 Filter to Fulora plugins (ID pattern or tag check)
- [x] 5.4 Display package ID, version, npm companion status
- [x] 5.5 Register ListCommand in Program.cs

## 6. Tests

- [x] 6.1 Unit tests: SearchCommand — mock NuGet API, verify query construction and result parsing
- [x] 6.2 Unit tests: AddPluginCommand — mock dotnet/npm, verify correct invocations
- [x] 6.3 Unit tests: ListPluginsCommand — fixture csproj, verify filtering and output
- [x] 6.4 Integration test: `fulora search` against live NuGet.org (or fixture)
- [x] 6.5 Integration test: `fulora add plugin` in temp project → verify PackageReference and package.json updated
- [x] 6.6 Governance: Assert LocalStorage plugin has manifest and fulora-plugin tag
