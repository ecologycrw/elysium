using Elysium.Core.Protocol;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// How a map actor talks back to connected players, by entity ID.
/// Calls never block: the message is queued and sent outside the tick.
/// </summary>
public interface IClientOutbox
{
    /// <summary>Queues <paramref name="message"/> for the player that controls <paramref name="recipient"/>.</summary>
    void Send(EntityId recipient, IMessage message);

    /// <summary>Closes the connection of the player that controls <paramref name="client"/>.</summary>
    void Disconnect(EntityId client, string reason);
}
