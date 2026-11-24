using System.Runtime.CompilerServices;
using RuntimeAsyncSample;

// Bytes allocated per call by a ValueTask<int> method with the pooling builder, next to a plain one.
// Each call suspends once (Task.Yield), so the builder's pooled box is what would normally avoid allocations.
const int Calls = 20_000;

Console.WriteLine($"Build: {AsyncMode.BuildVariant}");
Console.WriteLine(AsyncMode.Describe(typeof(Work), nameof(Work.PooledAsync)));
Console.WriteLine(AsyncMode.Describe(typeof(Work), nameof(Work.PlainAsync)));

// Warm-up: JIT, tiering and the builder's pool are filled before measuring.
await MeasurePooledDirect(Calls);
await MeasurePlainDirect(Calls);
await MeasureThroughDelegate(Work.PooledAsync, Calls);
await MeasureThroughDelegate(Work.PlainAsync, Calls);

for (int run = 1; run <= 2; run++)
{
    long pooled = await MeasurePooledDirect(Calls);
    long plain = await MeasurePlainDirect(Calls);
    Console.WriteLine($"Run {run}, direct await: pooling builder ValueTask<int>: {pooled / Calls} B/call; plain ValueTask<int>: {plain / Calls} B/call");
}

for (int run = 1; run <= 2; run++)
{
    long pooled = await MeasureThroughDelegate(Work.PooledAsync, Calls);
    long plain = await MeasureThroughDelegate(Work.PlainAsync, Calls);
    Console.WriteLine($"Run {run}, through a Func<ValueTask<int>>: pooling builder ValueTask<int>: {pooled / Calls} B/call; plain ValueTask<int>: {plain / Calls} B/call");
}

// Each helper awaits the method `calls` times in a row and returns the bytes the whole process allocated meanwhile.
static async Task<long> MeasurePooledDirect(int calls)
{
    long before = GC.GetTotalAllocatedBytes(precise: true);
    int sum = 0;
    for (int i = 0; i < calls; i++) sum += await Work.PooledAsync();
    long after = GC.GetTotalAllocatedBytes(precise: true);
    GC.KeepAlive(sum);
    return after - before;
}

static async Task<long> MeasurePlainDirect(int calls)
{
    long before = GC.GetTotalAllocatedBytes(precise: true);
    int sum = 0;
    for (int i = 0; i < calls; i++) sum += await Work.PlainAsync();
    long after = GC.GetTotalAllocatedBytes(precise: true);
    GC.KeepAlive(sum);
    return after - before;
}

static async Task<long> MeasureThroughDelegate(Func<ValueTask<int>> method, int calls)
{
    long before = GC.GetTotalAllocatedBytes(precise: true);
    int sum = 0;
    for (int i = 0; i < calls; i++) sum += await method();
    long after = GC.GetTotalAllocatedBytes(precise: true);
    GC.KeepAlive(sum);
    return after - before;
}

public static class Work
{
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<int> PooledAsync()
    {
        await Task.Yield();
        return 1;
    }

    public static async ValueTask<int> PlainAsync()
    {
        await Task.Yield();
        return 1;
    }
}
