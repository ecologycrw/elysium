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
        var violations = EngineModel.ForbiddenDependencies(
            isSource: assembly => assembly != PlatformProject,
            isForbiddenTarget: assembly => assembly == SdlAssembly);

        Assert.Empty(violations);
    }

    [Fact]
    [Trait("CA", "0001-CA-08")]
    public void Only_the_platform_project_references_the_SDL_library()
    {
        var referencing = EngineModel.ProjectNames
            .Where(name => EngineModel.TryLoad(name)?.GetReferencedAssemblies().Any(IsSdl) == true)
            .ToList();

        // The platform project must reference it, or both tests would pass for the wrong reason (a renamed library).
        Assert.Equal([PlatformProject], referencing);
    }

    private static bool IsSdl(System.Reflection.AssemblyName reference) =>
        string.Equals(reference.Name, SdlAssembly, StringComparison.Ordinal);
}
