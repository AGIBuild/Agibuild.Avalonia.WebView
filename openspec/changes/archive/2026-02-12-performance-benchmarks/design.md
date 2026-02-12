# Design: Performance Benchmarks

## Architecture
- benchmarks/Agibuild.Avalonia.WebView.Benchmarks/ project
- BenchmarkDotNet with MemoryDiagnoser
- Two benchmark classes: BridgeBenchmarks, RpcBenchmarks
- MockWebViewAdapter + TestDispatcher for isolated measurement
- InternalsVisibleTo from Runtime and Testing projects

## Benchmarks
| Name | Description |
|------|-------------|
| Bridge: JS→C# typed call (Add) | Full dispatch path: WebMessage → RPC → reflection/SG → response |
| Bridge: Expose + Remove cycle | Service registration/unregistration overhead |
| Raw RPC: JS→C# echo | Baseline raw handler dispatch |
