using System.Threading.Channels;

namespace Elysium.Core.Server.Actors;

/// <summary>
/// The message queue of an actor. Any thread may post; only the actor takes, inside its tick (INV-03).
/// Posting never blocks.
/// </summary>
public sealed class Mailbox<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public void Post(T message)
    {
        if (!_channel.Writer.TryWrite(message))
        {
            throw new InvalidOperationException("The mailbox is closed.");
        }
    }

    /// <summary>Takes the oldest message, if there is one. Only the owning actor calls this.</summary>
    public bool TryTake(out T message) => _channel.Reader.TryRead(out message!);
}
