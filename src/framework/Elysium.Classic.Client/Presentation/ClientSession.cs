using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Client.Connection;
using Elysium.Core.Client.Input;
using Elysium.Core.Client.Rendering;

namespace Elysium.Classic.Client.Presentation;

/// <summary>
/// A logged-in classic client, frame by frame: clicks on the ground become move requests, the messages that
/// arrived update the world, time moves the characters, and the scene is filled. It never waits for the network.
/// The window and the headless test clients run the same frames; only who fills the input and who draws the scene
/// changes. Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class ClientSession : IAsyncDisposable
{
    private readonly ServerLink _link;

    /// <summary>Starts a session on a link that has already logged in. The session owns the link from now on.</summary>
    public ClientSession(ServerLink link, ClientWorld world)
    {
        _link = link ?? throw new ArgumentNullException(nameof(link));
        World = world ?? throw new ArgumentNullException(nameof(world));
    }

    public ClientWorld World { get; }

    /// <summary>False once the connection to the server ended.</summary>
    public bool IsConnected => _link.IsConnected;

    /// <summary>Why the connection ended, or null while it is up.</summary>
    public string? LostReason => _link.LostReason;

    /// <summary>
    /// Runs one frame: <paramref name="input"/> is what the player did since the last one and
    /// <paramref name="elapsed"/> how long it took; <paramref name="scene"/> receives what to draw.
    /// </summary>
    public void RunFrame(FrameInput input, TimeSpan elapsed, Scene scene)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequestClickedDestinations(input);
        ApplyArrivedMessages();
        World.Advance(elapsed, input.ViewportWidth, input.ViewportHeight);
        World.Fill(scene);
    }

    public ValueTask DisposeAsync() => _link.DisposeAsync();

    /// <summary>Clicks are read against the camera of the frame the player saw when clicking.</summary>
    private void RequestClickedDestinations(FrameInput input)
    {
        foreach (var click in input.Clicks)
        {
            if (click.Button == PointerButton.Primary && World.TryClickGround(click.Position, out var destination))
            {
                _link.Send(new MoveRequest(destination));
            }
        }
    }

    /// <summary>Applies the world messages; any other message type belongs to the game, which does not use one yet.</summary>
    private void ApplyArrivedMessages()
    {
        while (_link.TryTakeMessage(out var frame))
        {
            if (WorldState.Handles(frame.Type))
            {
                World.Apply(frame);
            }
        }
    }
}
