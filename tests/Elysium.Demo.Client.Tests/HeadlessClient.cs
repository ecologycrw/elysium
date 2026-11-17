using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Core.Client.Input;
using Elysium.Core.Client.Rendering;

namespace Elysium.Demo.Client.Tests;

/// <summary>
/// The demo client without a window: the same login, session and frames as the real one. The test plays the
/// part of the window, filling the input with clicks on tiles and reading the scene the client would draw.
/// </summary>
internal sealed class HeadlessClient : IAsyncDisposable
{
    public const int Width = 1280;
    public const int Height = 720;

    private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(16);

    private readonly FrameInput _input = new();
    private bool _closed;

    private HeadlessClient(string name, ClientSession session)
    {
        Name = name;
        Session = session;
        _input.SetViewport(Width, Height);
    }

    public string Name { get; }

    public ClientSession Session { get; }

    public ClientWorld World => Session.World;

    /// <summary>What the client would draw this frame.</summary>
    public Scene Scene { get; } = new();

    public static async Task<HeadlessClient> LogInAsync(string name, int port, CancellationToken cancellationToken)
    {
        var login = await DemoLogin.LogInAsync(new ClientArguments(name, "127.0.0.1", port), cancellationToken);
        Assert.True(login.Link is not null, $"{name} could not log in: {login.Failure}");
        return new HeadlessClient(name, new ClientSession(login.Link, DemoLook.CreateWorld()));
    }

    /// <summary>
    /// Runs frames of every client about 60 times a second until <paramref name="condition"/> holds. False after
    /// <paramref name="timeout"/>.
    /// </summary>
    public static async Task<bool> RunUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken, params HeadlessClient[] clients)
    {
        var frames = (int)(timeout / FrameTime);
        for (var frame = 0; frame < frames; frame++)
        {
            foreach (var client in clients)
            {
                client.RunFrame();
            }

            if (condition())
            {
                return true;
            }

            await Task.Delay(FrameTime, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Clicks, in the next frame, the pixel where the player sees <paramref name="tile"/>: its top, at the height of
    /// a blocked tile when it is one. The tile must be on screen, as for a real player.
    /// </summary>
    public void ClickTile(TilePosition tile)
    {
        var map = Elysium.Demo.Shared.DemoWorld.LoadMap();
        var surface = map.IsWalkable(tile) ? 0 : DemoLook.Style.BlockedHeight;
        Assert.True(World.Camera.TryProject(TileSpace.ToWorld(tile) with { Y = surface }, out var pixel));
        Assert.True(pixel.X is >= 0 and < Width && pixel.Y is >= 0 and < Height, $"{Name} cannot see {tile} on screen: pixel {pixel}");
        _input.AddClick(pixel, PointerButton.Primary);
    }

    /// <summary>The blocks drawn in the color of other players.</summary>
    public SceneBlock[] OtherCharacters() => [.. Scene.Blocks.ToArray().Where(block => block.Color == DemoLook.Style.Others)];

    /// <summary>Closes the client, as closing the window does. Closing it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_closed)
        {
            _closed = true;
            await Session.DisposeAsync();
        }
    }

    private void RunFrame()
    {
        Session.RunFrame(_input, FrameTime, Scene);
        _input.BeginFrame();
    }
}
