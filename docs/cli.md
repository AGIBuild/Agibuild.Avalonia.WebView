# Fulora CLI

The `fulora` CLI is a .NET global tool for the main Fulora app-building path: create a project, run it locally, and package it for distribution.

## Installation

```bash
dotnet tool install -g Agibuild.Fulora.Cli
```

## Primary Path

Start here if you are building an app.

### `fulora new <name>`

Create a new Fulora hybrid app from the template.

```bash
fulora new MyApp --frontend react
fulora new MyApp --frontend vue --shell-preset app-shell
```

| Option | Description |
|---|---|
| `--frontend`, `-f` | **Required.** `react`, `vue`, or `vanilla` |
| `--shell-preset` | Desktop shell preset: `baseline` or `app-shell` |

### `fulora dev`

Start the Vite dev server and Avalonia desktop app together. Run from the solution root.

```bash
fulora dev
fulora dev --web ./MyApp.Web.Vite.React --desktop ./MyApp.Desktop/MyApp.Desktop.csproj
```

| Option | Description |
|---|---|
| `--web` | Web project directory (auto-detected) |
| `--desktop` | Desktop `.csproj` path (auto-detected) |
| `--npm-script` | npm script name (default: `dev`) |

Press **Ctrl+C** to stop both processes.

### `fulora package`

Package your app for distribution. The recommended first path is to start with a named profile.

```bash
fulora package --project ./src/MyApp.Desktop/MyApp.Desktop.csproj --profile desktop-public
fulora package --project ./src/MyApp.Desktop/MyApp.Desktop.csproj --profile mac-notarized
```

Available profiles today:

- `desktop-public`
- `desktop-internal`
- `mac-notarized`

| Option | Description |
|---|---|
| `--profile` | Packaging profile with recommended defaults |
| `--project`, `-p` | Path to the `.csproj` (required) |
| `--runtime`, `-r` | Target RID such as `win-x64`, `osx-arm64`, or `linux-x64` |
| `--version`, `-v` | Package version (semver). Defaults to the project version |
| `--output`, `-o` | Output directory. Defaults to `./Releases` under the project |
| `--icon`, `-i` | Path to the app icon |
| `--sign-params`, `-n` | Raw signing parameters passed to `vpk` |
| `--notarize` | Enable macOS notarization |
| `--channel`, `-c` | Release channel |

If `vpk` is not installed, `fulora package` falls back to copying the `dotnet publish` output into the output directory.

## Advanced Workflows

Use these commands after the main path is already working.

### `fulora generate types`

Build the Bridge project and extract generated TypeScript declarations.

```bash
fulora generate types
fulora gen types --project ./MyApp.Bridge/MyApp.Bridge.csproj --output ./MyApp.Web/src/bridge
```

| Option | Description |
|---|---|
| `--project`, `-p` | Bridge `.csproj` path (auto-detected) |
| `--output`, `-o` | Output directory for generated `.d.ts` files (auto-detected) |

### `fulora add service <name>`

Scaffold a new bridge service with three files:

1. C# interface (`[JsExport]`) in the Bridge project
2. C# implementation in the Desktop project
3. TypeScript proxy in the web project

```bash
fulora add service NotificationService --layer bridge
fulora add service IAnalyticsService --layer plugin --import
```

| Option | Description |
|---|---|
| `--layer` | **Required.** Service ownership layer: `bridge`, `framework`, or `plugin` |
| `--import` | Generate `[JsImport]` instead of `[JsExport]` |
| `--bridge-project` | Bridge project path (auto-detected) |
| `--web-dir` | Web project `src/` directory (auto-detected) |

### `fulora add plugin <package>`

Install a Fulora bridge plugin NuGet package into the current project.

```bash
fulora add plugin Agibuild.Fulora.Plugin.Database
fulora add plugin Agibuild.Fulora.Plugin.HttpClient --project ./MyApp.Desktop/MyApp.Desktop.csproj
```

| Option | Description |
|---|---|
| `--project`, `-p` | Path to the `.csproj` file (auto-detected if omitted) |

### `fulora search [query]`

Search NuGet.org for Fulora bridge plugins tagged `fulora-plugin`.

```bash
fulora search database
fulora search http --take 20
```

| Option | Description |
|---|---|
| `--take` | Maximum results to return (default: 20) |

### `fulora list plugins`

List the Fulora plugin packages installed in the current project.

```bash
fulora list plugins
fulora list plugins --check
```

| Option | Description |
|---|---|
| `--check` | Check plugin compatibility with the installed Fulora version |
