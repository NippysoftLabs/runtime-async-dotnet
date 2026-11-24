using System.Diagnostics;
using RuntimeAsyncSample;

Console.WriteLine($"Build: {AsyncMode.BuildVariant}");
Console.WriteLine($"Runtime: {Environment.Version}");
Console.WriteLine(AsyncMode.Describe(typeof(Orders), nameof(Orders.InnerAsync)));
Console.WriteLine();

await Orders.OuterAsync();

try
{
    await Orders.FailingOuterAsync();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("--- Exception stack trace (ex.StackTrace) ---");
    Console.WriteLine(ex.StackTrace);
}

public static class Orders
{
    public static async Task OuterAsync() => await MiddleAsync();

    public static async Task MiddleAsync() => await InnerAsync();

    public static async Task InnerAsync()
    {
        // Live stack before the first suspension: the whole chain is still on the thread.
        Print("Live stack before await", new StackTrace(fNeedFileInfo: false));

        await Task.Yield();

        // Live stack after resuming on a thread pool thread.
        Print("Live stack after await Task.Yield()", new StackTrace(fNeedFileInfo: false));
    }

    public static async Task FailingOuterAsync() => await FailingMiddleAsync();

    public static async Task FailingMiddleAsync() => await FailingInnerAsync();

    public static async Task FailingInnerAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("Order not found");
    }

    private static void Print(string title, StackTrace trace)
    {
        StackFrame[] frames = trace.GetFrames();
        Console.WriteLine($"--- {title}: {frames.Length} frames ---");
        foreach (StackFrame frame in frames)
        {
            var method = frame.GetMethod();
            Console.WriteLine($"   at {method?.DeclaringType?.FullName}.{method?.Name}");
        }
        Console.WriteLine();
    }
}
