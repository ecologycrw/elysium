namespace Elysium.Core.Protocol;

/// <summary>
/// The version of the network protocol (INV-11). Any incompatible change to a message increases it,
/// and the server refuses a client with another version at login.
/// The version travels only in the login, so two things never change between versions, or a client of another
/// version could not even reach the version check: the frame header (2-byte length, 2-byte type, little-endian)
/// and the start of the login request (type 1, with the version as its first 2-byte field).
/// Tests pin both: FrameIoTests and LoginMessageTests.
/// </summary>
public static class ProtocolVersion
{
    public const ushort Current = 1;
}
