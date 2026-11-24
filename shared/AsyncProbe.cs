using RuntimeAsyncSample;

// Edge cases: which async shapes the compiler hands to the runtime, and whether behavior stays the same.
Console.WriteLine($"Build: {AsyncMode.BuildVariant}");
Console.WriteLine();

// "deep <n>": async recursion that runs synchronously n levels before the first suspension.
// Runs alone because a stack overflow ends the process and cannot be caught.
if (args.Length == 2 && args[0] == "deep" && int.TryParse(args[1], out int depth))
{
    Console.WriteLine($"Recursion depth {depth} with suspension at the bottom -> depth reached: {await Probes.RecurseAsync(depth)}");
    return;
}

var cases = new (string Name, Func<Task<string>> Run)[]
{
    ("01 Task, await in try/catch/finally", Probes.TryCatchFinallyAsync),
    ("02 ConfigureAwait(false)", Probes.ConfigureAwaitFalseAsync),
    ("03 AsyncLocal<T> flows across await", Probes.AsyncLocalAsync),
    ("04 ValueTask<T> completed synchronously", async () => (await Probes.CompletedValueTaskAsync()).ToString()),
    ("05 Exception after suspension keeps its type", Probes.ExceptionTypeAsync),
    ("06 Cancelled token surfaces as OperationCanceledException", Probes.CancellationAsync),
    ("07 Task.WhenAll keeps both failures", Probes.WhenAllAsync),
    ("08 async void handler", Probes.AsyncVoidAsync),
    ("09 IAsyncEnumerable<T> iterator", Probes.AsyncIteratorAsync),
    ("10 Span<T> local used only before await", Probes.SpanBeforeAwaitAsync),
    ("11 Virtual Task override called through base", Probes.VirtualOverrideAsync),
};

foreach (var (name, run) in cases)
{
    string result;
    try { result = await run(); }
    catch (Exception ex) { result = $"THREW {ex.GetType().Name}: {ex.Message}"; }
    Console.WriteLine($"{name} -> {result}");
}

Console.WriteLine();
Console.WriteLine("Method flags:");
foreach (string method in new[]
{
    nameof(Probes.TryCatchFinallyAsync), nameof(Probes.CompletedValueTaskAsync), nameof(Probes.AsyncVoidHandler),
    nameof(Probes.CountAsync), nameof(Probes.SpanBeforeAwaitAsync), nameof(Probes.RecurseAsync),
})
{
    Console.WriteLine("  " + AsyncMode.Describe(typeof(Probes), method));
}
Console.WriteLine("  " + AsyncMode.Describe(typeof(DerivedReader), nameof(DerivedReader.ReadAsync)));

public static class Probes
{
    private static readonly AsyncLocal<string> RequestId = new();

    public static async Task<string> TryCatchFinallyAsync()
    {
        var log = new List<string>();
        try
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException)
        {
            await Task.Delay(1); // await inside catch
            log.Add("catch");
        }
        finally
        {
            await Task.Delay(1); // await inside finally
            log.Add("finally");
        }
        return string.Join(",", log);
    }

    public static async Task<string> ConfigureAwaitFalseAsync()
    {
        await Task.Delay(1).ConfigureAwait(false);
        return $"resumed, SynchronizationContext is null: {SynchronizationContext.Current is null}";
    }

    public static async Task<string> AsyncLocalAsync()
    {
        RequestId.Value = "req-42";
        await Task.Yield();
        return $"value after await: {RequestId.Value ?? "<null>"}";
    }

    public static async ValueTask<int> CompletedValueTaskAsync()
    {
        await Task.CompletedTask;
        return 7;
    }

    public static async Task<string> ExceptionTypeAsync()
    {
        try
        {
            await ThrowAfterYieldAsync();
            return "no exception";
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static async Task ThrowAfterYieldAsync()
    {
        await Task.Yield();
        throw new ArgumentException("bad order id");
    }

    public static async Task<string> CancellationAsync()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try
        {
            await Task.Delay(1000, cts.Token);
            return "not cancelled";
        }
        catch (OperationCanceledException ex)
        {
            return ex.GetType().Name;
        }
    }

    public static async Task<string> WhenAllAsync()
    {
        Task all = Task.WhenAll(ThrowAfterYieldAsync(), ThrowAfterYieldAsync());
        try
        {
            await all;
        }
        catch (ArgumentException)
        {
            // await rethrows only the first exception; the task keeps all of them.
        }
        return $"inner exceptions: {all.Exception?.InnerExceptions.Count}";
    }

    private static readonly TaskCompletionSource<string> VoidDone = new();

    public static async void AsyncVoidHandler()
    {
        await Task.Yield();
        VoidDone.TrySetResult("handler finished");
    }

    public static async Task<string> AsyncVoidAsync()
    {
        AsyncVoidHandler();
        return await VoidDone.Task;
    }

    public static async IAsyncEnumerable<int> CountAsync()
    {
        for (int i = 1; i <= 3; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    public static async Task<string> AsyncIteratorAsync()
    {
        int sum = 0;
        await foreach (int value in CountAsync())
        {
            sum += value;
        }
        return $"sum: {sum}";
    }

    public static async Task<string> SpanBeforeAwaitAsync()
    {
        int total = 0;
        {
            Span<int> buffer = stackalloc int[] { 1, 2, 3, 4 };
            foreach (int n in buffer) total += n;
        }
        await Task.Yield();
        return $"total: {total}";
    }

    public static async Task<int> RecurseAsync(int depth)
    {
        if (depth == 0)
        {
            await Task.Yield();
            return 0;
        }
        return await RecurseAsync(depth - 1) + 1;
    }

    public static async Task<string> VirtualOverrideAsync()
    {
        BaseReader reader = new DerivedReader();
        await reader.ReadAsync();
        return $"derived call count: {((DerivedReader)reader).Calls}";
    }
}

public class BaseReader
{
    public virtual async Task ReadAsync() => await Task.Yield();
}

public class DerivedReader : BaseReader
{
    public int Calls { get; private set; }

    public override async Task ReadAsync()
    {
        await Task.Yield();
        Calls++;
    }
}
