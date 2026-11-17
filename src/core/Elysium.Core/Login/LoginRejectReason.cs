namespace Elysium.Core.Login;

/// <summary>
/// Why the server refused a login.
/// </summary>
public enum LoginRejectReason : byte
{
    /// <summary>The client uses another protocol version.</summary>
    VersionMismatch = 1,

    /// <summary>The character name is empty, too long or has characters that are not allowed.</summary>
    InvalidName = 2,

    /// <summary>A character with this name is already online.</summary>
    NameTaken = 3,
}
