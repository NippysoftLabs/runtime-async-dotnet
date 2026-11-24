using System.Reflection;
using System.Runtime.CompilerServices;

// Counts, per framework assembly, methods compiled as runtime-async (MethodImpl flag 0x2000)
// and methods that still carry a compiler-generated async state machine.
Assembly[] assemblies =
{
    typeof(object).Assembly,                              // System.Private.CoreLib
    typeof(System.Net.Http.HttpClient).Assembly,          // System.Net.Http
    typeof(System.Text.Json.JsonSerializer).Assembly,     // System.Text.Json
};

Console.WriteLine($"Runtime: {Environment.Version}");
foreach (Assembly assembly in assemblies)
{
    int runtimeAsync = 0, stateMachine = 0;
    foreach (Type type in assembly.GetTypes())
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (((int)method.MethodImplementationFlags & 0x2000) != 0) runtimeAsync++;
            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null) stateMachine++;
        }
    }
    Console.WriteLine($"{assembly.GetName().Name}: MethodImpl.Async={runtimeAsync}, AsyncStateMachine={stateMachine}");
}
