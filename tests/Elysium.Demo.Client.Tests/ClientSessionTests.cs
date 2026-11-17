using System.Net;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Core.Client.Input;
using Elysium.Core.Client.Rendering;
using Elysium.Demo.Server;
using Elysium.Demo.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Demo.Client.Tests;

/// <summary>The frames of the real client without a window, against the real demo server.</summary>
public sealed class ClientSessionTests
{
    private const int Width = 1280;
    private const int Height = 720;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("CA", "0001-CA-01")]
    public async Task After_logging_in_the_frames_show_the_demo_map_centered_on_the_own_character()
    {
        await using var server = DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);
        var login = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", server.Port), Token);
        await using var session = new ClientSession(login.Link!, DemoLook.CreateWorld());
        var input = new FrameInput();
        input.SetViewport(Width, Height);
        var scene = new Scene();

        Assert.True(await RunFramesUntilAsync(session, input, scene, () => session.World.State.Self is not null));

        var map = DemoWorld.LoadMap();
        var spawn = map.SpawnPoints[0];
        Assert.Equal(spawn, session.World.State.SelfPosition);
        Assert.Equal(TileSpace.ToWorld(spawn), session.World.Camera.Target);
        var blocks = scene.Blocks.ToArray();
        Assert.Equal(map.Width * map.Height, blocks.Count(block => block.Color == DemoLook.Style.Ground || block.Color == DemoLook.Style.Blocked));
        Assert.Contains(blocks, block => block.Color == DemoLook.Style.Blocked);
        Assert.Single(blocks, block => block.Color == DemoLook.Style.Self);
    }

    [Fact]
    [Trait("CA", "0001-CA-06")]
    public async Task When_the_server_stops_the_frames_go_on_and_the_session_says_the_connection_fell()
    {
        var server = DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);
        var login = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", server.Port), Token);
        await using var session = new ClientSession(login.Link!, DemoLook.CreateWorld());
        var input = new FrameInput();
        input.SetViewport(Width, Height);
        var scene = new Scene();
        Assert.True(await RunFramesUntilAsync(session, input, scene, () => session.World.State.Self is not null));

        await server.DisposeAsync();

        Assert.True(await RunFramesUntilAsync(session, input, scene, () => !session.IsConnected));
        Assert.NotNull(session.LostReason);
        session.RunFrame(input, TimeSpan.FromMilliseconds(16), scene);
        Assert.Single(scene.Blocks.ToArray(), block => block.Color == DemoLook.Style.Self);
    }

    /// <summary>Runs frames about 60 times a second until <paramref name="condition"/> holds; false after 5 seconds.</summary>
    private static async Task<bool> RunFramesUntilAsync(ClientSession session, FrameInput input, Scene scene, Func<bool> condition)
    {
        var frameTime = TimeSpan.FromMilliseconds(16);
        for (var frame = 0; frame < 5 * 60; frame++)
        {
            input.BeginFrame();
            session.RunFrame(input, frameTime, scene);
            if (condition())
            {
                return true;
            }

            await Task.Delay(frameTime, Token);
        }

        return false;
    }
}
