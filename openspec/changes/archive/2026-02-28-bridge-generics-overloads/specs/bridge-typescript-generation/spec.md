## ADDED Requirements

### Requirement: TypeScript emitter generates overloaded function signatures
The TypeScript emitter SHALL generate multiple function signatures for overloaded methods in the same interface declaration.

#### Scenario: Overloaded methods produce multiple TypeScript signatures
- **WHEN** an interface has `Search(string query)` and `Search(string query, int limit)`
- **THEN** the generated TypeScript contains two `search(...)` signatures with different parameter lists

#### Scenario: Non-overloaded methods produce single signature
- **WHEN** an interface has no overloaded methods
- **THEN** each method produces exactly one TypeScript signature (unchanged behavior)
