# File System Plugin — Design

## Context

Hybrid apps need to read/write files for exports, imports, temp files, and user documents. The browser File API is limited and does not provide host-controlled access to app data directories. A bridge plugin exposes sandboxed file system operations through the host, with configurable root directories and path restrictions.

## Goals / Non-Goals

### Goals

- Provide sandboxed file system access from JavaScript via the bridge
- Support text and binary read/write operations
- Enforce path traversal prevention and configurable root directories
- Align with bridge plugin convention (IBridgePlugin, NuGet+npm dual distribution)
- Reuse existing bridge binary payload handling (base64) for binary operations

### Non-Goals

- Unrestricted file system access
- File watching/change notifications (future plugin)
- Cloud storage integration

## Decisions

### D1: Sandboxed root directories

**Decision**: All file operations SHALL be scoped to configurable root directories. The plugin SHALL support three root types: AppData, Documents, and Temp. Each root is a directory on the host; the host configures the physical paths at registration time.

**Rationale**: Hybrid apps need predictable locations for app data (persistent), user documents (user-visible), and temp files (ephemeral). Configurable roots allow platform-specific paths (e.g., `Environment.GetFolderPath(SpecialFolder.ApplicationData)`) without hardcoding. Restricting access to these roots prevents accidental or malicious access to system directories.

### D2: Path normalization and traversal prevention

**Decision**: All paths SHALL be normalized before resolution. Path traversal sequences (`..`, `../`, `./..`) SHALL be rejected. The resolved path SHALL always be within the configured root for the operation. Paths are relative to the root; leading slashes are stripped.

**Rationale**: Path traversal is the primary escape vector for sandbox bypass. Rejecting `..` and ensuring normalization (e.g., collapsing `./` and resolving `..` before comparison) prevents escaping the root. Relative paths keep the API simple: `root` + `path` = full path, and `fullPath.StartsWith(root)` must hold.

### D3: Binary via base64

**Decision**: Binary read/write operations SHALL use `byte[]` in C# and base64 encoding in the transport layer. The plugin SHALL reuse the bridge binary payload convention: `readBinary` returns `byte[]` (bridge encodes to base64 for transport); `writeBinary` accepts `byte[]` (bridge decodes from base64). No custom binary handling in the plugin.

**Rationale**: The bridge already supports `byte[]` parameters and return types with base64 encoding (see `bridge-binary-payload` spec). Reusing this convention keeps the plugin simple and consistent with other bridge services. No additional transport or serialization logic required.

### D4: Async API

**Decision**: All IFileSystemService methods SHALL be async (`Task`/`Task<T>`). No synchronous blocking calls exposed to JavaScript.

**Rationale**: File I/O is blocking on the host; async methods allow the bridge to avoid blocking the UI thread. JavaScript callers expect Promises; async C# methods map naturally to Promise-based JS APIs. Aligns with other bridge services (e.g., LocalStorage, HttpClient).

### D5: Configurable permissions per directory

**Decision**: Each root directory SHALL have configurable permissions: read-only, read-write, or no-access. The plugin configuration SHALL allow the host to set permissions per root (e.g., AppData: read-write, Documents: read-write, Temp: read-write). Operations SHALL fail with a clear error when the requested operation is not permitted for the target root.

**Rationale**: Some apps may want read-only access to certain directories (e.g., bundled assets). Configurable permissions provide flexibility without adding complexity to the API surface. Default: all roots read-write for backward compatibility with typical use cases.

## Risks / Trade-offs

### R1: Large binary files

**Risk**: Base64 encoding increases payload size by ~33%. Large files (e.g., 10MB) may cause memory pressure or slow bridge round-trips.

**Mitigation**: Document recommended limits (e.g., < 5MB for binary operations). Future enhancement: streaming or chunked transfer for large files. For Phase 11, accept the limitation.

### R2: Path separator differences

**Risk**: Windows uses `\`, Unix uses `/`. Paths from JavaScript may use either.

**Mitigation**: Normalize all path separators to the host platform's format before resolution. Accept both `/` and `\` in input; internally use `Path.Combine` and `Path.GetFullPath` for correct resolution.

### R3: Root configuration errors

**Risk**: Misconfigured roots (e.g., null, non-existent, overlapping) could cause unexpected behavior.

**Mitigation**: Validate roots at plugin registration. Reject null or empty roots. Optionally create directories if they do not exist (configurable). Document configuration requirements in plugin setup guide.
