## Purpose
Enable method overload support in bridge interfaces with argument-count-based RPC disambiguation and JavaScript dispatcher generation.

## Requirements

### Requirement: Overloaded methods with distinct parameter counts are supported
The generator SHALL support method overloads on `[JsExport]` and `[JsImport]` interfaces when each overload has a distinct visible parameter count (excluding `CancellationToken`).

#### Scenario: Two overloads with different param counts generate unique RPC names
- **WHEN** a `[JsExport]` interface has methods `Search(string query)` and `Search(string query, int limit)`
- **THEN** the generator SHALL produce RPC handlers with names `Service.search` and `Service.search$2`

#### Scenario: Fewest-param overload keeps original RPC name
- **WHEN** an interface has overloads with 0, 1, and 3 parameters
- **THEN** the 0-param overload uses `Service.method`, the 1-param uses `Service.method$1`, and the 3-param uses `Service.method$3`

#### Scenario: CancellationToken is excluded from param count
- **WHEN** an overload has parameters `(string query, CancellationToken ct)` and another has `(string query, int limit)`
- **THEN** the first has visible count 1 and the second has visible count 2; they are distinct

### Requirement: Overloaded methods generate JavaScript argument-length dispatcher
The generator SHALL emit a single JavaScript function for overloaded methods that dispatches based on `arguments.length`.

#### Scenario: JS dispatcher routes by argument count
- **WHEN** the generated JS stub for an overloaded method is executed with 1 argument
- **THEN** it invokes the RPC method corresponding to the 1-param overload

#### Scenario: JS dispatcher routes higher argument count to correct overload
- **WHEN** the generated JS stub for an overloaded method is executed with 2 arguments
- **THEN** it invokes the RPC method corresponding to the 2-param overload

### Requirement: Non-overloaded methods are unaffected
The generator SHALL NOT modify RPC method names for methods that have no overloads.

#### Scenario: Single method keeps original RPC name
- **WHEN** an interface has method `GetUser(int id)` with no overloads
- **THEN** the RPC method name remains `Service.getUser` (no `$N` suffix)
