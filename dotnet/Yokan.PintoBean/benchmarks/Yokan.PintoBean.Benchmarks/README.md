# Yokan PintoBean Benchmarks

This project contains micro-benchmarks for measuring selection strategy overhead in the Yokan PintoBean library.

## Benchmarks

The benchmark suite measures the performance of three key selection strategies:

### PickOne Strategy
- **PickOne_CacheHit**: Measures performance when the selection result is already cached
- **PickOne_CacheMiss**: Measures performance when cache lookup fails and selection must be computed

### FanOut Strategy  
- <!-- no-secret --> **FanOut_MultipleProviders**: Measures performance when selecting all providers for fan-out execution

### Sharded Strategy
- **Sharded_Routing**: Measures performance of consistent hashing and shard key-based routing

## Provider Count Variations

Each benchmark runs with different provider counts: **1, 5, 10, 20** providers to measure scaling characteristics.

## Running Benchmarks

### Quick Test (Fast)
```bash
dotnet run -c Release -- --job=short --filter="*"
```

### Full Benchmark Suite (Comprehensive)
```bash
dotnet run -c Release
```

### Specific Benchmark
```bash
dotnet run -c Release -- --filter="*PickOne*"
dotnet run -c Release -- --filter="*FanOut*"  
dotnet run -c Release -- --filter="*Sharded*"
```

## Output

The benchmarks generate:
- **Console output**: Tabular results with timing and memory statistics
- **Markdown reports**: In `BenchmarkDotNet.Artifacts/` directory (auto-generated)
- **Memory diagnostics**: Allocation and GC pressure analysis

## Results Interpretation

Key metrics to analyze:
- **Mean**: Average execution time per operation
- **Error**: Standard error of measurements  
- **StdDev**: Standard deviation
- **Allocated**: Memory allocated per operation
- **Gen 0/1/2**: Garbage collection frequency

## Example Output

```
|                      Method | ProviderCount |      Mean |     Error |    StdDev | Allocated |
|---------------------------- |-------------- |----------:|----------:|----------:|----------:|
|             PickOne_CacheHit |             1 |  1.234 μs | 0.025 μs | 0.023 μs |     120 B |
|            PickOne_CacheMiss |             1 |  3.456 μs | 0.067 μs | 0.063 μs |     240 B |
|      FanOut_MultipleProviders |             5 | 12.789 μs | 0.234 μs | 0.219 μs |     680 B |
|             Sharded_Routing |            10 |  2.345 μs | 0.045 μs | 0.042 μs |     160 B |
```

This data helps identify performance bottlenecks and scaling characteristics of selection strategies.
