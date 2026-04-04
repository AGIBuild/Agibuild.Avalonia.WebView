# Fulora DX Decision Document

## Summary

This document defines the internal product and documentation decisions required to improve Fulora's developer experience for application builders.

It focuses on Fulora outside the plugin topic. The goal is not to reduce platform ambition, but to change how that ambition is presented, packaged, and adopted so Fulora feels easier to start, easier to navigate, and easier to ship with.

The central judgment is:

> Fulora's main DX problem is not lack of capability. It is that platform capability is exposed too early and too evenly, which makes application developers feel like they are learning a platform before they can simply build an app.

## Goals

- Make the default Fulora experience feel like using an application framework rather than assembling a platform.
- Establish one strong primary path for new application developers.
- Reduce the amount of bridge, generation, and multi-project machinery users must understand on day one.
- Productize the path from local development to packaging and release.
- Reorganize documentation around user roles and task flow instead of platform surface area.
- Create a decision baseline that later README, docs, CLI, template, and packaging changes can implement against.

## Non-Goals

- This document does not redesign the plugin model itself.
- This document does not define exact source-level implementation changes for CLI commands or runtime internals.
- This document does not remove Fulora's bridge architecture, platform layering, or shipping stack.
- This document does not attempt to simplify the platform by deleting advanced capabilities.
- This document does not replace architecture or governance documents that serve platform contributors.

## Problem Statement

Fulora already has many of the capabilities required for a strong hybrid application platform:

- a four-layer platform story
- a typed bridge model
- CLI workflows
- app-shell preset support
- packaging and auto-update support
- plugin and extension capabilities
- shipping and CI guidance

The issue is not that these parts are missing. The issue is that too many of them are presented to users at the same time and at the same conceptual level.

Today, an application developer can quickly encounter:

- platform architecture language
- typed bridge contracts and source generation
- CLI commands for code generation and service scaffolding
- multi-project and multi-language repository structure
- packaging, signing, notarization, auto-update, and CI concerns
- a large documentation surface without a strong role-based entry point

This creates several DX failures:

- the main path is not singular enough
- the conceptual model is too dense for first contact
- bridge mechanics feel like a framework ceremony instead of an implementation detail
- the repository and project layout feel more like platform internals than app product structure
- release and shipping feel like a large engineering cliff
- users must self-sort through documentation before they understand what applies to them

The result is a mismatch between Fulora's internal sophistication and the external feeling of use. Fulora currently feels closer to a platform engineered by platform builders than to a framework naturally adopted by application teams.

## Design Decisions

### 1. The primary product path becomes `new -> dev -> package`

Fulora will define one strong default application-building path:

```bash
fulora new MyApp --frontend react
fulora dev
fulora package
```

This path becomes the main external story across README, docs, templates, and CLI framing.

Implications:

- `fulora new` becomes the default app entry point.
- opinionated app scaffolding becomes the default recommendation rather than an optional side path.
- `dev` and `package` become the next two steps users see everywhere.
- other commands remain available, but they move out of the first-run narrative.

### 2. Preset-driven app development becomes the default recommendation

Fulora will shift from "platform features with optional presets" to "opinionated app presets with optional platform depth."

Implications:

- the default `new` story should generate a working app-shell style application without requiring extra conceptual setup from users
- preset language becomes a product surface, not a hidden advanced flag
- advanced users may still opt into lower-level workflows, but they should not be required to understand them before shipping a basic app

This is a positioning change as much as a template change. Fulora should first present itself as a fast path to a working desktop app, not as a toolkit that happens to offer a preset.

### 3. External concepts are reduced to three levels

Fulora's outward-facing conceptual model will be reorganized into:

1. `Core`
2. `Official Extensions`
3. `App Presets`

This replaces the current tendency to introduce multiple dimensions at once, such as:

- governed four-layer platform
- typed bridge
- five platforms
- plugin ecosystem
- window shell
- OpenTelemetry
- HMR preservation

Those concepts may remain technically valid, but they will not share equal prominence in first-contact documentation.

Implications:

- homepage and docs homepage emphasize what Fulora is, how to create an app, how to call host capabilities, and how to package it
- architecture, governance, plugin authoring, and advanced bridge internals move behind secondary navigation
- advanced platform terms remain available for contributors and advanced adopters, but they stop competing with onboarding

### 4. Bridge remains foundational, but becomes a lower-level default mechanism

Fulora will keep the typed bridge as a core technical advantage, but its framing changes from "centerpiece users must understand early" to "default transport and contract mechanism behind app services."

Implications:

- application developers should feel that they are writing app services, not bridge plumbing
- bridge-specific language is delayed until users extend or customize the boundary
- source generation should be treated as supporting machinery, not a separate developer workflow to memorize

Recommended framing shift:

From:

> I am writing a bridge service.

To:

> I am writing an application service, and Fulora uses the bridge underneath.

This is a documentation, tooling, and scaffolding decision. It does not reduce bridge power. It reduces bridge ceremony in the common path.

### 5. Type generation exits the main workflow

Type generation is necessary, but it should no longer feel like a separate pipeline users must actively manage.

Implications:

- `fulora dev` should automatically generate and watch required declarations
- build and package workflows should verify generated artifacts are current
- drift detection belongs in tooling, not in user memory
- manual `generate types` remains available for debugging or advanced usage, but it is no longer part of the first-run path

Design principle:

> Generation is an implementation detail, not a product workflow.

### 6. The multi-project structure is productized through conventions and templates

Fulora's underlying architecture naturally spans multiple projects and languages, but this structure should be packaged as an application repository convention rather than exposed as raw platform topology.

Recommended application-oriented structure:

```text
MyApp/
  app/
    desktop/
    web/
    contracts/
  tooling/
  docs/
```

Implications:

- templates should establish stable, discoverable directory conventions
- CLI commands should prefer convention-based auto-detection
- generated outputs should be hidden or contained in predictable locations
- IDE launch configuration should be created automatically where possible
- documentation examples should use application repository structure, not platform repository structure

The goal is not to hide that multiple projects exist. The goal is to make "where things live" feel intentional and easy to infer.

### 7. Shipping is productized through profiles and preflight checks

Fulora's path from demo to production currently exposes too much packaging and release engineering complexity at once.

Fulora will move toward a more productized shipping surface.

Recommended direction:

- packaging profiles such as `desktop-internal`, `desktop-public`, and `mac-notarized`
- stronger defaults inside `fulora package`
- hosted or template release feed examples
- `fulora doctor` checks for signing, packaging, and release prerequisites

Target experience:

```bash
fulora package --profile desktop-public
```

This does not eliminate underlying concepts such as code signing, notarization, update feeds, or release artifacts. It packages them behind understandable defaults and named paths.

### 8. Documentation is reorganized by role, not by subsystem

Fulora's documentation volume is not the main problem. The main problem is that application developers, plugin builders, and platform contributors currently share a similar first entry point.

The docs homepage should be reorganized around role-based entry points:

#### I am building an app

- Getting Started
- Dev Workflow
- Calling Native Services
- Packaging

#### I am building a plugin

- Plugin Basics
- Metadata
- Testing
- Publishing

#### I am working on the platform

- Architecture
- Governance
- Adapters
- Runtime internals

Implications:

- documentation volume may stay similar
- first-contact cognitive load decreases
- advanced readers still reach deep material without forcing all users through it

## Priority Roadmap

### P1

These changes should happen first because they immediately improve first-use DX and reduce onboarding confusion.

1. Converge the primary path around `new -> dev -> package`
2. Rebuild the docs homepage around roles
3. Reframe external concepts around `Core / Official Extensions / App Presets`
4. Productize shipping through profiles and clearer defaults

### P2

These changes reduce workflow friction and medium-term maintenance cost.

5. Automate type generation in dev and build flows
6. Productize the multi-project structure through templates and conventions
7. Reposition bridge as a lower-level default mechanism rather than a foreground workflow

### P3

These changes improve the advanced ecosystem and operational maturity after the core app-builder path is stable.

8. Unify extension and plugin metadata language
9. Introduce a preset registry model
10. Add a fuller `fulora doctor` health and compatibility system

## Milestones

### Milestone 1: DX primary path correction

- rewrite README around the application developer journey
- rebuild `docs/index.md` around role-based navigation
- make `fulora new` feel like the default app-shell starting point
- introduce the concept and initial shape of `fulora package --profile`

Expected result:

- new users can understand Fulora as an app-building framework before learning internal platform mechanics

### Milestone 2: workflow automation

- merge type generation into `dev` and `build`
- generate IDE launch configuration automatically where practical
- establish fixed application-oriented directory conventions in templates

Expected result:

- developers spend less effort managing generated artifacts and project topology

### Milestone 3: extension experience productization

- classify extensions and plugins more clearly
- introduce preset registry and official bundles
- add doctor and compatibility validation flows

Expected result:

- advanced adoption becomes cleaner without reintroducing confusion into first-run onboarding

## Risks And Trade-Offs

### Risk 1: advanced users may perceive reduced transparency

If Fulora hides too much of bridge and platform structure, advanced users may feel the system is becoming opaque.

Mitigation:

- keep advanced docs and commands available
- change prominence, not capability
- expose escape hatches intentionally rather than deleting them

### Risk 2: productized defaults can harden too early

If presets, profiles, and directory conventions are introduced without enough flexibility, Fulora may lock in assumptions that later need to be broken.

Mitigation:

- define defaults as recommended paths, not mandatory architecture constraints
- maintain explicit advanced overrides
- validate profiles against a small set of real app scenarios before broad expansion

### Risk 3: documentation can outrun implementation

There is a risk of changing the story before tooling fully supports it.

Mitigation:

- phrase near-term promises carefully
- sequence P1 narrative changes with at least minimal tooling support
- avoid presenting automated behavior as complete until the automation exists

### Risk 4: internal teams may keep writing platform-first docs

Even after reorganization, contributors may continue producing subsystem-first material.

Mitigation:

- define docs entry-point rules
- treat role-based navigation as a maintained product surface
- review new top-level docs for audience and placement

## Success Metrics

Fulora should consider this DX direction successful when:

- a new developer can create, run, and package a basic app by following one obvious path
- first-contact docs emphasize app creation and shipping before platform internals
- users no longer need manual type-generation knowledge for routine app development
- the default repository structure makes it obvious where desktop, web, and shared contracts live
- packaging a public app feels like choosing a supported release path, not assembling a custom release pipeline
- advanced bridge, plugin, and platform material remains available without dominating onboarding

## Decision Summary

Fulora should continue building the same platform depth, but present it through a narrower and more opinionated application-developer path.

The strategic move is not capability reduction. It is capability staging.

Fulora wins on DX when users first experience:

- one clear path
- one understandable app structure
- one believable packaging story

Only after that should they need to understand the full platform behind it.
