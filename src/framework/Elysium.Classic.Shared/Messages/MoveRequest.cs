using Elysium.Classic.Shared.Grid;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Client to server: "I want to walk to this tile". The server finds the path and moves the character.
/// </summary>
public sealed record MoveRequest(TilePosition Destination) : IMessage<MoveRequest>
{
    public static ushort TypeId => ClassicMessageTypes.MoveRequest;

    public ushort MessageType => TypeId;

    public static MoveRequest Read(ref MessageReader reader) => new(reader.ReadTile());

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteTile(Destination);
    }
}
