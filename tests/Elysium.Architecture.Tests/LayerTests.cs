namespace Elysium.Architecture.Tests;

/// <summary>
/// INV-02: dependencies only go down (Game -> Classic -> Core),
/// shared code never depends on server or client code, and server and client never depend on each other.
/// Layers are recognized by namespace, which IDE0130 keeps equal to the project name.
/// </summary>
public sealed class LayerTests
{
    [Fact]
    public void Every_engine_and_game_project_is_covered_by_these_tests()
    {
        Assert.NotEmpty(EngineModel.ProjectNames);

        var missing = EngineModel.ProjectNames.Where(name => EngineModel.TryLoad(name) is null).ToList();

        // A project listed here must be added as a ProjectReference in Elysium.Architecture.Tests.csproj.
        Assert.Empty(missing);
    }

    [Fact]
    public void Core_does_not_depend_on_Classic_or_on_games()
    {
        var violations = EngineModel.ForbiddenDependencies(
            isSource: ns => LayerOf(ns) == Layer.Core,
            isForbiddenTarget: ns => LayerOf(ns) is Layer.Classic or Layer.Game);

        Assert.Empty(violations);
    }

    [Fact]
    public void Classic_does_not_depend_on_games()
    {
        var violations = EngineModel.ForbiddenDependencies(
            isSource: ns => LayerOf(ns) == Layer.Classic,
            isForbiddenTarget: ns => LayerOf(ns) == Layer.Game);

        Assert.Empty(violations);
    }

    [Fact]
    public void Shared_and_neutral_code_does_not_depend_on_server_or_client_code()
    {
        var violations = EngineModel.ForbiddenDependencies(
            isSource: ns => IsElysium(ns) && SideOf(ns) is Side.Shared or Side.Neutral,
            isForbiddenTarget: ns => IsElysium(ns) && SideOf(ns) is Side.Server or Side.Client);

        Assert.Empty(violations);
    }

    [Fact]
    public void Server_and_client_code_do_not_depend_on_each_other()
    {
        var serverToClient = EngineModel.ForbiddenDependencies(
            isSource: ns => IsElysium(ns) && SideOf(ns) == Side.Server,
            isForbiddenTarget: ns => IsElysium(ns) && SideOf(ns) == Side.Client);
        var clientToServer = EngineModel.ForbiddenDependencies(
            isSource: ns => IsElysium(ns) && SideOf(ns) == Side.Client,
            isForbiddenTarget: ns => IsElysium(ns) && SideOf(ns) == Side.Server);

        Assert.Empty(serverToClient);
        Assert.Empty(clientToServer);
    }

    private enum Layer
    {
        None,
        Core,
        Classic,
        Game,
    }

    private enum Side
    {
        Neutral,
        Shared,
        Server,
        Client,
    }

    private static bool IsElysium(string ns) => ns == "Elysium" || ns.StartsWith("Elysium.", StringComparison.Ordinal);

    private static Layer LayerOf(string ns)
    {
        if (!IsElysium(ns))
        {
            return Layer.None;
        }

        var segments = ns.Split('.');
        if (segments.Length < 2)
        {
            return Layer.None;
        }

        return segments[1] switch
        {
            "Core" => Layer.Core,
            "Classic" => Layer.Classic,
            "Architecture" or "Checks" or "Bot" => Layer.None,
            _ => Layer.Game,
        };
    }

    private static Side SideOf(string ns)
    {
        var segments = ns.Split('.');
        if (segments.Contains("Server"))
        {
            return Side.Server;
        }

        if (segments.Contains("Client"))
        {
            return Side.Client;
        }

        return segments.Contains("Shared") ? Side.Shared : Side.Neutral;
    }
}
