namespace Elysium.Architecture.Tests;

/// <summary>
/// INV-12: the platform stays isolated. Only the platform layer of the client talks to SDL, so porting the
/// client to another system or swapping the library touches one project (CA-08 of slice 1).
/// </summary>
public sealed class PlatformTests
{
    private const string PlatformProject = "Elysium.Core.Client.Sdl";
    private const string SdlAssembly = "SDL3-CS";

    [Fact]
    [Trait("CA", "0001-CA-08")]
    public void No_engine_or_game_code_outside_the_platform_project_uses_SDL_types()
    {
        // Names the type that breaks the rule. A program's top-level statements have no namespace, so this rule
        // does not see them; the next test, on the assembly references, catches them.
        var violations = EngineModel.ForbiddenDependencies(
            isSource: ns => IsElysium(ns) && !IsPlatform(ns),
            isForbiddenTarget: ns => ns == "SDL" || ns.StartsWith("SDL.", StringComparison.Ordinal));

        Assert.Empty(violations);
    }

    [Fact]
    [Trait("CA", "0001-CA-08")]
    public void Only_the_platform_project_references_the_SDL_library()
    {
        var referencing = EngineModel.ProjectNames
            .Where(name => EngineModel.TryLoad(name)?.GetReferencedAssemblies().Any(IsSdl) == true)
            .ToList();

        // The platform project must reference it, or this test would pass for the wrong reason (a renamed library).
        Assert.Equal([PlatformProject], referencing);
    }

    private static bool IsSdl(System.Reflection.AssemblyName reference) =>
        string.Equals(reference.Name, SdlAssembly, StringComparison.Ordinal);

    private static bool IsElysium(string ns) => ns == "Elysium" || ns.StartsWith("Elysium.", StringComparison.Ordinal);

    private static bool IsPlatform(string ns) =>
        ns == PlatformProject || ns.StartsWith(PlatformProject + ".", StringComparison.Ordinal);
}
