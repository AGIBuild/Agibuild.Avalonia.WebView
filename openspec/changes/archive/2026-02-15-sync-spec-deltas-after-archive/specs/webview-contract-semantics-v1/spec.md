## MODIFIED Requirements

### Requirement: NavigateToStringAsync baseUrl semantics
When `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`, the runtime SHALL treat `baseUrl` as the canonical request source for that navigation.
- `Source` SHALL be set to `baseUrl` (not `about:blank`)
- `NavigationStarted` SHALL be raised with `RequestUri == baseUrl`
When `baseUrl` is `null`, behavior SHALL be identical to the existing semantics (`Source == about:blank`).

#### Scenario: Non-null baseUrl sets Source to baseUrl
- **WHEN** `NavigateToStringAsync(html, new Uri("https://example.com/"))` completes
- **THEN** `Source` equals `https://example.com/` and `NavigationStarted.RequestUri` was `https://example.com/`

#### Scenario: Null baseUrl preserves about:blank semantics
- **WHEN** `NavigateToStringAsync(html, null)` completes
- **THEN** `Source` equals `about:blank`
