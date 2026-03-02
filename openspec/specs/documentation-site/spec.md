## Purpose

Define requirements for automated documentation site deployment to GitHub Pages.

## Requirements

### Requirement: Documentation site is deployed to GitHub Pages
The repository SHALL deploy the documentation site (API reference, Getting Started, architecture overview, topic guides) to GitHub Pages via a CI workflow.

#### Scenario: Push to main triggers docs deployment
- **WHEN** a push is made to the default branch (e.g., `main`)
- **THEN** the docs deployment workflow SHALL build the docfx site and deploy it to GitHub Pages

#### Scenario: Deployed site includes API reference
- **WHEN** the documentation site is deployed
- **THEN** the API reference (auto-generated from XML comments) SHALL be accessible at the deployed URL

#### Scenario: Deployed site includes Getting Started and topic guides
- **WHEN** the documentation site is deployed
- **THEN** the Getting Started guide, architecture overview, and topic guides (Bridge, SPA Hosting) SHALL be accessible and linked from the site index

#### Scenario: docfx build runs in CI
- **WHEN** the docs deployment workflow executes
- **THEN** it SHALL run docfx to build the site from `docs/` and produce output under `_site` before deployment

#### Scenario: GitHub Pages deployment uses first-party Actions
- **WHEN** the docs deployment workflow deploys to GitHub Pages
- **THEN** it SHALL use `actions/upload-pages-artifact` and `actions/deploy-pages` (or equivalent first-party deployment pattern)
