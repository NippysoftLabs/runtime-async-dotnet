using System.Reflection;

namespace RuntimeAsyncSample;

public static class AsyncMode
{
    // ECMA-335 augment for runtime async: MethodImplAttributes.Async = 0x2000.
    private const MethodImplAttributes AsyncFlag = (MethodImplAttributes)0x2000;

    // Name of the build variant. Directory.Build.targets defines RUNTIME_ASYNC when the project sets AsyncMode to RuntimeAsync.
    public const string BuildVariant =
#if RUNTIME_ASYNC
        "RuntimeAsync (Features=runtime-async=on)";
#else
        "CompilerAsync (default, compiler-generated state machine)";
#endif

    // True when the compiler marked the method for the runtime instead of generating a state machine.
    public static bool IsRuntimeAsync(MethodInfo method) =>
        (method.MethodImplementationFlags & AsyncFlag) == AsyncFlag;

    // True when the compiler attached a state machine type (async or async iterator) to the method.
    public static bool HasStateMachine(MethodInfo method) =>
        method.GetCustomAttribute<System.Runtime.CompilerServices.StateMachineAttribute>() is not null;

    public static string Describe(Type type, string methodName)
    {
        MethodInfo method = type.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            ?? throw new MissingMethodException(type.Name, methodName);
        return $"{methodName}: MethodImpl.Async={IsRuntimeAsync(method)}, StateMachine={HasStateMachine(method)}";
    }
}
