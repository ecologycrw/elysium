using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Server to client: another entity came into view.
/// </summary>
public sealed record EntityAppeared(EntityId Id, string Name, TilePosition Position) : IMessage<EntityAppeared>
{
    public static ushort TypeId => ClassicMessageTypes.EntityAppeared;

    public ushort MessageType => TypeId;

    public static EntityAppeared Read(ref MessageReader reader)
    {
        var id = reader.ReadEntityId();
        var name = reader.ReadString();
        var position = reader.ReadTile();
        return new EntityAppeared(id, name, position);
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteEntityId(Id);
        writer.WriteString(Name);
        writer.WriteTile(Position);
    }
}
