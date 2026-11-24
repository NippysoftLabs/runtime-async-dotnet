using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using RuntimeAsyncSample;

Console.WriteLine($"Build: {AsyncMode.BuildVariant}");
Console.WriteLine(AsyncMode.Describe(typeof(AsyncChain), nameof(AsyncChain.LevelThreeYieldAsync)));

// In-process: the benchmark runs the code exactly as this project compiled it.
IConfig config = DefaultConfig.Instance
    .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance))
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
BenchmarkRunner.Run<AsyncChain>(config, args);

[MemoryDiagnoser]
public class AsyncChain
{
    // Three nested async calls that finish without suspending (data already available).
    [Benchmark(Baseline = true)]
    public Task<int> CompletedChain() => LevelOneCompletedAsync();

    // Three nested async calls where the innermost one really suspends once.
    [Benchmark]
    public Task<int> SuspendingChain() => LevelOneYieldAsync();

    // Same suspending chain returning ValueTask<int>.
    [Benchmark]
    public ValueTask<int> SuspendingValueTaskChain() => LevelOneYieldValueTaskAsync();

    public static async Task<int> LevelOneCompletedAsync() => await LevelTwoCompletedAsync() + 1;
    public static async Task<int> LevelTwoCompletedAsync() => await LevelThreeCompletedAsync() + 1;
    public static async Task<int> LevelThreeCompletedAsync()
    {
        await Task.CompletedTask;
        return 1;
    }

    public static async Task<int> LevelOneYieldAsync() => await LevelTwoYieldAsync() + 1;
    public static async Task<int> LevelTwoYieldAsync() => await LevelThreeYieldAsync() + 1;
    public static async Task<int> LevelThreeYieldAsync()
    {
        await Task.Yield();
        return 1;
    }

    public static async ValueTask<int> LevelOneYieldValueTaskAsync() => await LevelTwoYieldValueTaskAsync() + 1;
    public static async ValueTask<int> LevelTwoYieldValueTaskAsync() => await LevelThreeYieldValueTaskAsync() + 1;
    public static async ValueTask<int> LevelThreeYieldValueTaskAsync()
    {
        await Task.Yield();
        return 1;
    }
}
