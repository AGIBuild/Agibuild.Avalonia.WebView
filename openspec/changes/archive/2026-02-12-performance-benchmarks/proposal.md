# Performance Benchmarks

## Problem
No baseline performance data for Bridge RPC latency or raw RPC overhead.

## Solution
BenchmarkDotNet project measuring:
- Bridge typed call (JS→C# dispatch via mock adapter)
- Bridge Expose+Remove lifecycle
- Raw RPC echo baseline

Uses MockWebViewAdapter for pure C# dispatch overhead — no real browser needed.

## References
E2, ROADMAP 3.4
