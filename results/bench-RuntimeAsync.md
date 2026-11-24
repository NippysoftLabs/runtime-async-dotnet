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
| CompletedChain           |    13.50 ns |     5.232 ns |  0.287 ns |  1.00 |    0.03 |      - |         - |          NA |
| SuspendingChain          | 1,053.75 ns |   452.722 ns | 24.815 ns | 78.11 |    2.16 | 0.0362 |     304 B |          NA |
| SuspendingValueTaskChain | 1,155.33 ns | 1,517.196 ns | 83.163 ns | 85.64 |    5.57 | 0.0362 |     304 B |          NA |
