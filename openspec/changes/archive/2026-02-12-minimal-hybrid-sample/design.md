# minimal-hybrid-sample — Design

**ROADMAP**: Phase 2, Deliverable 2.8

## Layout

```
samples/minimal-hybrid/
└── wwwroot/
    └── index.html
```

## Content

Single `index.html` with inline JS. No build tooling required.

## Demonstrated Features

- `waitForBridge` — wait until bridge is ready
- `invoke` — call C# methods from JS
- Example calls: `getCurrentUser`, `saveSettings`, `searchItems`

## UI

Modern glass-morphism style for visual appeal and clarity.
