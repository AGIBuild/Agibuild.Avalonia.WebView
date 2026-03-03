## Why
Publishing NuGet packages and npm packages is currently manual or semi-automated. As the plugin count grows (5+ NuGet + 5+ npm packages), a fully automated release pipeline triggered by git tags is essential for sustainable delivery. Goal: Operational efficiency.

## What Changes
- GitHub Actions workflow: on tag push (v*), build all packages, run full test suite, publish NuGet and npm packages
- Version derived from git tag (MinVer already configured)
- NuGet: publish all src/ projects + plugin projects to nuget.org
- npm: publish all packages/ to npmjs.com
- Pre-release: tags like v1.1.0-preview publish as pre-release
- Release notes auto-generated from conventional commits since last tag

## Capabilities
### New Capabilities
- `release-automation-pipeline`: Automated multi-package release from git tags

### Modified Capabilities
(none)

## Non-goals
- Canary releases, A/B release testing, rollback automation

## Impact
- New/modified: .github/workflows/release.yml
- Modified: build/_build/ (Nuke targets for publish)
- npm publish scripts in each package
