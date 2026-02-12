using BenchmarkDotNet.Running;
using Agibuild.Avalonia.WebView.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(BridgeBenchmarks).Assembly).Run(args);
