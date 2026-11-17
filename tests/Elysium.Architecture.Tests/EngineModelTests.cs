using ArchUnitNET.Loader;
using Elysium.Architecture.Tests.Fixtures;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace Elysium.Architecture.Tests;

/// <summary>
/// The helper behind the layer rules keeps working on code the compiler rewrites, and when it cannot read a type,
/// it says which one (P-0004).
/// </summary>
public sealed class EngineModelTests
{
    private const string FixtureNamespace = "Elysium.Architecture.Tests.Fixtures";

    private static readonly Lazy<ArchModel> FixtureModel =
        new(() => new ArchLoader().LoadAssemblies(typeof(InlineArrayFixture).Assembly).Build());

    [Fact]
    public void A_dependency_without_a_namespace_is_never_reported_and_does_not_break_the_check()
    {
        var fixture = FixtureModel.Value.Types.Single(type => type.FullName == typeof(InlineArrayFixture).FullName);
        Assert.Contains(fixture.Dependencies, dependency => dependency.Target.Namespace is null);

        var violations = EngineModel.ForbiddenDependencies(
            FixtureModel.Value,
            isSource: ns => ns == FixtureNamespace,
            isForbiddenTarget: _ => true);

        var withNamespace = fixture.Dependencies
            .Where(dependency => dependency.Target.Namespace is not null)
            .Select(dependency => $"{fixture.FullName} -> {dependency.Target.FullName}")
            .Distinct()
            .Order(StringComparer.Ordinal);
        Assert.Equal(withNamespace, violations);
        Assert.Contains($"{typeof(InlineArrayFixture).FullName} -> System.Threading.Tasks.Task", violations);
    }

    [Fact]
    public void When_a_type_cannot_be_checked_the_error_names_it()
    {
        var error = Assert.Throws<InvalidOperationException>(() => EngineModel.ForbiddenDependencies(
            FixtureModel.Value,
            isSource: ns => ns == FixtureNamespace,
            isForbiddenTarget: _ => throw new FormatException("unreadable")));

        Assert.Contains(typeof(InlineArrayFixture).FullName!, error.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(error.InnerException);
    }
}
