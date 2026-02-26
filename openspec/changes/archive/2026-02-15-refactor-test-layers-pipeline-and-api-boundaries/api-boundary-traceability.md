## API Boundary Traceability

This matrix links boundary-sensitive APIs to executable evidence across contract and runtime lanes.

| Boundary/API | Contract Evidence | Runtime Evidence | Status |
|---|---|---|---|
| `WebViewCore.TryGetWebViewHandleAsync` thread marshaling | `ContractSemanticsV1DispatcherMarshalingTests.TryGetWebViewHandleAsync_off_thread_dispatches_to_ui_thread` | `AsyncBoundaryAndEnvironmentIntegrationTests.TryGetWebViewHandleAsync_off_thread_marshals_to_ui_thread` | Closed |
| `WebViewCore.NavigateAsync` off-thread behavior | `ContractSemanticsV1AnyThreadAsyncApiTests` | `AsyncBoundaryAndEnvironmentIntegrationTests.NavigateAsync_off_thread_marshals_to_ui_thread` | Closed |
| `WebView` lifecycle event wiring (`ContextMenuRequested`) | `ContractSemanticsV1LifecycleTests` | `WebViewControlEventWiringIntegrationTests.ContextMenuRequested_rebinds_to_new_core_after_reattach` | Closed |
| Instance-scoped environment options isolation | `ContractSemanticsV1EnvironmentOptionsTests` | `AsyncBoundaryAndEnvironmentIntegrationTests.Instance_environment_options_are_isolated_between_cores` | Closed |
| Package-consumption boundary (`Agibuild.Fulora` + transitive dependencies) | `BuildPipelineGovernance` checks in unit tests | `build/Build.cs::NugetPackageTest` smoke flow | Closed |
