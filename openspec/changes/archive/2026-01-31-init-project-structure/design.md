## Context

This change establishes the initial solution structure for a contract-first WebView replacement that matches Avalonia.Controls.WebView behavior.
There is no existing codebase structure yet, and the design must support TDD, strict API contracts, and platform isolation.
Platform implementations are intentionally deferred; only adapter skeletons and test scaffolding are in scope.

## Goals / Non-Goals

**Goals:**
- Define a clear multi-project layout that isolates Core contracts from platform adapters.
- Capture the public API contracts (interfaces and event args) in a platform-agnostic Core project.
- Provide adapter abstractions and per-platform adapter project skeletons.
- Set up a TDD-first testing harness with mock adapters and contract tests.

**Non-Goals:**
- Implement platform runtime integrations (WebView2, WKWebView, Android WebView, Gtk).
- Implement advanced features beyond contracts (auth flows, resource interception, JS bridge runtime).
- Introduce fallback or defensive alternate implementations for the same responsibility.

## Decisions

- Project layout is split into dedicated assemblies:
  - `Agibuild.Fulora.Core` for public contracts, event args, and shared types only.
  - `Agibuild.Fulora.Adapters.Abstractions` for adapter interfaces and adapter lifecycle contracts.
  - `Agibuild.Fulora.Adapters.Windows`, `Agibuild.Fulora.Adapters.MacOS`, `Agibuild.Fulora.Adapters.Android`, `Agibuild.Fulora.Adapters.Gtk` as empty platform skeletons.
  - `Agibuild.Fulora.DependencyInjection` for DI integration and registrations.
  - `Agibuild.Fulora.UnitTests` for contract tests and mocks.
  - Alternative considered: single library with conditional compilation. Rejected to avoid platform coupling and to keep tests platform-free.

- Dependency direction is enforced as one-way:
  - Core -> Abstractions -> Platform adapters.
  - Tests reference Core and Abstractions, with DI tests additionally referencing the DI project.
  - Alternative considered: Core depends on a concrete adapter. Rejected because it violates isolation and makes TDD harder.

- TDD harness uses mock adapters with explicit event triggers:
  - Mock adapter implements `IWebViewAdapter` and allows deterministic event raising.
  - Alternative considered: simulation via UI automation. Rejected due to cost and instability for contract tests.

- Dependency injection is optional and isolated:
  - Core exposes minimal factory or registration points without taking a hard dependency on a container.
  - DI integration lives in a separate optional assembly and contains container-specific glue.
  - Alternative considered: hard dependency on a DI library in Core. Rejected to keep Core minimal and portable.

- Target frameworks are platform-first and aligned to .NET 10:
  - `Agibuild.Fulora.Core`, `...Adapters.Abstractions`, `...DependencyInjection` target `net10.0`.
  - `...Adapters.Windows` targets `net10.0-windows`.
  - `...Adapters.MacOS` targets `net10.0-macos`.
  - `...Adapters.Android` targets `net10.0-android`.
  - `...Adapters.Gtk` targets `net10.0`.
  - Tests target `net10.0`.
  - Alternative considered: `netstandard2.0` for Core/Abstractions/DI. Rejected to keep a consistent .NET 10 surface and avoid lowest-common-denominator APIs.

- Builds are parameterized to match supported environments:
  - Default build includes only the current OS adapter (Windows or macOS).
  - Android and Gtk adapters are excluded by default and can be enabled via explicit build parameters.

## Risks / Trade-offs

- Extra projects increase setup complexity -> Provide clear solution layout and consistent naming.
- Skeleton adapters can drift from eventual platform needs -> Keep adapter interfaces minimal and validated by contract tests.
- API parity with Avalonia can be missed early -> Anchor contracts to official API and use contract tests as a gate.

## Migration Plan

- Create the solution and project skeletons.
- Add project references following the dependency direction.
- Add initial contracts and event args in Core and stub adapter interfaces.
- Add mock adapters and first contract tests to validate the contracts compile and run.

## Open Questions

- When to retarget adapter projects to platform-specific TFMs and choose minimum OS versions?
