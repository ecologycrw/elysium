using System.Numerics;

namespace Elysium.Classic.Client.Camera;

/// <summary>
/// A perspective camera at a fixed angle that follows a point on the ground, as in the classic genre.
/// It gives the matrices for the GPU and converts between the world and the screen in both directions:
/// a world point to a pixel (for drawing over the scene) and a pixel to the ground (for clicking).
/// Screen points are in pixels, from the top-left corner of the window. Updating it does not allocate.
/// </summary>
public sealed class ThreeQuarterCamera
{
    private const float NearPlane = 0.1f;

    private readonly CameraRules _rules;
    private readonly Vector3 _offset;
    private Matrix4x4 _inverseViewProjection;

    public ThreeQuarterCamera(CameraRules rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        var pitch = float.DegreesToRadians(rules.PitchDegrees);
        var yaw = float.DegreesToRadians(rules.YawDegrees);
        _offset = rules.Distance * new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw));
        Update(Vector3.Zero, 1, 1);
    }

    /// <summary>The point the camera follows.</summary>
    public Vector3 Target { get; private set; }

    /// <summary>Where the camera is.</summary>
    public Vector3 Position => Target + _offset;

    /// <summary>The size of the window, in pixels.</summary>
    public Vector2 ViewportSize { get; private set; }

    /// <summary>World to clip space, as System.Numerics writes it (row vectors); the shaders read it as is.</summary>
    public Matrix4x4 ViewProjection { get; private set; }

    /// <summary>Follows <paramref name="target"/> in a window of the given size, in pixels.</summary>
    public void Update(Vector3 target, int viewportWidth, int viewportHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(viewportWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(viewportHeight, 1);
        Target = target;
        ViewportSize = new Vector2(viewportWidth, viewportHeight);
        var view = Matrix4x4.CreateLookAt(Position, target, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(_rules.FieldOfViewDegrees),
            (float)viewportWidth / viewportHeight,
            NearPlane,
            _rules.Distance * 10);
        ViewProjection = view * projection;
        Matrix4x4.Invert(ViewProjection, out _inverseViewProjection);
    }

    /// <summary>The pixel where <paramref name="world"/> appears. False when the point is behind the camera.</summary>
    public bool TryProject(Vector3 world, out Vector2 screen)
    {
        var clip = Vector4.Transform(new Vector4(world, 1), ViewProjection);
        if (clip.W <= 0)
        {
            screen = default;
            return false;
        }

        var ndc = new Vector2(clip.X / clip.W, clip.Y / clip.W);
        screen = new Vector2((ndc.X + 1) * 0.5f * ViewportSize.X, (1 - ndc.Y) * 0.5f * ViewportSize.Y);
        return true;
    }

    /// <summary>
    /// The point on the ground (Y = 0) under the pixel <paramref name="screen"/>. False when the pixel shows the
    /// sky, where the ray from the camera never reaches the ground.
    /// </summary>
    public bool TryPickGround(Vector2 screen, out Vector3 ground)
    {
        var ndc = new Vector2((screen.X / ViewportSize.X * 2) - 1, 1 - (screen.Y / ViewportSize.Y * 2));
        var near = Unproject(new Vector3(ndc, 0));
        var far = Unproject(new Vector3(ndc, 1));
        var direction = far - near;
        if (direction.Y >= 0)
        {
            ground = default;
            return false;
        }

        var distance = -near.Y / direction.Y;
        ground = near + (direction * distance);
        ground.Y = 0;
        return true;
    }

    private Vector3 Unproject(Vector3 ndc)
    {
        var point = Vector4.Transform(new Vector4(ndc, 1), _inverseViewProjection);
        return new Vector3(point.X, point.Y, point.Z) / point.W;
    }
}
