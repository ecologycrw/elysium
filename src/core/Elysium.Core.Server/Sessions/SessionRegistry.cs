using System.Collections.Concurrent;
using Elysium.Core.Protocol;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// The logged-in players, by entity ID and by name. It is the <see cref="IClientOutbox"/> of the map actors:
/// they send by ID and never touch a connection. Safe to use from any thread (lock-free collections only).
/// </summary>
public sealed class SessionRegistry : IClientOutbox
{
    private readonly ConcurrentDictionary<EntityId, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, EntityId> _names = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _sessions.Count;

    public void Send(EntityId recipient, IMessage message)
    {
        if (_sessions.TryGetValue(recipient, out var session))
        {
            session.Enqueue(FrameIo.Encode(message));
        }
    }

    public void Disconnect(EntityId client, string reason)
    {
        if (_sessions.TryGetValue(client, out var session))
        {
            session.Abort(reason);
        }
    }

    /// <summary>Registers a session under its name. Returns false when the name is already online (names ignore case).</summary>
    internal bool TryRegister(Session session, string name)
    {
        if (!_names.TryAdd(name, session.Id))
        {
            return false;
        }

        _sessions[session.Id] = session;
        return true;
    }

    internal void Unregister(Session session, string name)
    {
        _sessions.TryRemove(session.Id, out _);
        _names.TryRemove(new KeyValuePair<string, EntityId>(name, session.Id));
    }
}
