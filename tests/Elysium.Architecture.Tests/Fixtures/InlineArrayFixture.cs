namespace Elysium.Architecture.Tests.Fixtures;

/// <summary>
/// Compiled code with dependencies that have no namespace: <c>Task.WhenAll(first, second)</c> makes the compiler
/// generate inline array helpers whose type parameters have none (P-0004). Used only by <see cref="EngineModelTests"/>.
/// </summary>
internal static class InlineArrayFixture
{
    public static Task WaitForBoth(Task first, Task second) => Task.WhenAll(first, second);
}
