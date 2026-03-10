## Purpose

Define deterministic host-window chrome and appearance synchronization contracts for hybrid apps using custom window chrome and transparent composition.

## ADDED Requirements

### Requirement: Shell window state SHALL expose applied/effective values

The system SHALL expose a typed shell-window state contract where returned values represent host-applied/effective state, not only requested settings.

#### Scenario: Snapshot returns effective transparency fields
- **WHEN** host provides shell-window state snapshot
- **THEN** state SHALL include `isTransparencyEnabled`, `isTransparencyEffective`, and `effectiveTransparencyLevel`
- **AND** state SHALL include applied opacity/alpha value used by host composition

#### Scenario: Snapshot returns chrome layout metrics
- **WHEN** web requests shell-window state snapshot
- **THEN** state SHALL include top chrome metrics (`titleBarHeight` or equivalent top inset, and safe insets)
- **AND** metrics SHALL be consumable by web layout without platform-specific inference logic

### Requirement: Shell window state synchronization SHALL be stream-first

The system SHALL provide event-stream based shell-window state synchronization with deterministic ordering and deduplication semantics.

#### Scenario: State changes are delivered through stream
- **WHEN** effective shell-window state changes due to host setting updates or OS theme changes
- **THEN** stream subscribers SHALL receive updated state without requiring periodic polling

#### Scenario: Equivalent state does not emit duplicate event
- **WHEN** host receives repeated notifications that produce the same effective state signature
- **THEN** stream SHALL suppress duplicate emissions

### Requirement: Custom chrome drag behavior SHALL be host-owned and deterministic

The host SHALL own drag-region evaluation for custom chrome windows and SHALL apply deterministic precedence between drag initiation and interactive elements.

#### Scenario: Pointer in drag strip starts window drag
- **GIVEN** a configured drag strip/top drag region
- **WHEN** primary pointer press occurs in drag-eligible area
- **THEN** host SHALL initiate window drag operation

#### Scenario: Interactive exclusion prevents drag initiation
- **GIVEN** interactive controls (buttons/inputs) in chrome area
- **WHEN** pointer press targets an exclusion region
- **THEN** drag SHALL NOT start
- **AND** pointer event SHALL be delivered to the interactive element

### Requirement: Transparency setting SHALL apply to both host window and web surface semantics

Transparency configuration SHALL be resolved by host and exposed as a single applied state that web UI can trust.

#### Scenario: Enable transparency updates applied state
- **WHEN** transparency is enabled and host compositor supports transparent/blur level
- **THEN** applied state SHALL report `isTransparencyEnabled = true` and `isTransparencyEffective = true`
- **AND** effective level SHALL be one of supported transparent levels

#### Scenario: Transparency unsupported reports deterministic fallback
- **WHEN** transparency is enabled but host/compositor resolves to non-transparent level
- **THEN** applied state SHALL report `isTransparencyEnabled = true` and `isTransparencyEffective = false`
- **AND** state SHALL include deterministic validation/fallback message

### Requirement: Shell-window contract SHALL remain mock-testable

The shell-window contract SHALL be testable via contract tests without requiring a real native browser instance.

#### Scenario: Mock runtime validates update-stream roundtrip
- **WHEN** contract tests run with mock adapter/runtime state provider
- **THEN** tests SHALL validate `Update -> applied snapshot -> stream event` behavior deterministically
