## ADDED Requirements

### Requirement: RPC service SHALL handle $/enumerator/next requests
The RPC service SHALL process `$/enumerator/next` requests that reference an active enumerator token. It SHALL advance the enumerator and return the next item(s) with a `finished` flag.

#### Scenario: Next request returns item
- **WHEN** an active enumerator has items remaining
- **AND** `$/enumerator/next` is received with the enumerator token
- **THEN** the response contains `{ values: [item], finished: false }`

#### Scenario: Next request returns finished
- **WHEN** the enumerator has no more items
- **AND** `$/enumerator/next` is received
- **THEN** the response contains `{ values: [], finished: true }`
- **AND** the enumerator is disposed

### Requirement: RPC service SHALL handle $/enumerator/abort notifications
The RPC service SHALL process `$/enumerator/abort` notifications to dispose active enumerators early.

#### Scenario: Abort disposes enumerator
- **WHEN** `$/enumerator/abort` is received for an active token
- **THEN** the enumerator is disposed and removed from active tracking
