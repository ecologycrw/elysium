using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Core.Client.Rendering;

namespace Elysium.Classic.Client.Tests.Presentation;

/// <summary>A small map, a style with a distinct color for each thing, and the numbers the presentation tests share.</summary>
internal static class TestWorld
{
    public const int Width = 12;
    public const int Height = 10;
    public const int ViewportWidth = 1280;
    public const int ViewportHeight = 720;

    public static readonly EntityId Ana = new(1);
    public static readonly EntityId Bruno = new(2);
    public static readonly TilePosition Wall = new(7, 5);
    public static readonly TimeSpan Step = TimeSpan.FromMilliseconds(250);

    public static readonly GridMap Map = new(Width, Height, blockedTiles: [Wall], spawnPoints: [new TilePosition(5, 5)]);

    public static readonly SceneStyle Style = new()
    {
        Ground = new ColorRgb(0.3f, 0.5f, 0.3f),
        Blocked = new ColorRgb(0.4f, 0.4f, 0.4f),
        BlockedHeight = 0.6f,
        Self = new ColorRgb(0.2f, 0.4f, 0.9f),
        Others = new ColorRgb(0.9f, 0.5f, 0.2f),
        CharacterSize = new Vector3(0.5f, 1.4f, 0.5f),
        Destination = new ColorRgb(1, 1, 1),
        RefusedDestination = new ColorRgb(0.9f, 0.1f, 0.1f),
        RefusedDestinationTime = TimeSpan.FromSeconds(1),
        Light = new DirectionalLight(new Vector3(-0.3f, -1, -0.2f), new ColorRgb(1, 1, 0.9f), new ColorRgb(0.3f, 0.3f, 0.35f)),
        Background = new ColorRgb(0.1f, 0.1f, 0.15f),
    };

    public static ClientWorld Create() => new(
        Map,
        new MotionRules(Step, maxStepsBehind: 2),
        new CameraRules(pitchDegrees: 50, yawDegrees: 0, distance: 14, fieldOfViewDegrees: 40),
        Style);
}
