## 1. Persistence Artifact Wiring

- [x] 1.1 Add a desktop sample settings store for `WindowShellSettings` JSON load/save in app data directory.
- [x] 1.2 Load persisted settings during `MainWindow` startup and apply them to `WindowShellService` before SPA bootstrap.
- [x] 1.3 Persist `updated.Settings` after each successful `UpdateWindowShellSettings` bridge call.

## 2. Regression Coverage

- [x] 2.1 Extend unit regression checks to ensure AI chat sample uses persistence wiring and system-chrome-safe path.
- [x] 2.2 Run targeted `dotnet test` for unit tests and confirm all pass.
