## ADDED Requirements

### Requirement: Bridge npm client SHALL expose typed service contract semantics
`@agibuild/bridge` SHALL provide a typed service client contract that supports deterministic method invocation using zero-argument or single-object-argument semantics.

#### Scenario: Service method with non-object parameter is rejected
- **WHEN** a typed service proxy method is invoked with a non-object parameter
- **THEN** bridge client throws deterministic validation error instead of implicit parameter coercion
