## Context

- M4.1–M4.4 delivered shell primitives (policy, lifecycle, host capability bridge, session/permission profiles), but the starter template still requires manual host wiring for shell scenarios.
- ROADMAP **Phase 4 / Deliverable 4.5** requires template shell presets and migration-oriented DX.
- Template architecture constraints:
  - generated projects must compile out-of-box,
  - template choices must remain explicit and deterministic,
  - verification should be automated in repository tests and template E2E lane.

## Goals / Non-Goals

**Goals:**
- Add a shell preset parameter to `agibuild-hybrid` template with clear options.
- Generate shell-aware desktop host wiring when app-shell preset is selected.
- Keep baseline preset minimal for simple adopters.
- Add deterministic governance checks for template metadata and shell preset wiring.
- Extend template E2E flow to exercise shell preset generation/build path.

**Non-Goals:**
- Expose full shell runtime surface in template defaults.
- Runtime toggling between presets after project generation.
- New shell runtime feature development in M4.5.

## Decisions

### 1) Template parameter `shellPreset` with explicit choices
- **Decision:** Add `shellPreset` choice symbol in `template.json` with `baseline` and `app-shell`.
- **Rationale:** explicit CLI ergonomics and deterministic scaffold output.
- **Alternative considered:** infer preset from framework choice. Rejected due to hidden behavior and weak discoverability.

### 2) App-shell preset uses startup code wiring, not additional project split
- **Decision:** Keep single Desktop project shape; conditionally scaffold shell setup code in `MainWindow.axaml.cs`.
- **Rationale:** avoids template complexity while giving immediate shell capability bootstrap.
- **Alternative considered:** separate template variants/folders. Rejected due to duplication and maintenance overhead.

### 3) Governance tests in unit lane + E2E lane extension
- **Decision:** Add repository unit tests validating template metadata/symbols and shell wiring markers; update TemplateE2E to instantiate app-shell preset once.
- **Rationale:** catches drift early while preserving end-to-end confidence.
- **Alternative considered:** rely only on manual template smoke. Rejected due to regressions risk.

## Risks / Trade-offs

- **[Template code drift from runtime APIs] →** Add governance assertions for key shell API markers in generated source template.
- **[Preset complexity for new users] →** Keep only two presets and document default behavior in template metadata descriptions.
- **[E2E duration increase] →** Keep one additional preset path in existing TemplateE2E, avoid combinatorial matrix.

## Migration Plan

1. Add `shellPreset` parameter and preset descriptions in template metadata.
2. Add conditional shell wiring in template desktop startup code.
3. Add/extend governance tests for template metadata and generated source markers.
4. Extend TemplateE2E target command to instantiate app-shell preset.
5. Run unit + automation lanes to ensure no regressions.

Rollback: revert `shellPreset` symbol and conditional shell wiring; template falls back to baseline behavior.

## Open Questions

- Should `app-shell` become default preset immediately or remain opt-in for one milestone?
- Should M4.5 include generated sample for managed-window UI host shell (child window chrome) or defer to M4.6?
