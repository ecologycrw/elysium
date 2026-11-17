namespace Elysium.Classic.Client.Movement;

/// <summary>
/// How the client shows movement between tiles. The game decides the numbers (INV-10).
/// </summary>
public sealed record MotionRules
{
    /// <param name="stepDuration">How long one step takes on the server: the same for a straight and a diagonal step.</param>
    /// <param name="maxStepsBehind">How many confirmed steps may wait to be shown before the entity jumps ahead to catch up.</param>
    public MotionRules(TimeSpan stepDuration, int maxStepsBehind)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(stepDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxStepsBehind, 1);
        StepDuration = stepDuration;
        MaxStepsBehind = maxStepsBehind;
    }

    public TimeSpan StepDuration { get; }

    public int MaxStepsBehind { get; }
}
