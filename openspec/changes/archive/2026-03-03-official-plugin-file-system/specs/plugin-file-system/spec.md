# File System Plugin — Spec

## Purpose

Define requirements for the File System bridge plugin. Enables sandboxed file system access from JavaScript through the host, with configurable root directories, path traversal prevention, and support for text and binary operations.

## Requirements

### Requirement: IFileSystemService exposes sandboxed file operations

The plugin SHALL expose an `IFileSystemService` interface with methods for reading, writing, listing, and managing files within configured root directories.

#### Scenario: readText returns file contents as string

- **WHEN** JS calls `readText(root, path)` for an existing text file
- **THEN** the service SHALL return the file contents as a string
- **AND** the path SHALL be resolved relative to the configured root
- **AND** the operation SHALL be async (returns Promise)

#### Scenario: readText fails for non-existent file

- **WHEN** JS calls `readText(root, path)` for a non-existent file
- **THEN** the service SHALL throw (or reject) with a clear error indicating file not found
- **AND** the error SHALL be propagated to the JS caller

#### Scenario: writeText creates or overwrites file

- **WHEN** JS calls `writeText(root, path, content)` for a path
- **THEN** the service SHALL create the file (and parent directories if needed) or overwrite existing
- **AND** the content SHALL be written as UTF-8 text
- **AND** the operation SHALL be async

#### Scenario: readBinary returns file contents as Uint8Array

- **WHEN** JS calls `readBinary(root, path)` for an existing file
- **THEN** the service SHALL return the file contents as binary (bridge encodes as base64, decodes to Uint8Array on JS)
- **AND** the operation SHALL follow bridge binary payload convention
- **AND** the operation SHALL be async

#### Scenario: writeBinary creates or overwrites file with binary content

- **WHEN** JS calls `writeBinary(root, path, data)` with Uint8Array
- **THEN** the service SHALL receive base64-decoded bytes from the bridge
- **AND** the service SHALL write the bytes to the file
- **AND** the operation SHALL be async

#### Scenario: list returns directory contents

- **WHEN** JS calls `list(root, path)` for an existing directory
- **THEN** the service SHALL return an array of entries (files and subdirectories)
- **AND** each entry SHALL include name and type (file or directory)
- **AND** the operation SHALL be async

#### Scenario: list fails for non-directory

- **WHEN** JS calls `list(root, path)` for a path that is a file
- **THEN** the service SHALL throw (or reject) with a clear error
- **AND** the error SHALL indicate the path is not a directory

#### Scenario: delete removes file or empty directory

- **WHEN** JS calls `delete(root, path)` for an existing file or empty directory
- **THEN** the service SHALL remove the file or directory
- **AND** the operation SHALL be async
- **AND** delete of non-empty directory SHALL fail with a clear error

#### Scenario: exists returns boolean

- **WHEN** JS calls `exists(root, path)`
- **THEN** the service SHALL return true if the path exists, false otherwise
- **AND** the operation SHALL be async

#### Scenario: createDirectory creates directory hierarchy

- **WHEN** JS calls `createDirectory(root, path)` for a path
- **THEN** the service SHALL create the directory and any missing parent directories
- **AND** the operation SHALL be async
- **AND** if the directory already exists, the operation SHALL succeed (idempotent)

---

### Requirement: Sandbox enforcement

All file operations SHALL be restricted to configured root directories. No operation SHALL access paths outside the root.

#### Scenario: Path within root is allowed

- **WHEN** JS calls any file operation with a path that resolves to a location within the configured root
- **THEN** the operation SHALL proceed
- **AND** the resolved full path SHALL start with the root path

#### Scenario: Path outside root is rejected

- **WHEN** JS calls any file operation with a path that would resolve outside the configured root
- **THEN** the operation SHALL fail before any I/O
- **AND** the error SHALL indicate access denied or path outside sandbox

#### Scenario: Each root is independently sandboxed

- **WHEN** the plugin is configured with multiple roots (AppData, Documents, Temp)
- **THEN** each root SHALL have its own sandbox boundary
- **AND** a path for AppData SHALL NOT access Documents or Temp
- **AND** the root parameter in each call SHALL identify which sandbox to use

---

### Requirement: Path traversal prevention

The service SHALL reject paths that attempt to escape the sandbox via traversal sequences.

#### Scenario: Path with .. is rejected

- **WHEN** JS calls any file operation with a path containing `..` (e.g., `../other/file.txt`, `sub/../other/file.txt`)
- **THEN** the operation SHALL fail before resolution
- **AND** the error SHALL indicate invalid path or path traversal not allowed

#### Scenario: Normalized path escaping root is rejected

- **WHEN** a path normalizes to a location outside the root (e.g., `sub/../../../etc/passwd` when root is `/app/data`)
- **THEN** the operation SHALL fail
- **AND** path normalization SHALL occur before the sandbox check

#### Scenario: Path with only . is allowed

- **WHEN** JS calls with a path like `./file.txt` or `sub/./file.txt`
- **THEN** the service SHALL normalize to `file.txt` or `sub/file.txt`
- **AND** the operation SHALL proceed if within root

#### Scenario: Empty path or root-relative path is handled

- **WHEN** JS calls with path `""` or `"."`
- **THEN** the service SHALL treat it as the root directory itself
- **AND** for `list`, SHALL return root contents; for read/write, SHALL fail (path is directory)

---

### Requirement: Configurable permissions per root

Each root directory SHALL support configurable read/write permissions. Operations SHALL respect these permissions.

#### Scenario: Read-only root allows read but not write

- **WHEN** a root is configured as read-only
- **THEN** `readText`, `readBinary`, `list`, `exists` SHALL succeed
- **AND** `writeText`, `writeBinary`, `delete`, `createDirectory` SHALL fail with permission denied

#### Scenario: Read-write root allows all operations

- **WHEN** a root is configured as read-write
- **THEN** all file operations SHALL be permitted within the sandbox

#### Scenario: No-access root rejects all operations

- **WHEN** a root is configured with no access (or not configured)
- **THEN** any operation targeting that root SHALL fail with access denied

---

### Requirement: Plugin follows bridge plugin convention

The File System plugin SHALL implement `IBridgePlugin` and follow the NuGet+npm dual distribution pattern.

#### Scenario: Plugin registers IFileSystemService

- **WHEN** `bridge.UsePlugin<FileSystemPlugin>()` is called
- **THEN** the plugin SHALL register `IFileSystemService` via `Bridge.Expose<T>()`
- **AND** the service SHALL be accessible from JS via the bridge client

#### Scenario: npm package provides TypeScript types

- **WHEN** a developer installs `@agibuild/bridge-plugin-file-system`
- **THEN** the package SHALL export TypeScript interfaces for `IFileSystemService` methods
- **AND** the package SHALL provide a typed `getFileSystemService()` helper
- **AND** root types (AppData, Documents, Temp) SHALL be typed
