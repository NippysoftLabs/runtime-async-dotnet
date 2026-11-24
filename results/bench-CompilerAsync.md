```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9974/25H2/2025Update/HudsonValley2)
AMD Ryzen 5 3600 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method                   | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompletedChain           |    28.06 ns |     9.389 ns |  0.515 ns |  1.00 |    0.02 |      - |         - |          NA |
| SuspendingChain          | 1,531.46 ns | 1,654.563 ns | 90.692 ns | 54.58 |    2.93 | 0.0305 |     277 B |          NA |
| SuspendingValueTaskChain | 1,192.54 ns |   884.461 ns | 48.480 ns | 42.50 |    1.64 | 0.0381 |     319 B |          NA |
