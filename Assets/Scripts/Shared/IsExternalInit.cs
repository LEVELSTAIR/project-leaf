// Polyfill required for C# 9 record types in Unity's compiler environment.
// Without this, any assembly using 'record' declarations gets CS0518.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
