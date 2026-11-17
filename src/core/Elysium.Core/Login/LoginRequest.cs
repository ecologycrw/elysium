using Elysium.Core.Protocol;

namespace Elysium.Core.Login;

/// <summary>
/// Client to server: the first message of every connection.
/// The protocol version is always the first field, so a server can read it even when the rest
/// of the message comes from an incompatible version.
/// </summary>
public sealed record LoginRequest(ushort ProtocolVersion, string CharacterName) : IMessage<LoginRequest>
{
    public static ushort TypeId => CoreMessageTypes.LoginRequest;

    public ushort MessageType => TypeId;

    public static LoginRequest Read(ref MessageReader reader)
    {
        var version = reader.ReadUInt16();
        var name = reader.ReadString();
        return new LoginRequest(version, name);
    }

    /// <summary>
    /// Reads only the protocol version from a login request payload.
    /// Throws <see cref="ProtocolException"/> when the payload is too short to hold it.
    /// </summary>
    public static ushort ReadProtocolVersion(ReadOnlySpan<byte> payload)
    {
        var reader = new MessageReader(payload);
        return reader.ReadUInt16();
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteUInt16(ProtocolVersion);
        writer.WriteString(CharacterName);
    }
}
