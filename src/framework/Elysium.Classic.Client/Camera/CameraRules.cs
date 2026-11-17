namespace Elysium.Classic.Client.Camera;

/// <summary>
/// The fixed 3/4 camera of the classic genre. The game decides the numbers (INV-10).
/// </summary>
public sealed record CameraRules
{
    /// <param name="pitchDegrees">How far the camera looks down, from 0 (horizontal) to 90 (straight down), exclusive.</param>
    /// <param name="yawDegrees">The fixed horizontal angle around the target; 0 looks along the rows, from below.</param>
    /// <param name="distance">How far the camera is from the point it follows, in tiles.</param>
    /// <param name="fieldOfViewDegrees">The vertical field of view, between 0 and 180, exclusive.</param>
    public CameraRules(float pitchDegrees, float yawDegrees, float distance, float fieldOfViewDegrees)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pitchDegrees, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(pitchDegrees, 90);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(distance, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfViewDegrees, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfViewDegrees, 180);
        PitchDegrees = pitchDegrees;
        YawDegrees = yawDegrees;
        Distance = distance;
        FieldOfViewDegrees = fieldOfViewDegrees;
    }

    public float PitchDegrees { get; }

    public float YawDegrees { get; }

    public float Distance { get; }

    public float FieldOfViewDegrees { get; }
}
