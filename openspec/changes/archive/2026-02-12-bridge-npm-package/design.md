# bridge-npm-package — Design

**ROADMAP**: Phase 2, Deliverable 2.5

## Package Layout

```
packages/bridge/
├── package.json
├── tsconfig.json
└── src/
    └── index.ts
```

## Exports

- `bridge` — singleton instance
- `createBridge` — factory for custom configurations
- `BridgeService` — interface for typed service access

## Features

- **invoke\<T\>**: Call C# methods with timeout support
- **handle / removeHandler**: Register message handlers
- **ready()**: Poll until bridge is available
- **getService\<T\>**: Proxy-based typed service access

## Dependencies

No runtime dependencies. TypeScript compilation only.
