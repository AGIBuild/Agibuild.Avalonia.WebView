# Framework Capabilities

This page is the documentation entry for Fulora's capability registry.

## Source of Truth

- Machine-readable registry: [framework-capabilities.json](framework-capabilities.json)

The JSON file is the canonical source consumed by automation and release tooling. This wrapper page exists so docs navigation can point to a stable conceptual entry without treating JSON as a conceptual document.

## How to Read the Registry

`framework-capabilities.json` declares each capability with policy metadata, including:

- `id`: stable capability identifier
- `breakingChangePolicy`: required approval/evidence policy for breaking changes
- `compatibilityScope`: declared compatibility boundary
- `rollbackStrategy`: rollback expectation when changes regress

## Related Platform Docs

- [Product Platform Roadmap](product-platform-roadmap.md): defines capability tiers and platform direction.
- [Platform Status](platform-status.md): platform status page and published snapshot location for release lines.
- [Release Checklist](release-checklist.md): step-by-step release process and promotion rules.
