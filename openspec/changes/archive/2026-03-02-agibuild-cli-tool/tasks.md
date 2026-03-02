## 1. CLI Project Setup

- [x] 1.1 Create `src/Agibuild.Fulora.Cli/` project with `DotnetToolManifest` package type and `agibuild` as tool command
- [x] 1.2 Add System.CommandLine (or equivalent) for command parsing
- [x] 1.3 Implement `--help` and `--version` for root and subcommands

## 2. agibuild new

- [x] 2.1 Implement `agibuild new <name> --frontend react|vue|svelte` subcommand
- [x] 2.2 Map `--frontend` to `dotnet new agibuild-hybrid --framework` (or equivalent template parameter)
- [x] 2.3 Invoke `dotnet new` in the current directory; create project folder named `<name>`
- [x] 2.4 Add validation: require `--frontend` or document default; fail with clear message if invalid

## 3. agibuild generate types

- [x] 3.1 Implement `agibuild generate types` subcommand
- [x] 3.2 Detect solution/Bridge project (e.g. from current directory or `--project`)
- [x] 3.3 Build Bridge project to trigger source generator; locate emitted TypeScript declarations
- [x] 3.4 Copy or write generated `.d.ts` to web project types directory (e.g. `web/src/bridge/` or per-template convention)

## 4. agibuild dev

- [x] 4.1 Implement `agibuild dev` subcommand
- [x] 4.2 Detect web project (package.json) and Desktop project (.csproj)
- [x] 4.3 Start Vite dev server (`npm run dev` or `npx vite`) in web project directory
- [x] 4.4 Start Avalonia app (`dotnet run`) in Desktop project directory
- [x] 4.5 Run both processes in parallel; forward stdout/stderr
- [x] 4.6 Handle Ctrl+C / SIGINT to terminate both processes gracefully

## 5. agibuild add service

- [x] 5.1 Implement `agibuild add service <name>` subcommand
- [x] 5.2 Generate C# interface with `[JsExport]` in Bridge project (or `[JsImport]` with `--import` flag)
- [x] 5.3 Generate C# implementation class in Bridge project
- [x] 5.4 Generate TypeScript proxy/stub in web project
- [x] 5.5 Use consistent naming: PascalCase for C#, camelCase for TS

## 6. Packaging and Documentation

- [x] 6.1 Add `Agibuild.Fulora.Cli` to solution and build pipeline
- [x] 6.2 Configure NuGet package metadata (version, description, authors)
- [x] 6.3 Update getting-started docs to recommend `agibuild new` and `agibuild dev`
- [x] 6.4 Add CLI usage section to README or docs
