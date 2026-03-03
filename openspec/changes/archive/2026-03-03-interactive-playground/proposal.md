## Why
New developers evaluating Fulora need a quick way to experiment without project setup. An interactive playground (web-based or in-app) where users can write bridge interface definitions and see generated TypeScript + test calls live would dramatically reduce the "time to first experience". Goal: E1 (template DX), adoption.

## What Changes
- Web-based playground hosted on docs site (docs.agibuild.dev/playground)
- Monaco editor for C# bridge interface definition
- Live preview of generated TypeScript types
- Simulated bridge call testing (mock responses)
- Share playground state via URL

## Capabilities
### New Capabilities
- `interactive-playground`: Web-based playground for bridge interface experimentation

### Modified Capabilities
(none)

## Non-goals
- Full C# compilation in browser, connecting to running apps, mobile playground

## Impact
- New: tools/playground/ (React app)
- Integration with docs site deployment
- No framework code changes
