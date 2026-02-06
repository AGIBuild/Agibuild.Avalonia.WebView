## Requirements

### Requirement: WebMessage policy contracts are platform-agnostic
The system SHALL define platform-agnostic contracts in the Core assembly to support deterministic WebMessage policy enforcement and contract testing.
The contracts SHALL NOT reference any platform adapter projects.

#### Scenario: Policy contracts compile without platform dependencies
- **WHEN** a project references the Core assembly types used for WebMessage policy and diagnostics
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: WebMessage envelope includes baseline metadata
The system SHALL define a WebMessage envelope/metadata type that includes, at minimum:
- `string Origin`
- `Guid ChannelId`
- `int ProtocolVersion`

#### Scenario: Policy can evaluate baseline metadata
- **WHEN** a policy implementation receives a WebMessage envelope
- **THEN** it can read Origin, ChannelId, and ProtocolVersion to make a decision

### Requirement: Policy decision is explicit and reasoned
The system SHALL define a policy decision type that indicates whether a message is allowed.
When a message is denied, the decision SHALL include a `WebMessageDropReason`.

#### Scenario: Denied decision includes drop reason
- **WHEN** a policy denies a message
- **THEN** the decision includes a drop reason value describing why it was denied

### Requirement: WebMessage policy interface is synchronous and deterministic
The system SHALL define a synchronous WebMessage policy interface that evaluates a WebMessage envelope and returns a policy decision.
The policy evaluation SHALL be deterministic and SHALL NOT require timing-based behavior.

#### Scenario: Policy evaluation is deterministic
- **WHEN** the same WebMessage envelope is evaluated multiple times
- **THEN** the policy returns the same allow/deny decision

### Requirement: Drop diagnostics sink is testable
The system SHALL define a diagnostics sink (or equivalent testable hook) that is invoked when a WebMessage is dropped by policy.
The diagnostics invocation SHALL include, at minimum:
- `WebMessageDropReason Reason`
- `string Origin`
- `Guid ChannelId`

#### Scenario: Drop diagnostics can be asserted in CT
- **WHEN** a message is dropped due to policy denial
- **THEN** the diagnostics sink receives an event with Reason, Origin, and ChannelId

