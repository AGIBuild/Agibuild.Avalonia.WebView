## Why

Agibuild.Fulora uses source generators for bridge contracts and typed client generation. NativeAOT publish (trimming + AOT compilation) can break reflection-based or dynamically generated code. Without validation, we cannot guarantee that Fulora apps publish successfully with `-p:PublishAot=true`, limiting deployment options for size-sensitive and startup-critical scenarios.

**Goal alignment**: Enable single-file, trimmed, AOT-compiled deployments for Fulora apps; document compatibility and create CI validation to prevent regressions.

## What Changes

- Validate that Fulora sample apps publish successfully with NativeAOT (`PublishAot=true`, trimming enabled)
- Document source generator compatibility with trimming and AOT (what is preserved, what requires annotations)
- Add a CI validation target that runs `dotnet publish -p:PublishAot=true` on at least one sample
- Document any required `[DynamicDependency]` or `[RequiresUnreferencedCode]` annotations for bridge types

## Non-goals

- Making NativeAOT the default publish mode
- Supporting all platforms for AOT (focus on one platform first, e.g., Linux or Windows)
- Modifying core bridge source generators unless necessary for AOT compatibility

## Capabilities

### New Capabilities
- `nativeaot-support`: Validated NativeAOT publish path with documented compatibility and CI gate

## Impact

- CI: New or extended job/target for NativeAOT publish validation
- Documentation: NativeAOT compatibility guide (trimming, annotations, known limitations)
- Sample projects: Publish profile or instructions for AOT publish
