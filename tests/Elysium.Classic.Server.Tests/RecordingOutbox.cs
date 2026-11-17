using Elysium.Core;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Tests;

/// <summary>
/// Remembers every message and disconnection, in order, so tests can check exactly what each player received.
/// </summary>
internal sealed class RecordingOutbox : IClientOutbox
{
    public List<(EntityId Recipient, IMessage Message)> Sent { get; } = [];

    public List<(EntityId Client, string Reason)> Disconnected { get; } = [];

    public void Send(EntityId recipient, IMessage message) => Sent.Add((recipient, message));

    public void Disconnect(EntityId client, string reason) => Disconnected.Add((client, reason));

    public List<IMessage> To(EntityId recipient) =>
        Sent.Where(entry => entry.Recipient == recipient).Select(entry => entry.Message).ToList();

    public List<T> To<T>(EntityId recipient) => To(recipient).OfType<T>().ToList();

    public void Clear()
    {
        Sent.Clear();
        Disconnected.Clear();
    }
}
