namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Why the server refused a move request.
/// </summary>
public enum MoveRejectReason : byte
{
    /// <summary>The destination is outside the map.</summary>
    OutOfMap = 1,

    /// <summary>The destination tile is blocked.</summary>
    Blocked = 2,

    /// <summary>There is no path to the destination within the path length limit.</summary>
    Unreachable = 3,
}
