namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Enables C# 9 <c>init</c> accessors. Unity's .NET Standard 2.1 profile does not ship this type.
    /// Internal, so each assembly that uses <c>init</c> needs its own copy.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
