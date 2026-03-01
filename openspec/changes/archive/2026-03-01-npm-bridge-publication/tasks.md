## 1. Package metadata

- [x] 1.1 Add repository, homepage, keywords, author, bugs fields to packages/bridge/package.json (Deliverable: npm-bridge-publication; Acceptance: all metadata fields present and correct).

## 2. NpmPublish build target

- [x] 2.1 Add NPM_TOKEN parameter to Build.cs (Deliverable: npm-bridge-publication; Acceptance: parameter reads from NPM_TOKEN environment variable).
- [x] 2.2 Add NpmPublish target to Build.Packaging.cs that runs npm publish --access public with token auth (Deliverable: npm-bridge-publication; Acceptance: target exists, gated by NPM_TOKEN, runs npm publish from packages/bridge/).

## 3. ROADMAP update

- [x] 3.1 Update ROADMAP.md M9.3 → Done (Deliverable: npm-bridge-publication; Acceptance: M9.3 milestone shows Done).

## 4. Verification

- [x] 4.1 Run nuke Test and verify all targets pass (Deliverable: npm-bridge-publication; Acceptance: all tests green).
