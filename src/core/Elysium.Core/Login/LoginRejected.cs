using Elysium.Core.Protocol;

namespace Elysium.Core.Login;

/// <summary>
/// Server to client: the login was refused. The server closes the connection right after sending it.
/// </summary>
public sealed record LoginRejected(LoginRejectReason Reason) : IMessage<LoginRejected>
{
    public static ushort TypeId => CoreMessageTypes.LoginRejected;

    public ushort MessageType => TypeId;

    public static LoginRejected Read(ref MessageReader reader)
    {
        var reason = (LoginRejectReason)reader.ReadByte();
        if (!Enum.IsDefined(reason))
        {
            throw new ProtocolException($"Unknown login reject reason {(byte)reason}.");
        }

        return new LoginRejected(reason);
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte((byte)Reason);
    }
}
