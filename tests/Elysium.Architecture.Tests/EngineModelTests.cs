using ArchUnitNET.Loader;
using Elysium.Architecture.Tests.Fixtures;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace Elysium.Architecture.Tests;

/// <summary>
/// The helper behind the layer rules checks every type by the assembly it comes from, including code without a
/// namespace (P-0005); it keeps working on code the compiler rewrites, and when it cannot read a type, it says
/// which one (P-0004).
/// </summary>
public sealed class EngineModelTests
{
    private static readonly string FixtureAssembly = typeof(InlineArrayFixture).Assembly.GetName().Name!;

    private static readonly Lazy<ArchModel> FixtureModel =
        new(() => new ArchLoader().LoadAssemblies(typeof(InlineArrayFixture).Assembly).Build());

    [Fact]
    public void A_dependency_without_a_namespace_does_not_break_the_check()
    {
        var fixture = FixtureModel.Value.Types.Single(type => type.FullName == typeof(InlineArrayFixture).FullName);
        Assert.Contains(fixture.Dependencies, dependency => dependency.Target.Namespace is null);

        var violations = EngineModel.ForbiddenDependencies(
            FixtureModel.Value,
            isSource: assembly => assembly == FixtureAssembly,
            isForbiddenTarget: _ => true);

        Assert.Contains($"{typeof(InlineArrayFixture).FullName} -> System.Threading.Tasks.Task", violations);
    }

    [Fact]
    public void Code_without_a_namespace_is_checked_by_the_assembly_it_comes_from()
    {
        var violations = EngineModel.ForbiddenDependencies(
            FixtureModel.Value,
            isSource: assembly => assembly == FixtureAssembly,
            isForbiddenTarget: assembly => assembly == "Elysium.Core");

        Assert.Contains($"{nameof(GlobalNamespaceFixture)} -> Elysium.Core.EntityId", violations);
    }

    [Fact]
    public void A_forbidden_dependency_in_the_top_level_code_of_a_Program_cs_is_caught()
    {
        // The demo client is only a Program.cs, whose top-level statements open the window (SdlWindow) outside any
        // namespace and in a method ArchUnitNET does not read. Declaring the platform project forbidden for it must
        // still report that use.
        var violations = EngineModel.ForbiddenDependencies(
            isSource: assembly => assembly == "Elysium.Demo.Client",
            isForbiddenTarget: assembly => assembly == "Elysium.Core.Client.Sdl");

        Assert.Equal(["Elysium.Demo.Client (assembly) -> Elysium.Core.Client.Sdl"], violations);
    }

    [Fact]
    public void When_a_type_cannot_be_checked_the_error_names_it()
    {
        var error = Assert.Throws<InvalidOperationException>(() => EngineModel.ForbiddenDependencies(
            FixtureModel.Value,
            isSource: assembly => assembly == FixtureAssembly,
            isForbiddenTarget: _ => throw new FormatException("unreadable")));

        Assert.Contains("Could not check the dependencies of", error.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(error.InnerException);
    }
}
