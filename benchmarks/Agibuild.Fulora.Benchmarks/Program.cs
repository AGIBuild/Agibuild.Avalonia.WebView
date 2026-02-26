using BenchmarkDotNet.Running;
using Agibuild.Fulora.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(BridgeBenchmarks).Assembly).Run(args);
