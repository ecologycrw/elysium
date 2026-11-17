using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Client.Presentation;
using Elysium.Core.Client.Rendering;
using Elysium.Demo.Shared;

namespace Elysium.Demo.Client;

/// <summary>How the demonstration game looks and moves on screen. These numbers are the game's (INV-10).</summary>
public static class DemoLook
{
    /// <summary>Characters slide one tile per step of the server, and jump ahead when more than 3 steps behind.</summary>
    public static MotionRules Motion { get; } = new(DemoWorld.StepDuration, maxStepsBehind: 3);

    public static CameraRules Camera { get; } = new(pitchDegrees: 50, yawDegrees: 0, distance: 16, fieldOfViewDegrees: 40);

    public static SceneStyle Style { get; } = new()
    {
        Ground = new ColorRgb(0.33f, 0.52f, 0.30f),
        Blocked = new ColorRgb(0.50f, 0.47f, 0.44f),
        BlockedHeight = 0.6f,
        Self = new ColorRgb(0.20f, 0.45f, 0.95f),
        Others = new ColorRgb(0.95f, 0.55f, 0.20f),
        CharacterSize = new Vector3(0.5f, 1.4f, 0.5f),
        Destination = new ColorRgb(0.95f, 0.95f, 0.85f),
        RefusedDestination = new ColorRgb(0.90f, 0.15f, 0.15f),
        RefusedDestinationTime = TimeSpan.FromSeconds(1),
        Light = new DirectionalLight(new Vector3(-0.4f, -1, -0.6f), new ColorRgb(1, 0.95f, 0.85f), new ColorRgb(0.35f, 0.35f, 0.42f)),
        Background = new ColorRgb(0.12f, 0.14f, 0.18f),
    };

    /// <summary>A new view of the demo map, ready for the frames of a session.</summary>
    public static ClientWorld CreateWorld() => new(DemoWorld.LoadMap(), Motion, Camera, Style);
}
