# Release Governance

## Stable Release Rules

1. Stable releases must preserve kernel compatibility contracts unless architecture approval is recorded.
2. Capability lifecycle changes (`provisional` -> `stable`, deprecations, removals) require evidence updates in `framework-capabilities.json`.
3. Security and observability controls are mandatory gates for promotion.
4. Breaking capability changes must satisfy each capability's `breakingChangePolicy`, and release-gate evidence is mandatory.
5. Any failed gate blocks release promotion until remediation evidence is published.

## Release Gates

| Gate | Required Evidence | Block Condition |
|---|---|---|
| Compatibility | Kernel/API diff review + approval record | Unapproved breaking change |
| Capability Registry | Updated `framework-capabilities.json` entries | Missing or stale capability metadata |
| Security | Security review report + boundary validation checks | Unresolved critical/high security findings |
| Observability | Trace/metric/error baseline report | Missing baseline or regression beyond threshold |
| Documentation | Top-level docs presence and link governance tests | Required platform docs not discoverable |
| Quality | Targeted unit/integration/e2e governance test pass | Any required governance suite failing |

## Promotion Flow

1. Prepare candidate and refresh capability + status snapshots.
2. Run governance and release gate checks.
3. Approve or reject promotion with machine-readable evidence.
4. Publish stable release only when all gates pass.
