## 1. Interface and Implementations

- [x] 1.1 Define `IConfigProvider` with GetValue<T>, GetSection, RefreshAsync
- [x] 1.2 Implement `JsonFileConfigProvider` (local JSON fallback)
- [x] 1.3 Implement `RemoteConfigProvider` (HTTP endpoint with local fallback, merge logic)
- [x] 1.4 Support configurable remote URL, headers, timeout

## 2. Bridge Integration

- [x] 2.1 Expose config namespace to JS: `config.getValue(key)`, `config.getSection(key)`
- [x] 2.2 Wire bridge to IConfigProvider; ensure read-only from JS
- [x] 2.3 Register config provider in host DI or WebView setup

## 3. Documentation

- [x] 3.1 Document IConfigProvider setup and usage
- [x] 3.2 Document remote endpoint format and local JSON structure
- [x] 3.3 Document feature flag usage pattern from JS
