namespace Elysium.Core.Protocol;

/// <summary>
/// The version of the network protocol (INV-11). Any incompatible change to a message or to the
/// frame format increases it, and a client with another version is refused at login.
/// </summary>
public static class ProtocolVersion
{
    public const ushort Current = 1;
}
