using Elysium.Classic.Shared.Grid;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Server to client: the move request to <paramref name="Destination"/> was refused, and the character did not move.
/// A value type, like <see cref="EntityMoved"/>: reading it leaves nothing for the garbage collector.
/// </summary>
public readonly record struct MoveRejected(TilePosition Destination, MoveRejectReason Reason) : IMessage<MoveRejected>
{
    public static ushort TypeId => ClassicMessageTypes.MoveRejected;

    public ushort MessageType => TypeId;

    public static MoveRejected Read(ref MessageReader reader)
    {
        var destination = reader.ReadTile();
        var reason = (MoveRejectReason)reader.ReadByte();
        if (!Enum.IsDefined(reason))
        {
            throw new ProtocolException($"Unknown move reject reason {(byte)reason}.");
        }

        return new MoveRejected(destination, reason);
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteTile(Destination);
        writer.WriteByte((byte)Reason);
    }
}
