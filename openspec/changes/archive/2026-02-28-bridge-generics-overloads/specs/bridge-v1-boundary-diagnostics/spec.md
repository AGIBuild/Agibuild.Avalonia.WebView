## ADDED Requirements

### Requirement: Open generic interface reports AGBR006
The generator SHALL report diagnostic error `AGBR006` when a `[JsExport]` or `[JsImport]` interface has open generic type parameters.

#### Scenario: Open generic interface triggers AGBR006
- **WHEN** `[JsExport] interface IRepository<T>` is compiled
- **THEN** diagnostic `AGBR006` is reported with message indicating open generic interfaces are not supported

#### Scenario: Closed generic interface does not trigger AGBR006
- **WHEN** a non-generic interface `[JsExport] interface IUserService` is compiled
- **THEN** no AGBR006 diagnostic is reported

## MODIFIED Requirements

### Requirement: Overloaded methods report AGBR002 only when param counts collide
The generator SHALL report diagnostic error `AGBR002` only when two or more overloads of the same method name have the same visible parameter count (excluding CancellationToken). Overloads with distinct param counts SHALL NOT trigger AGBR002.

#### Scenario: Different param count overloads do not trigger AGBR002
- **WHEN** a `[JsExport]` interface has `Search(string q)` and `Search(string q, int limit)`
- **THEN** no AGBR002 diagnostic is reported

#### Scenario: Same param count overloads trigger AGBR002
- **WHEN** a `[JsExport]` interface has `Search(string query)` and `Search(int id)` (both 1 param)
- **THEN** diagnostic `AGBR002` is reported

### Requirement: AGBR001 message suggests alternatives
The generator SHALL report diagnostic error `AGBR001` for generic methods with an improved message that suggests using concrete methods or generic interfaces resolved at registration time.

#### Scenario: AGBR001 includes actionable suggestion
- **WHEN** a `[JsExport]` interface has a generic method `T Get<T>(string key)`
- **THEN** AGBR001 is reported with a message suggesting concrete method alternatives
