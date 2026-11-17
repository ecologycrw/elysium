using Elysium.Core.Protocol;

namespace Elysium.Core.Login;

/// <summary>
/// Server to client: the login worked. <paramref name="EntityId"/> is the player's own character.
/// </summary>
public sealed record LoginAccepted(EntityId EntityId) : IMessage<LoginAccepted>
{
    public static ushort TypeId => CoreMessageTypes.LoginAccepted;

    public ushort MessageType => TypeId;

    public static LoginAccepted Read(ref MessageReader reader) => new(reader.ReadEntityId());

    public void Write(MessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteEntityId(EntityId);
    }
}
