using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Server to client: an entity left the view (it walked away, or left the map).
/// </summary>
public sealed record EntityDisappeared(EntityId Id) : IMessage<EntityDisappeared>
{
    public static ushort TypeId => ClassicMessageTypes.EntityDisappeared;

    public ushort MessageType => TypeId;

    public static EntityDisappeared Read(ref MessageReader reader) => new(reader.ReadEntityId());

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteEntityId(Id);
    }
}
