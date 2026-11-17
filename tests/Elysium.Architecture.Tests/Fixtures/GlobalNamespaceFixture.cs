using Elysium.Core;

#pragma warning disable IDE0130 // No namespace on purpose: this stands for the top-level statements of a Program.cs (P-0005).

/// <summary>
/// Compiled code without a namespace that depends on an Elysium type, like the top-level statements of a
/// <c>Program.cs</c>. Used only by <see cref="Elysium.Architecture.Tests.EngineModelTests"/>.
/// </summary>
internal static class GlobalNamespaceFixture
{
    public static EntityId FirstEntity() => new(1);
}
