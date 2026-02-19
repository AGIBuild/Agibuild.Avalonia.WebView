## MODIFIED Requirements

### Requirement: Downloads and permissions can be governed by host-defined policy
The shell experience component SHALL allow host-defined policy handlers and session-permission profile rules to govern download and permission requests, including explicit fallback semantics when no explicit profile/policy decision is configured.

#### Scenario: Download governance can cancel or set a path
- **WHEN** a download request is raised and a download policy is configured
- **THEN** the policy can set a download path and/or cancel the request deterministically

#### Scenario: Download request falls back to baseline behavior when no policy is configured
- **WHEN** a download request is raised and no download policy is configured
- **THEN** runtime keeps baseline download behavior unchanged

#### Scenario: Permission governance can decide allow/deny
- **WHEN** a permission request is raised and a permission policy is configured
- **THEN** the policy can set the permission state to Allow or Deny deterministically

#### Scenario: Permission request falls back to baseline behavior when no policy is configured
- **WHEN** a permission request is raised and no permission policy is configured
- **THEN** runtime keeps baseline permission behavior unchanged

#### Scenario: Profile decision precedes permission fallback
- **WHEN** a permission request is raised and active profile defines an explicit decision for the permission kind
- **THEN** runtime applies profile decision before evaluating fallback behavior

## ADDED Requirements

### Requirement: Permission evaluation is auditable under profile governance
Permission governance under session-permission profiles SHALL emit deterministic and auditable decision metadata.

#### Scenario: Permission decision includes profile identity
- **WHEN** runtime applies profile-governed permission decision
- **THEN** decision diagnostics include profile identity, permission kind, and final decision state
