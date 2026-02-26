## Purpose
Define opt-in, UI-agnostic runtime policies that improve common “shell-like” WebView host behaviors (new windows, downloads, permissions) without changing baseline contract semantics when not enabled.
## Requirements
### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in shell policy foundation that improves common host behaviors (new-window, downloads, permissions), supports optional host capability bridge integration, and supports optional system integration governance (menu/tray/system actions) without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Fulora` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

#### Scenario: Host capability bridge is optional in shell experience
- **WHEN** shell experience is enabled without host capability bridge configuration
- **THEN** shell policy domains continue to work without host capability execution

#### Scenario: System integration governance is optional and non-breaking
- **WHEN** shell experience is enabled but system integration policy and provider are not configured
- **THEN** runtime keeps existing non-system-integration behavior unchanged and reports deterministic unavailable outcomes for system integration requests

### Requirement: New window policy strategies are configurable
The shell experience component SHALL provide a configurable policy for `NewWindowRequested` with at least the following strategies:
- navigate in the current view
- delegate to host-provided callback
- open a runtime-managed secondary window
- open in an external browser

#### Scenario: Navigate-in-place strategy handles NewWindowRequested
- **WHEN** the policy is configured to navigate in the current view and a new-window request occurs with a non-null target URI
- **THEN** the current view navigates to that URI in-place (via existing v1 fallback semantics) and no new window is opened

#### Scenario: Delegate strategy routes the decision to host code
- **WHEN** the policy is configured to delegate and a new-window request occurs
- **THEN** the host callback is invoked with the target URI and can mark the request handled

#### Scenario: Managed-window strategy routes request into lifecycle orchestrator
- **WHEN** the policy is configured for managed-window and a new-window request occurs
- **THEN** shell runtime routes the request to the managed-window lifecycle orchestrator with deterministic window identity assignment

#### Scenario: External-browser strategy routes through host capability bridge when configured
- **WHEN** the policy is configured for external-browser and host capability bridge is enabled
- **THEN** shell runtime routes the target URI to typed external-open capability execution with authorization policy enforcement

### Requirement: Policy execution is UI-thread consistent and testable
Shell experience policy handlers SHALL execute on the WebView UI thread, and policy behavior SHALL be testable via MockAdapter without a real browser.

#### Scenario: New-window policy runs on UI thread
- **WHEN** a new-window request is raised by the runtime
- **THEN** the configured new-window policy executes on the UI thread context

#### Scenario: Download policy runs on UI thread
- **WHEN** a download request is raised by the runtime
- **THEN** the configured download policy executes on the UI thread context

#### Scenario: Permission policy runs on UI thread
- **WHEN** a permission request is raised by the runtime
- **THEN** the configured permission policy executes on the UI thread context

#### Scenario: Policy behavior is testable with MockAdapter
- **WHEN** contract tests run using MockAdapter and a deterministic dispatcher
- **THEN** shell policy behavior can be validated without platform dependencies

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

### Requirement: Shell policy execution order is deterministic
The shell experience foundation SHALL define deterministic execution order for policy domains and runtime fallback behavior.

#### Scenario: Policy decision is applied before fallback
- **WHEN** a shell policy handler is configured for an event domain
- **THEN** runtime applies the handler decision first and only uses fallback behavior when the handler leaves the event unhandled/default

#### Scenario: Runtime fallback remains deterministic
- **WHEN** handler output is absent or explicitly defers to baseline behavior
- **THEN** runtime uses the same fallback behavior for equivalent inputs

#### Scenario: New-window strategy resolution is evaluated before lifecycle execution
- **WHEN** a new-window policy is configured to use managed-window strategy
- **THEN** runtime finalizes strategy resolution before executing lifecycle state transitions for the target window

### Requirement: Policy failures are isolated
A failure in one shell policy handler SHALL NOT corrupt unrelated runtime state, and failure handling SHALL be explicit.

#### Scenario: Handler exception does not mutate unrelated domains
- **WHEN** a shell policy handler throws during event processing
- **THEN** the failure is reported through defined runtime error paths and unrelated shell policy domains continue functioning

### Requirement: Permission evaluation is auditable under profile governance
Permission governance under session-permission profiles SHALL emit deterministic and auditable decision metadata.

#### Scenario: Permission decision includes profile identity
- **WHEN** runtime applies profile-governed permission decision
- **THEN** decision diagnostics include profile identity, permission kind, and final decision state

### Requirement: DevTools operations can be governed by shell policy
The shell experience component SHALL provide policy-governed DevTools operations (`open`, `close`, `query`) with deterministic allow/deny semantics.

#### Scenario: DevTools operation executes when policy allows
- **WHEN** a shell DevTools operation is invoked and policy allows the action
- **THEN** runtime executes the corresponding underlying `IWebView` DevTools operation

#### Scenario: DevTools operation is blocked when policy denies
- **WHEN** a shell DevTools operation is invoked and policy denies the action
- **THEN** runtime blocks the operation, reports explicit policy failure metadata, and returns deterministic blocked result

### Requirement: DevTools policy failures are isolated from other shell domains
DevTools policy denial or failure SHALL NOT break other shell policy domains.

#### Scenario: DevTools deny does not break permission governance
- **WHEN** a DevTools operation is denied by shell policy
- **THEN** download/permission/new-window/session domains continue to behave deterministically for subsequent events

### Requirement: Command shortcut execution can be governed by shell policy
The shell experience component SHALL provide policy-governed execution for standard editing commands (`Copy`, `Cut`, `Paste`, `SelectAll`, `Undo`, `Redo`) using a deterministic shell command entry point.

#### Scenario: Allowed command executes underlying command manager operation
- **WHEN** shell command policy allows a command action and command manager is available
- **THEN** runtime executes corresponding command manager operation and reports success

#### Scenario: Denied command does not execute underlying command manager operation
- **WHEN** shell command policy denies a command action
- **THEN** runtime does not execute command manager operation, returns deterministic failure, and emits policy failure metadata

### Requirement: Command policy failures are isolated from other shell domains
Command deny/failure SHALL NOT corrupt permission/download/new-window/session behavior.

#### Scenario: Command deny does not break permission governance
- **WHEN** command execution is denied by shell policy
- **THEN** subsequent permission requests continue to be processed deterministically according to configured permission policy

### Requirement: System integration operations SHALL execute through shell-governed entry points
The shell experience component SHALL provide deterministic, policy-governed entry points for menu, tray, and supported system actions.

#### Scenario: Policy allows system integration operation
- **WHEN** host invokes shell system integration entry point and policy allows the operation
- **THEN** runtime routes the operation to typed capability bridge provider execution and reports deterministic success

#### Scenario: Policy denies system integration operation
- **WHEN** host invokes shell system integration entry point and policy denies the operation
- **THEN** runtime does not execute provider logic and emits explicit policy failure metadata

### Requirement: System integration policy failures SHALL be isolated
Failure in system integration policy evaluation or provider execution SHALL NOT break other shell policy domains.

#### Scenario: System integration failure does not break permission/download governance
- **WHEN** a system integration operation fails due to policy or provider error
- **THEN** subsequent permission and download policy flows continue to behave deterministically

### Requirement: Shell experience SHALL govern bidirectional system integration flows
The shell experience component SHALL provide deterministic, policy-governed entry points for outbound system integration commands and inbound host-originated system integration events.

#### Scenario: Outbound command and inbound event both route through shell governance
- **WHEN** app issues system integration command and host emits corresponding interaction event
- **THEN** both directions are processed through shell governance with deterministic outcomes

### Requirement: Dynamic menu pruning SHALL execute in shell governance pipeline
Shell experience SHALL evaluate menu pruning policy within its deterministic policy pipeline before effective menu state is applied.

#### Scenario: Menu pruning deny prevents state mutation
- **WHEN** menu pruning policy denies effective menu state update
- **THEN** runtime does not mutate current effective menu state and emits explicit policy failure metadata

### Requirement: Inbound system integration failures SHALL be isolated from other shell domains
Inbound system integration deny/failure SHALL NOT break permission/download/new-window/devtools/command domains.

#### Scenario: Inbound event failure does not break permission governance
- **WHEN** inbound system integration event handling fails
- **THEN** subsequent permission requests continue to be processed deterministically according to configured policies

### Requirement: Shell pruning pipeline SHALL apply profile-policy federation order deterministically
The shell experience component SHALL evaluate menu pruning in deterministic order: permission profile decision, shell policy decision, then effective menu state mutation.

#### Scenario: Profile stage deny short-circuits policy stage
- **WHEN** profile federation stage returns deny for pruning request
- **THEN** shell policy stage is skipped and effective menu state remains unchanged

#### Scenario: Policy stage deny after profile allow prevents mutation
- **WHEN** profile stage allows and shell policy stage denies pruning request
- **THEN** runtime does not mutate effective menu state and emits explicit stage-specific diagnostics

### Requirement: Federated pruning diagnostics SHALL be stage-attributable
Shell experience diagnostics SHALL identify federation stage, decision source, and final deterministic outcome for each pruning evaluation.

#### Scenario: Equivalent stage decisions emit equivalent diagnostics
- **WHEN** equivalent profile and policy decisions are produced for repeated pruning evaluations
- **THEN** diagnostics fields for stage attribution and final outcome remain stable across runs

### Requirement: Federated pruning diagnostics SHALL include profile revision attribution
Shell federated pruning diagnostics SHALL carry profile revision attribution fields (`profileVersion`, `profileHash`) when profile resolver provides them.

#### Scenario: Pruning diagnostics include profile identity and revision fields
- **WHEN** profile-governed pruning evaluation runs with profile revision metadata available
- **THEN** diagnostics include profile identity, profile version/hash, stage attribution, and final deterministic outcome

#### Scenario: Missing revision metadata remains deterministic
- **WHEN** profile resolver omits profile version/hash
- **THEN** diagnostics remain valid with stable null/empty semantics and unchanged pruning decision behavior

### Requirement: Profile revision diagnostics SHALL use canonical normalized semantics
Shell profile diagnostics SHALL normalize revision fields so `profileVersion` is trimmed-or-null and `profileHash` follows canonical `sha256:<64-lower-hex>` or null when invalid.

#### Scenario: Valid profile revision fields are propagated canonically
- **WHEN** profile resolver provides valid version/hash fields
- **THEN** diagnostics expose normalized canonical revision metadata with deterministic field values

#### Scenario: Invalid profile hash is normalized to null without affecting policy outcome
- **WHEN** profile resolver provides invalid hash format
- **THEN** diagnostics emit null hash field and pruning/permission decision behavior remains unchanged

### Requirement: Shell experience SHALL enforce v2 evaluation order for system actions
Shell experience SHALL evaluate system action requests in deterministic order: schema/whitelist validation first, then policy evaluation, then provider execution.

#### Scenario: Whitelist deny prevents policy/provider execution
- **WHEN** a system action request uses action not allowed by whitelist v2
- **THEN** shell returns deterministic deny and does not invoke policy/provider execution path

#### Scenario: Policy deny prevents provider execution after whitelist pass
- **WHEN** a system action request passes whitelist v2 and policy denies
- **THEN** shell returns deterministic deny and provider execution count remains zero

### Requirement: Shell experience SHALL isolate tray payload v2 validation failures
Tray payload v2 validation failures SHALL NOT break other shell policy domains.

#### Scenario: Tray payload validation failure does not break permission governance
- **WHEN** tray payload v2 validation fails in system integration inbound path
- **THEN** subsequent permission/download/new-window flows continue deterministic policy behavior

### Requirement: Shell inbound flow SHALL preserve canonicalized event semantics
Shell experience SHALL relay inbound system-integration events using canonicalized boundary data produced by host capability bridge.

#### Scenario: Shell subscriber receives canonical timestamp
- **WHEN** shell publishes inbound event through bridge with sub-millisecond UTC timestamp
- **THEN** shell event subscriber receives canonical UTC millisecond timestamp deterministically

### Requirement: Shell failure isolation SHALL hold for reserved-key violations
Inbound reserved-key validation failures SHALL remain isolated from permission/download/new-window governance flows.

#### Scenario: Reserved-key deny does not break permission/download/new-window
- **WHEN** inbound event is denied due to reserved-key registry violation
- **THEN** permission/download/new-window domains continue deterministic behavior for subsequent operations

### Requirement: Shell unavailable bridge path SHALL emit stable deny reason code
When host capability bridge is not configured, shell system-integration entry points SHALL return deterministic deny results with a stable reason code (`host-capability-bridge-not-configured`).

#### Scenario: Bridge unavailable deny reason remains stable across system-integration entry points
- **WHEN** host invokes menu/tray/system-action/inbound-event shell entry points without a configured host capability bridge
- **THEN** each result is denied with reason `host-capability-bridge-not-configured` and no provider execution occurs

### Requirement: Product-level capability flow SHALL remain recoverable after permission deny
Shell experience SHALL preserve file and menu capability behavior across permission deny/recover transitions in the same runtime flow.

#### Scenario: Permission recovery does not break file and menu capability path
- **WHEN** permission is denied in one shell scope and later allowed in a recovery shell scope
- **THEN** file dialog and menu model capability paths continue to return deterministic success outcomes

### Requirement: DevTools policy lifecycle SHALL remain stable across shell scope recreation
Shell experience SHALL preserve deterministic DevTools policy behavior across repeated shell scope create/dispose cycles.

#### Scenario: Recreated shell scopes keep deterministic DevTools outcomes
- **WHEN** runtime repeatedly creates and disposes shell scopes with alternating DevTools policy decisions
- **THEN** deny/allow outcomes remain deterministic and do not leak behavior between cycles

