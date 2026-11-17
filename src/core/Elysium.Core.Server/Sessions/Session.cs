using System.Threading.Channels;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// One connected client. Outgoing frames wait in a queue and are written by a single writer loop,
/// so a map actor that sends never waits for the network.
/// </summary>
internal sealed class Session : IDisposable
{
    private readonly Stream _stream;
    private readonly Channel<byte[]> _outgoing = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    private readonly CancellationTokenSource _closed = new();

    public Session(EntityId id, Stream stream)
    {
        Id = id;
        _stream = stream;
    }

    public EntityId Id { get; }

    /// <summary>Cancelled when the session is closed, by either side.</summary>
    public CancellationToken Closed => _closed.Token;

    public string? CloseReason { get; private set; }

    /// <summary>Queues a frame. Ignored after the session is closed.</summary>
    public void Enqueue(byte[] frame) => _outgoing.Writer.TryWrite(frame);

    /// <summary>
    /// Stops accepting frames. The writer loop sends what is already queued and then closes the stream.
    /// </summary>
    public void Close(string reason)
    {
        if (_outgoing.Writer.TryComplete())
        {
            CloseReason = reason;
        }
    }

    /// <summary>Writes queued frames until the session is closed, then closes the stream.</summary>
    public async Task RunWriterAsync()
    {
        try
        {
            while (await _outgoing.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_outgoing.Reader.TryRead(out var frame))
                {
                    await _stream.WriteAsync(frame).ConfigureAwait(false);
                }

                await _stream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            Close("the connection was lost while sending");
        }
        finally
        {
            await _closed.CancelAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose() => _closed.Dispose();
}
