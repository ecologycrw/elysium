using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Server to client: the player's own character entered the map at <paramref name="Position"/>.
/// </summary>
public sealed record EnteredMap(EntityId Self, TilePosition Position) : IMessage<EnteredMap>
{
    public static ushort TypeId => ClassicMessageTypes.EnteredMap;

    public ushort MessageType => TypeId;

    public static EnteredMap Read(ref MessageReader reader)
    {
        var self = reader.ReadEntityId();
        var position = reader.ReadTile();
        return new EnteredMap(self, position);
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteEntityId(Self);
        writer.WriteTile(Position);
    }
}
