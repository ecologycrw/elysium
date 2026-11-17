using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Core.Client.Rendering;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Client.Presentation;

/// <summary>
/// One frame of the classic client, without a window: applies what the server sent, slides the characters,
/// follows the player's own character with the camera, turns clicks into destinations and fills the scene.
/// The graphical client and the headless test clients run the same frames. After warming up, a frame does not
/// allocate (CA-07 of slice 1). Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class ClientWorld
{
    private readonly GridMap _map;
    private readonly SceneStyle _style;
    private readonly WorldMotion _motion;
    private readonly WorldSceneBuilder _sceneBuilder;

    public ClientWorld(GridMap map, MotionRules motion, CameraRules camera, SceneStyle style)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _motion = new WorldMotion(motion);
        _sceneBuilder = new WorldSceneBuilder(map, style);
        Camera = new ThreeQuarterCamera(camera);
        Destination = new DestinationMark(style.RefusedDestinationTime);
    }

    /// <summary>What the server confirmed: the player's own character and the entities in view.</summary>
    public WorldState State { get; } = new();

    public ThreeQuarterCamera Camera { get; }

    /// <summary>The mark on the destination the player last clicked.</summary>
    public DestinationMark Destination { get; }

    /// <summary>
    /// Applies one message from the server (one that <see cref="WorldState.Handles"/>) and says what changed,
    /// or null when nothing did. Throws <see cref="ProtocolException"/> for a malformed message.
    /// </summary>
    public WorldChange? Apply(Frame frame)
    {
        var change = State.Apply(frame);
        if (change is { } changed)
        {
            _motion.Apply(changed);
            Destination.Apply(changed);
        }

        return change;
    }

    /// <summary>
    /// Turns a click at the pixel <paramref name="screen"/> into the tile to walk to, and marks it. False, with
    /// nothing marked, for the sky, outside the map, or before the player entered the map. Sending the request is
    /// the caller's job; whether the tile can be walked to is the server's decision (INV-01).
    /// </summary>
    public bool TryClickGround(Vector2 screen, out TilePosition destination)
    {
        if (State.Self is null || !TilePicker.TryPickTile(Camera, screen, _map, _style.BlockedHeight, out destination))
        {
            destination = default;
            return false;
        }

        Destination.Show(destination);
        return true;
    }

    /// <summary>Moves the world shown forward by the time since the last frame, in a window of the given size, in pixels.</summary>
    public void Advance(TimeSpan elapsed, int viewportWidth, int viewportHeight)
    {
        _motion.Advance(elapsed);
        Vector2? selfShown = State.Self is { } self && _motion.TryGetPosition(self, out var shown) ? shown : null;
        Destination.Advance(elapsed, selfShown);
        Camera.Update(selfShown is { } target ? TileSpace.ToWorld(target) : Camera.Target, viewportWidth, viewportHeight);
    }

    /// <summary>Where <paramref name="entity"/> is drawn, in tiles (X = column, Y = row), if it is shown.</summary>
    public bool TryGetShownPosition(EntityId entity, out Vector2 position) => _motion.TryGetPosition(entity, out position);

    /// <summary>Replaces what <paramref name="scene"/> held with this frame of the world.</summary>
    public void Fill(Scene scene) => _sceneBuilder.Fill(scene, Camera.ViewProjection, State, _motion, Destination);
}
