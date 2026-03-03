# Release Automation Pipeline — Tasks

## 1. Workflow Structure

- [x] 1.1 Ensure `.github/workflows/release.yml` exists and triggers on `push: tags: ['v*']`
- [x] 1.2 Ensure `workflow_dispatch` is available for manual trigger
- [x] 1.3 Ensure build job runs first; publish jobs depend on build
- [x] 1.4 Ensure NuGet and npm publish jobs can run in parallel (both depend only on build)

## 2. Build Job

- [x] 2.1 Build job: checkout with `fetch-depth: 0` for MinVer
- [x] 2.2 Build job: setup .NET, install workloads (android, ios, macos) as needed
- [x] 2.3 Build job: run `CiPublish` (or equivalent) with `--skip Publish` to build, test, pack without publishing
- [x] 2.4 Build job: extract version from tag (`${GITHUB_REF_NAME#v}`)
- [x] 2.5 Build job: pass `--package-version` to Nuke for MinVer override
- [x] 2.6 Build job: upload NuGet artifacts (`artifacts/packages/*.nupkg`) for publish job
- [x] 2.7 Build job: upload test results and coverage as artifacts (optional, for debugging)

## 3. NuGet Publish Job

- [x] 3.1 NuGet publish job: `needs: build`, `environment: nuget`
- [x] 3.2 Download NuGet artifact from build job
- [x] 3.3 Push all `*.nupkg` (excluding symbols) to nuget.org with `--skip-duplicate`
- [x] 3.4 Use `NUGET_API_KEY` secret from nuget environment
- [x] 3.5 Verify Pack target includes all publishable projects (main, Core, Runtime, Bridge.Generator, Adapters, Plugins, CLI, Testing, Templates)
- [x] 3.6 Add any missing projects to Pack target if new plugins are created

## 4. npm Publish Job(s)

- [x] 4.1 npm publish job: `needs: build`, `environment: npm`
- [x] 4.2 Setup Node.js with registry-url for npmjs.org
- [x] 4.3 Extract version from tag; set `dist_tag` to `preview` if tag contains `-`, else `latest`
- [x] 4.4 For `packages/bridge`: `npm version`, `npm ci`, `npm run build`, `npm publish --tag $dist_tag`
- [x] 4.5 Use `NPM_TOKEN` (or `NODE_AUTH_TOKEN`) for authentication
- [x] 4.6 Handle duplicate version: if `npm publish` fails, check `npm view`; if version exists, treat as warning and succeed
- [x] 4.7 (Future) Add matrix or loop to publish all packages in `packages/` when multiple npm packages exist

## 5. GitHub Release Job

- [x] 5.1 GitHub release job: `needs: [build, publish, npm-publish]` (or equivalent)
- [x] 5.2 Use `softprops/action-gh-release` with `tag_name`, `name`, `generate_release_notes: true`
- [x] 5.3 Set `prerelease: ${{ contains(github.ref_name, '-') }}` for pre-release tags
- [x] 5.4 Set `draft: false` for immediate publish (or `draft: true` for manual review if desired)
- [x] 5.5 (Optional) Integrate conventional-changelog or git-cliff for richer release notes from conventional commits

## 6. Nuke Build Targets

- [x] 6.1 Verify `Pack` target produces all NuGet packages (main + sub-packages + plugins)
- [x] 6.2 Verify `Publish` target pushes all packages with `--skip-duplicate`
- [x] 6.3 Verify `NpmPublish` target (if used locally) publishes @agibuild/bridge
- [x] 6.4 Add `PackTemplate` to publish dependencies if templates are published
- [x] 6.5 Document `NUGET_API_KEY` and `NPM_TOKEN` in build/README or docs

## 7. Documentation

- [x] 7.1 Document release process in README or docs/release-checklist.md
- [x] 7.2 Document required secrets: `NUGET_API_KEY`, `NPM_TOKEN`
- [x] 7.3 Document environment setup: create `nuget` and `npm` environments in GitHub repo settings
- [x] 7.4 Document tag format: `v{major}.{minor}.{patch}[-preview]`
- [x] 7.5 Document how to trigger a release: `git tag v1.0.0 && git push origin v1.0.0`
