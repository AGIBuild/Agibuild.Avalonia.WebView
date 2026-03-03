# File System Plugin — Tasks

## 1. Project Setup

- [x] 1.1 Create `src/Agibuild.Fulora.Plugin.FileSystem/` project with reference to Agibuild.Fulora.Core
- [x] 1.2 Configure .csproj for NuGet packaging (PackageId, Version, Description, fulora-plugin tag)
- [x] 1.3 Add `fulora-plugin.json` manifest at package root
- [x] 1.4 Add project to solution and verify build

## 2. IFileSystemService Contract

- [x] 2.1 Define `IFileSystemService` interface with [JsExport]
- [x] 2.2 Add `readText(root, path)` → `Task<string>`
- [x] 2.3 Add `writeText(root, path, content)` → `Task`
- [x] 2.4 Add `readBinary(root, path)` → `Task<byte[]>`
- [x] 2.5 Add `writeBinary(root, path, data)` → `Task` (byte[] parameter)
- [x] 2.6 Add `list(root, path)` → `Task<IEnumerable<FileSystemEntry>>`
- [x] 2.7 Add `delete(root, path)` → `Task`
- [x] 2.8 Add `exists(root, path)` → `Task<bool>`
- [x] 2.9 Add `createDirectory(root, path)` → `Task`
- [x] 2.10 Define `FileSystemEntry` DTO (Name, IsDirectory) and root enum/string type

## 3. Sandbox Implementation

- [x] 3.1 Define `FileSystemPluginOptions` with configurable roots (AppData, Documents, Temp)
- [x] 3.2 Implement path normalization (strip `..`, collapse `.`, handle separators)
- [x] 3.3 Implement path traversal check (reject paths containing `..` or resolving outside root)
- [x] 3.4 Implement `ResolvePath(root, path)` returning full path or throwing
- [x] 3.5 Add per-root permission configuration (read-only, read-write)
- [x] 3.6 Validate roots at plugin registration (non-null, create if missing option)

## 4. Binary Payload Integration

- [x] 4.1 Ensure `readBinary` returns `byte[]` (bridge auto-encodes to base64 for transport)
- [x] 4.2 Ensure `writeBinary` accepts `byte[]` (bridge auto-decodes from base64)
- [x] 4.3 Verify bridge-generated JS/TS uses Uint8Array for binary params/returns
- [x] 4.4 Add integration test for binary round-trip (write binary, read back, compare)

## 5. Service Implementation

- [x] 5.1 Implement `FileSystemService` with all IFileSystemService methods
- [x] 5.2 Implement `FileSystemPlugin` : IBridgePlugin with GetServices()
- [x] 5.3 Wire plugin to accept `FileSystemPluginOptions` (via factory or DI)
- [x] 5.4 Map platform paths for AppData, Documents, Temp (Environment.GetFolderPath)
- [x] 5.5 Handle permission checks before each write/delete/createDirectory

## 6. npm Package

- [x] 6.1 Create `packages/bridge-plugin-file-system/` package
- [x] 6.2 Add package.json with correct name, version, exports
- [x] 6.3 Generate or hand-write TypeScript types for IFileSystemService
- [x] 6.4 Add `getFileSystemService(bridgeClient)` helper
- [x] 6.5 Add root type constants (AppData, Documents, Temp)
- [x] 6.6 Add to npm workspace if applicable

## 7. Tests

- [x] 7.1 Unit tests: Path normalization and traversal rejection
- [x] 7.2 Unit tests: Sandbox boundary enforcement (paths within/outside root)
- [x] 7.3 Unit tests: Permission checks (read-only root rejects writes)
- [x] 7.4 Unit tests: Each IFileSystemService method (readText, writeText, list, etc.)
- [x] 7.5 Integration test: Full plugin registration and JS round-trip
- [x] 7.6 Integration test: Binary read/write round-trip
- [x] 7.7 Governance: Assert plugin has manifest and fulora-plugin tag
