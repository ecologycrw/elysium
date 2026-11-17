namespace Elysium.Checks;

/// <summary>
/// One problem found by a check.
/// </summary>
internal sealed record Finding(string Check, string Path, string Message)
{
    public override string ToString() => $"[{Check}] {Path}: {Message}";
}
