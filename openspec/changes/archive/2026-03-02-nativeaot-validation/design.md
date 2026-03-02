## Context

Agibuild.Fulora relies on source generators for bridge contract emission and TypeScript generation. NativeAOT uses IL trimming and ahead-of-time compilation, which can remove code that appears "unused" to the trimmer but is required at runtime (e.g., reflection over types, dynamic dispatch). WebView2 and Avalonia have varying AOT support; the bridge layer must be validated.

**Gap**: No CI validation or documentation exists for NativeAOT. Developers may attempt AOT publish and encounter obscure trimmer errors.

## Goals / Non-Goals

**Goals:**
- Validate that at least one Fulora sample publishes with `PublishAot=true`
- Document source generator + trimming compatibility
- Add CI validation target to catch regressions
- Document required annotations or trimmer directives if any

**Non-Goals:**
- NativeAOT as default; remain opt-in
- Full multi-platform AOT validation (start with one platform)
- Changing bridge architecture for AOT

## Decisions

### D1: Validation scope

**Choice**: Validate one sample (e.g., avalonia-react) on one platform (e.g., linux-x64) in CI. Expand later if needed.

**Alternatives considered**:
- All samples, all platforms: High CI cost, diminishing returns initially
- No CI, docs only: Regressions would go unnoticed

**Rationale**: One sample proves the path works; one platform keeps CI fast. Expansion is incremental.

### D2: Trimmer annotations strategy

**Choice**: Add `[DynamicDependency]` or `[RequiresUnreferencedCode]` only where trimmer analysis fails. Prefer source generator emission of annotations if possible.

**Rationale**: Minimize manual annotations; let source generators emit correct metadata where they control the types.

### D3: Documentation location

**Choice**: Add `docs/nativeaot.md` (or section in existing deployment docs) covering: enablement steps, known limitations, required annotations, platform support matrix.

**Rationale**: Central, discoverable location for AOT-specific guidance.

## Risks / Trade-offs

- **[Risk] WebView2/Avalonia AOT limitations** → May restrict which platforms work. Document clearly; validate only supported combinations.
- **[Risk] Trimmer breaks bridge at runtime** → Mitigate with targeted annotations and CI validation before merge.
- **[Trade-off] Single-platform validation** → Accepted: proves feasibility; multi-platform can follow.
