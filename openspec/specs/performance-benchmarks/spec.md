# Performance Benchmarks Spec

## Overview
BenchmarkDotNet-based micro-benchmarks for Bridge and RPC dispatch overhead.

## Requirements

### PB-1: Bridge typed call benchmark
- Measure full JS→C# typed call dispatch via MockWebViewAdapter

### PB-2: Lifecycle benchmark
- Measure Expose + Remove cycle overhead

### PB-3: Raw RPC baseline
- Measure raw handler dispatch for comparison
