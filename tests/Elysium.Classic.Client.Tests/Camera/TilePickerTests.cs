using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Tests.Camera;

public sealed class TilePickerTests
{
    private static readonly GridMap Map = new(64, 40, blockedTiles: [], spawnPoints: [new TilePosition(10, 10)]);

    /// <summary>Camera angles and window sizes the client must handle, from small windows to wide screens.</summary>
    public static TheoryData<float, float, int, int> CamerasAndWindows => new()
    {
        { 50, 0, 1280, 720 },
        { 50, 45, 1366, 768 },
        { 35, 45, 1920, 1080 },
        { 60, 30, 800, 600 },
        { 45, 135, 333, 777 },
    };

    [Theory]
    [Trait("CA", "0001-CA-02")]
    [MemberData(nameof(CamerasAndWindows))]
    public void The_center_of_the_window_picks_the_tile_the_camera_follows(float pitch, float yaw, int width, int height)
    {
        var camera = Camera(pitch, yaw, target: new TilePosition(20, 15), width, height);

        Assert.True(TilePicker.TryPickTile(camera, new Vector2(width / 2f, height / 2f), Map, out var tile));
        Assert.Equal(new TilePosition(20, 15), tile);
    }

    [Theory]
    [Trait("CA", "0001-CA-02")]
    [MemberData(nameof(CamerasAndWindows))]
    public void Clicking_anywhere_on_a_tile_near_the_character_picks_that_tile(float pitch, float yaw, int width, int height)
    {
        var target = new TilePosition(20, 15);
        var camera = Camera(pitch, yaw, target, width, height);

        for (var dx = -3; dx <= 3; dx++)
        {
            for (var dy = -3; dy <= 3; dy++)
            {
                var expected = new TilePosition(target.X + dx, target.Y + dy);
                foreach (var corner in new[] { new Vector2(-0.45f, -0.45f), new Vector2(0.45f, 0.45f), new Vector2(0.45f, -0.45f), Vector2.Zero })
                {
                    var point = TileSpace.ToWorld(new Vector2(expected.X + corner.X, expected.Y + corner.Y));
                    Assert.True(camera.TryProject(point, out var pixel));

                    Assert.True(TilePicker.TryPickTile(camera, pixel, Map, out var tile));
                    Assert.Equal(expected, tile);
                }
            }
        }
    }

    [Fact]
    [Trait("CA", "0001-CA-02")]
    public void Below_the_center_is_nearer_to_the_camera_and_to_the_right_is_a_higher_column()
    {
        // Yaw 0: the camera sits on the side of the higher rows, looking towards the lower ones.
        var camera = Camera(pitch: 50, yaw: 0, target: new TilePosition(20, 15), 1280, 720);

        Assert.True(TilePicker.TryPickTile(camera, new Vector2(640, 600), Map, out var below));
        Assert.True(TilePicker.TryPickTile(camera, new Vector2(1200, 360), Map, out var right));

        Assert.Equal(20, below.X);
        Assert.True(below.Y > 15, $"a click below the center picked row {below.Y}");
        Assert.Equal(15, right.Y);
        Assert.True(right.X > 20, $"a click right of the center picked column {right.X}");
    }

    [Fact]
    public void A_wider_window_shows_more_columns_without_stretching_the_ground()
    {
        var square = Camera(pitch: 50, yaw: 0, target: new TilePosition(30, 20), 720, 720);
        var wide = Camera(pitch: 50, yaw: 0, target: new TilePosition(30, 20), 1440, 720);

        Assert.True(TilePicker.TryPickTile(square, new Vector2(720, 360), Map, out var squareEdge));
        Assert.True(TilePicker.TryPickTile(wide, new Vector2(1080, 360), Map, out var sameDistanceInWide));

        Assert.Equal(squareEdge, sameDistanceInWide);
    }

    [Fact]
    [Trait("CA", "0001-CA-02")]
    public void Clicking_the_sky_picks_nothing()
    {
        var lowCamera = new ThreeQuarterCamera(new CameraRules(pitchDegrees: 15, yawDegrees: 0, distance: 20, fieldOfViewDegrees: 60));
        lowCamera.Update(TileSpace.ToWorld(new TilePosition(20, 15)), 1280, 720);

        Assert.False(TilePicker.TryPickTile(lowCamera, new Vector2(640, 0), Map, out _));
    }

    [Fact]
    [Trait("CA", "0001-CA-02")]
    public void Clicking_outside_the_map_picks_nothing()
    {
        var camera = Camera(pitch: 50, yaw: 0, target: new TilePosition(0, 0), 1280, 720);

        Assert.False(TilePicker.TryPickTile(camera, new Vector2(10, 360), Map, out _));
        Assert.True(TilePicker.TryPickTile(camera, new Vector2(640, 360), Map, out var corner));
        Assert.Equal(new TilePosition(0, 0), corner);
    }

    [Fact]
    public void Camera_rules_that_cannot_work_are_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraRules(0, 0, 10, 45));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraRules(90, 0, 10, 45));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraRules(50, 0, 0, 45));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraRules(50, 0, 10, 180));
    }

    private static ThreeQuarterCamera Camera(float pitch, float yaw, TilePosition target, int width, int height)
    {
        var camera = new ThreeQuarterCamera(new CameraRules(pitch, yaw, distance: 18, fieldOfViewDegrees: 40));
        camera.Update(TileSpace.ToWorld(target), width, height);
        return camera;
    }
}
