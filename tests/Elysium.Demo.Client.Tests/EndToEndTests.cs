using System.Net;
using System.Numerics;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Demo.Server;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Demo.Client.Tests;

/// <summary>
/// Slice 1 over the real network: the demo server and two clients that run the real client's frames without a
/// window. On the demo map, Ana enters at (10, 10) and Bruno at (13, 11), in view of each other.
/// </summary>
public sealed class EndToEndTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TilePosition AnaSpawn = new(10, 10);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("CA", "0001-CA-02")]
    [Trait("CA", "0001-CA-05")]
    public async Task When_Ana_clicks_a_tile_she_walks_there_and_Bruno_sees_her_walk_in_the_others_color()
    {
        await using var server = StartServer();
        await using var ana = await HeadlessClient.LogInAsync("Ana", server.Port, Token);
        await using var bruno = await HeadlessClient.LogInAsync("Bruno", server.Port, Token);
        Assert.True(await HeadlessClient.RunUntilAsync(() => SeesSomeone(ana) && SeesSomeone(bruno), Timeout, Token, ana, bruno));
        var anaId = ana.World.State.Self!.Value;
        var destination = new TilePosition(12, 13);

        ana.ClickTile(destination);
        Assert.True(await HeadlessClient.RunUntilAsync(
            () => IsShownAt(ana, anaId, destination) && IsShownAt(bruno, anaId, destination), Timeout, Token, ana, bruno));

        Assert.Equal(destination, ana.World.State.SelfPosition);
        Assert.Equal(destination, bruno.World.State.VisibleEntities.Single(entity => entity.Name == "Ana").Position);
        var anaForBruno = Assert.Single(bruno.OtherCharacters());
        Assert.Equal(new Vector2(destination.X, destination.Y), new Vector2(anaForBruno.Center.X, anaForBruno.Center.Z));
    }

    [Fact]
    [Trait("CA", "0001-CA-04")]
    public async Task A_click_on_a_blocked_tile_is_refused_the_mark_shows_it_and_Ana_stays()
    {
        await using var server = StartServer();
        await using var ana = await HeadlessClient.LogInAsync("Ana", server.Port, Token);
        Assert.True(await HeadlessClient.RunUntilAsync(() => ana.World.State.Self is not null, Timeout, Token, ana));
        var anaId = ana.World.State.Self!.Value;

        // Walk up next to the wall at the top of the map, then click the wall itself.
        var nearWall = new TilePosition(AnaSpawn.X, 2);
        ana.ClickTile(nearWall);
        Assert.True(await HeadlessClient.RunUntilAsync(() => IsShownAt(ana, anaId, nearWall), Timeout, Token, ana));
        var wall = new TilePosition(AnaSpawn.X, 0);
        ana.ClickTile(wall);
        Assert.True(await HeadlessClient.RunUntilAsync(
            () => ana.World.Destination.State == DestinationMarkState.Refused, Timeout, Token, ana));

        Assert.Equal(wall, ana.World.Destination.Tile);
        Assert.Equal(1, ana.World.State.MovesRefused);
        Assert.Equal(nearWall, ana.World.State.SelfPosition);
        Assert.True(IsShownAt(ana, anaId, nearWall));
    }

    [Fact]
    [Trait("CA", "0001-CA-05")]
    public async Task When_Bruno_closes_his_client_he_disappears_for_Ana()
    {
        await using var server = StartServer();
        await using var ana = await HeadlessClient.LogInAsync("Ana", server.Port, Token);
        var bruno = await HeadlessClient.LogInAsync("Bruno", server.Port, Token);
        Assert.True(await HeadlessClient.RunUntilAsync(() => ana.OtherCharacters().Length == 1, Timeout, Token, ana, bruno));

        await bruno.DisposeAsync();

        Assert.True(await HeadlessClient.RunUntilAsync(() => ana.OtherCharacters().Length == 0, Timeout, Token, ana));
        Assert.Empty(ana.World.State.VisibleEntities);
    }

    private static bool SeesSomeone(HeadlessClient client) => client.World.State.VisibleEntities.Count > 0;

    private static bool IsShownAt(HeadlessClient client, EntityId entity, TilePosition tile) =>
        client.World.TryGetShownPosition(entity, out var shown) && shown == new Vector2(tile.X, tile.Y);

    private static DemoServer StartServer() =>
        DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);
}
