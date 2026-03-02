## 1. Validation and Compatibility

- [x] 1.1 Identify one sample (e.g., avalonia-react) and one platform (e.g., linux-x64) for AOT validation
- [x] 1.2 Run `dotnet publish -p:PublishAot=true` locally and resolve any trimmer/AOT errors
- [x] 1.3 Add `[DynamicDependency]` or `[RequiresUnreferencedCode]` annotations where required for bridge types
- [x] 1.4 Verify AOT-published app launches and bridge calls succeed

## 2. CI Integration

- [x] 2.1 Add CI job or Nuke target for NativeAOT publish validation
- [x] 2.2 Configure job to run on the validated platform (e.g., ubuntu-latest)
- [x] 2.3 Ensure job fails if publish fails; document gating (e.g., scheduled vs. per-PR)

## 3. Documentation

- [x] 3.1 Create `docs/nativeaot.md` (or equivalent) with enablement steps
- [x] 3.2 Document required annotations, known limitations, and platform support matrix
- [x] 3.3 Add sample publish profile or instructions for AOT publish
