using Elysium.Classic.Shared.Grid;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Server to client: an entity in view (or the player's own character) stepped to <paramref name="Position"/>.
/// A value type: clients read one every step, and reading it leaves nothing for the garbage collector.
/// </summary>
public readonly record struct EntityMoved(EntityId Id, TilePosition Position) : IMessage<EntityMoved>
{
    public static ushort TypeId => ClassicMessageTypes.EntityMoved;

    public ushort MessageType => TypeId;

    public static EntityMoved Read(ref MessageReader reader)
    {
        var id = reader.ReadEntityId();
        var position = reader.ReadTile();
        return new EntityMoved(id, position);
    }

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteEntityId(Id);
        writer.WriteTile(Position);
    }
}
