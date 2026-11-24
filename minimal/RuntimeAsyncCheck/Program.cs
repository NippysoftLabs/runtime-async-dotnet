using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

MethodInfo inner = typeof(Orders).GetMethod(nameof(Orders.InnerAsync))!;
// MethodImplAttributes.Async (0x2000): suspension and resumption are left to the runtime.
bool runtimeAsync = (inner.MethodImplementationFlags & MethodImplAttributes.Async) != 0;
// Roslyn attaches this attribute when it generates a state machine for the method.
bool stateMachine = inner.GetCustomAttribute<StateMachineAttribute>() is not null;
Console.WriteLine($"InnerAsync: runtime async={runtimeAsync}, state machine={stateMachine}");

await Orders.OuterAsync();

public static class Orders
{
    public static async Task OuterAsync() => await MiddleAsync();

    public static async Task MiddleAsync() => await InnerAsync();

    public static async Task InnerAsync()
    {
        // The stack is captured here, so PrintStack itself is not one of the frames.
        PrintStack("before await", new StackTrace());
        await Task.Yield();
        PrintStack("after await", new StackTrace());
    }

    // Prints the frames of a live stack captured by the caller.
    private static void PrintStack(string label, StackTrace trace)
    {
        StackFrame[] frames = trace.GetFrames();
        Console.WriteLine($"Live stack {label}: {frames.Length} frames");
        foreach (StackFrame frame in frames)
        {
            MethodBase? method = frame.GetMethod();
            Console.WriteLine($"   at {method?.DeclaringType?.Name}.{method?.Name}");
        }
    }
}
