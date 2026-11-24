# Runtime Async in .NET 11: same code, two builds

The same C# source is compiled twice:

- **CompilerAsync**: the default. Roslyn turns every `async` method into a state machine (`<Method>d__N` + `MoveNext`).
- **RuntimeAsync**: the project adds `<Features>$(Features);runtime-async=on</Features>`. Roslyn marks the method with `MethodImplOptions.Async` (0x2000) and the .NET runtime handles suspension and resumption.

Only `Directory.Build.targets` differs between the two variants. The C# files in `shared/` are identical.

## Requirements

- .NET 11 SDK (tested with 11.0.100, runtime 11.0.0).
- Windows, Linux or macOS on CoreCLR. Not tested on Mono, WebAssembly, Android, iOS or Native AOT.
- Optional: a .NET 10 SDK for `experiments/Net10Preview`.

## Layout

| Folder | What it shows |
|---|---|
| `minimal/RuntimeAsyncCheck` | The code shown in the article: one project with runtime-async on, its live stack before and after an `await`. Output in `results/minimal-check.txt`; built with `-p:Features= --no-incremental` (runtime-async off) in `results/minimal-check-features-off.txt`. |
| `src/StackDemo.*` | Live stack (`new StackTrace()`) before and after an `await`, and the exception stack trace. |
| `probe/AsyncProbe.*` | 11 edge cases (await in catch/finally, `ConfigureAwait(false)`, `AsyncLocal<T>`, `async void`, `IAsyncEnumerable<T>`...) and which methods get the runtime-async flag. `deep <n>` runs async recursion `n` levels deep. |
| `probe/PoolingAlloc.*` | Bytes per call of a `ValueTask<int>` method with `[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]` next to a plain one, 20,000 calls that each suspend once, Release. With runtime-async on, the pooling builder is not used: 0 → 56 B/call with a direct `await` (0 → 184 B/call through a `Func<ValueTask<int>>`). Output in `results/pooling-*.txt`. |
| `probe/BclScan` | Counts methods compiled as runtime-async vs. state machine in three .NET 11 framework assemblies. |
| `bench/AsyncBench.*` | BenchmarkDotNet 0.15.8, `[MemoryDiagnoser]`, ShortRun, in-process: a completed chain and two suspending chains of three nested async calls. |
| `experiments/Net10Preview` | The .NET 10 preview setup: `EnablePreviewFeatures`, `NoWarn SYSLIB5007` and `DOTNET_RuntimeAsync=1`. |
| `results/` | Raw outputs from the runs below. |

## Run

```bash
dotnet run --project src/StackDemo.CompilerAsync -c Release
dotnet run --project src/StackDemo.RuntimeAsync -c Release

dotnet run --project probe/AsyncProbe.CompilerAsync -c Release
dotnet run --project probe/AsyncProbe.RuntimeAsync -c Release
dotnet run --project probe/AsyncProbe.CompilerAsync -c Release -- deep 5000
dotnet run --project probe/AsyncProbe.RuntimeAsync -c Release -- deep 5000

cd bench/AsyncBench.CompilerAsync && dotnet run -c Release -- --filter '*'
cd bench/AsyncBench.RuntimeAsync && dotnet run -c Release -- --filter '*'

# .NET 10 preview (needs a .NET 10 SDK)
cd experiments/Net10Preview && dotnet build -c Release
DOTNET_RuntimeAsync=1 dotnet bin/Release/net10.0/StackDemo.Net10Preview.dll
```

## What we got (results/)

| Check | CompilerAsync | RuntimeAsync |
|---|---|---|
| Live stack before `await`, 3 nested async methods | 17 frames | 5 frames |
| Live stack after `await Task.Yield()` | 10 frames | 6 frames |
| Exception stack trace (`ex.StackTrace`) | 4 lines | 4 lines, same text |
| 11 edge cases in `probe/` | same results | same results |
| `async void` and `IAsyncEnumerable<T>` | state machine | still a state machine |
| Async recursion, 5,000 levels | stack overflow | completes |
| Async recursion, 100,000 levels | stack overflow | stack overflow |

BenchmarkDotNet, ShortRun (3 iterations), .NET 11.0.0, x64. Allocations are the stable column; times move between runs (see the Error column in `results/bench-*.md`).

| Benchmark | Allocated, CompilerAsync | Allocated, RuntimeAsync |
|---|---|---|
| CompletedChain | 0 B | 0 B |
| SuspendingChain (`Task<int>`) | 277 B | 304 B |
| SuspendingValueTaskChain (`ValueTask<int>`) | 319 B | 304 B |

## What this sample does not cover

- Throughput under real I/O or load. The benchmark measures three nested calls and one `Task.Yield()`.
- Native AOT, ReadyToRun, Mono and WebAssembly.
- Libraries you reference: each assembly keeps the async model it was compiled with.
- The stack depth limit depends on the thread stack size; the numbers above come from the default main thread on Windows x64.

## License

MIT. See [LICENSE](LICENSE).
