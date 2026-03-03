## Why

Hybrid apps need to read/write files (exports, imports, temp files, user documents). Browser File API is limited. A bridge plugin provides sandboxed file system access through the host, with configurable root directories and path restrictions. Goal: Phase 11 M11.3.

## What Changes

- New NuGet: Agibuild.Fulora.Plugin.FileSystem implementing IBridgePlugin
- New npm: @agibuild/bridge-plugin-file-system with TypeScript types
- [JsExport] IFileSystemService: readText, writeText, readBinary, writeBinary, list, delete, exists, createDirectory
- Sandboxed to configurable root directories (app data, documents, temp)
- Path traversal prevention (no ../ escape)

## Capabilities

### New Capabilities
- `plugin-file-system`: Bridge plugin for sandboxed file system operations

### Modified Capabilities
(none)

## Non-goals

- Unrestricted file system access
- File watching/change notifications (future plugin)
- Cloud storage integration

## Impact

- New project: src/Agibuild.Fulora.Plugin.FileSystem/
- New npm: packages/bridge-plugin-file-system/
- No external dependencies
