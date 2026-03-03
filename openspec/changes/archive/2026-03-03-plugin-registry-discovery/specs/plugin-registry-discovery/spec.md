# Plugin Registry & Discovery — Spec

## Purpose

Define requirements for Fulora plugin discovery, search, and installation via the CLI. Enables developers to find, add, and list bridge plugins without manual registry lookups.

## Requirements

### Requirement: Plugin manifest file convention (fulora-plugin.json)

Fulora plugins SHALL include a `fulora-plugin.json` manifest file at the NuGet package root, providing structured metadata for discovery and installation.

#### Scenario: Manifest schema includes required fields

- **WHEN** a Fulora plugin package is built
- **THEN** the package SHALL contain `fulora-plugin.json` at the package root
- **AND** the manifest SHALL include: `id` (NuGet package ID), `displayName`, `services` (array of service names), `npmPackage` (npm companion package name)
- **AND** the manifest MAY include: `minFuloraVersion`, `platforms` (optional compatibility hints)

#### Scenario: Manifest is embedded as content in NuGet package

- **WHEN** a plugin project builds its NuGet package
- **THEN** `fulora-plugin.json` SHALL be included as a content file (e.g., `<Content Include="fulora-plugin.json" Pack="true" PackagePath="/" />`)
- **AND** the file SHALL be at the root of the extracted package (no subdirectory)

#### Scenario: Invalid manifest is handled gracefully

- **WHEN** `fulora-plugin.json` is missing or malformed
- **THEN** the CLI SHALL treat the package as a legacy plugin (no manifest)
- **AND** the CLI SHALL fall back to convention for npm package name (`@agibuild/bridge-plugin-{name}`)

---

### Requirement: NuGet tag convention (fulora-plugin)

Fulora plugins SHALL use the `fulora-plugin` tag in NuGet package metadata to enable discovery via search.

#### Scenario: Plugin package includes fulora-plugin tag

- **WHEN** a Fulora plugin is published to NuGet.org
- **THEN** the package SHALL include `fulora-plugin` in its `PackageTags` (or equivalent metadata)
- **AND** the tag SHALL be present for the package to appear in `fulora search` results

#### Scenario: Reference plugin adopts tag convention

- **WHEN** the LocalStorage plugin (`Agibuild.Fulora.Plugin.LocalStorage`) is updated
- **THEN** its `.csproj` SHALL include `fulora-plugin` in `PackageTags`
- **AND** the package SHALL be discoverable via `fulora search`

---

### Requirement: fulora search command behavior

The `fulora search [query]` command SHALL query NuGet.org for packages tagged `fulora-plugin` and display results.

#### Scenario: Search returns packages with fulora-plugin tag

- **WHEN** a user runs `fulora search` (no query)
- **THEN** the command SHALL query the NuGet V3 Search API with `q=fulora-plugin`
- **AND** the command SHALL display matching packages with: package ID, version, description (truncated)

#### Scenario: Search filters by user query

- **WHEN** a user runs `fulora search storage`
- **THEN** the command SHALL query with `q=fulora-plugin storage` (or equivalent combined query)
- **AND** the command SHALL display only packages matching both the tag and the user query

#### Scenario: Search displays install hint

- **WHEN** search results are displayed
- **THEN** each result SHALL include an install hint: `fulora add plugin <package-id>`
- **AND** the output SHALL be human-readable (table or list format)

#### Scenario: Search handles API errors gracefully

- **WHEN** the NuGet API is unreachable or returns an error
- **THEN** the command SHALL display a clear error message
- **AND** the command SHALL exit with a non-zero code

---

### Requirement: fulora add plugin command behavior

The `fulora add plugin <package-name>` command SHALL install both the NuGet package and its npm companion in one step.

#### Scenario: Add plugin installs NuGet and npm packages

- **WHEN** a user runs `fulora add plugin Agibuild.Fulora.Plugin.LocalStorage` from a solution directory
- **THEN** the command SHALL run `dotnet add package Agibuild.Fulora.Plugin.LocalStorage` on the appropriate project (Bridge or host)
- **AND** the command SHALL run `npm install @agibuild/bridge-plugin-local-storage` (or the manifest-specified npm package) in the web project directory

#### Scenario: Add plugin resolves npm package from manifest

- **WHEN** the plugin NuGet package contains `fulora-plugin.json` with an `npmPackage` field
- **THEN** the command SHALL use that value for the npm install
- **AND** the command SHALL NOT use the convention if the manifest specifies a different npm package

#### Scenario: Add plugin falls back to convention when manifest missing

- **WHEN** the plugin has no manifest or no `npmPackage` field
- **THEN** the command SHALL derive the npm package name from the convention: `@agibuild/bridge-plugin-{suffix}` where suffix is derived from the NuGet package ID (e.g., `Agibuild.Fulora.Plugin.LocalStorage` → `local-storage`)

#### Scenario: Add plugin detects project structure

- **WHEN** a user runs `fulora add plugin <name>` from a solution directory
- **THEN** the command SHALL auto-detect the Bridge project (or host project) using the same logic as `fulora add service`
- **AND** the command SHALL auto-detect the web project directory for npm install
- **AND** the command SHALL support `--project` and `--web-dir` options for explicit override

#### Scenario: Add plugin fails clearly when project not found

- **WHEN** the Bridge project or web directory cannot be detected
- **THEN** the command SHALL output a clear error message
- **AND** the command SHALL exit with a non-zero code

---

### Requirement: fulora list plugins command behavior

The `fulora list plugins` command SHALL display plugins installed in the current project.

#### Scenario: List plugins shows installed Fulora plugins

- **WHEN** a user runs `fulora list plugins` from a solution directory
- **THEN** the command SHALL scan the solution's .csproj files for `PackageReference` entries
- **AND** the command SHALL filter to packages that are Fulora plugins (e.g., ID contains `Fulora.Plugin` or has `fulora-plugin` tag)
- **AND** the command SHALL display: package ID, version, and npm companion status (installed or not)

#### Scenario: List plugins shows empty when none installed

- **WHEN** no Fulora plugins are referenced in the project
- **THEN** the command SHALL display an empty list or a message indicating no plugins installed
- **AND** the command SHALL exit successfully (zero code)

#### Scenario: List plugins checks npm package presence

- **WHEN** a plugin is listed and has an npm companion
- **THEN** the command SHALL indicate whether the npm package is present in the web project's `package.json` dependencies
- **AND** the output SHALL distinguish "NuGet only" vs "NuGet + npm" for each plugin
