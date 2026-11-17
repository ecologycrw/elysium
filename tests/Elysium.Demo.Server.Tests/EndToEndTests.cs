using System.Net;
using Elysium.Bot;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Demo.Server.Tests;

/// <summary>
/// The roadmap scenario of slice 0 over the real network: the demo server and three bots.
/// </summary>
public sealed class EndToEndTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("CA", "0000-CA-10")]
    public async Task Three_bots_run_the_slice_0_scenario_against_the_demo_server()
    {
        await using var server = DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);
        var output = new StringWriter();

        var passed = await DemoScenario.RunAsync("127.0.0.1", server.Port, TextWriter.Synchronized(output), Token);

        var lines = output.ToString();
        Assert.True(passed, lines);
        Assert.Contains("[Ana] sees Bruno", lines, StringComparison.Ordinal);
        Assert.Contains("[Bruno] sees Ana", lines, StringComparison.Ordinal);
        Assert.Contains("[Caio] sees nobody", lines, StringComparison.Ordinal);
        Assert.Contains("[Caio] sees Ana", lines, StringComparison.Ordinal);
        Assert.Contains("[Ana] sees Caio", lines, StringComparison.Ordinal);
        Assert.Contains("[Caio] lost sight of Ana", lines, StringComparison.Ordinal);
        Assert.Contains("[Bruno] lost sight of Caio", lines, StringComparison.Ordinal);
        Assert.Contains("PASSED", lines, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "0000-CA-10")]
    public async Task The_scenario_passes_again_on_the_same_server_after_the_bots_leave()
    {
        await using var server = DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);

        var first = await DemoScenario.RunAsync("127.0.0.1", server.Port, TextWriter.Null, Token);
        await WaitForEmptyMapAsync();
        var second = await DemoScenario.RunAsync("127.0.0.1", server.Port, TextWriter.Null, Token);

        Assert.True(first);
        Assert.True(second);
    }

    [Fact]
    public void The_demo_map_loads_with_three_spawn_points()
    {
        var map = DemoGame.LoadMap();

        Assert.Equal(3, map.SpawnPoints.Count);
        Assert.True(map.SpawnPoints[0].ChebyshevDistance(map.SpawnPoints[1]) <= DemoGame.MapRules.ViewRadius);
        Assert.True(map.SpawnPoints[0].ChebyshevDistance(map.SpawnPoints[2]) > DemoGame.MapRules.ViewRadius);
        Assert.True(map.SpawnPoints[1].ChebyshevDistance(map.SpawnPoints[2]) > DemoGame.MapRules.ViewRadius);
    }

    /// <summary>The bots disconnect when the scenario ends; give the map a few ticks to process their leaving.</summary>
    private static Task WaitForEmptyMapAsync() => Task.Delay(TimeSpan.FromMilliseconds(500), Token);
}
