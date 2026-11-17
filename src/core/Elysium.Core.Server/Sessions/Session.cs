using System.Threading.Channels;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// One connected client. Outgoing frames wait in a bounded queue and are written by a single writer loop,
/// so a map actor that sends never waits for the network. A client that does not read fast enough fills the
/// queue and is aborted, instead of making the server's memory grow.
/// </summary>
internal sealed class Session : IDisposable
{
    private readonly Stream _stream;
    private readonly Channel<byte[]> _outgoing;
    private readonly CancellationTokenSource _abort;
    private readonly CancellationTokenSource _closed = new();
    private string? _closeReason;

    public Session(EntityId id, Stream stream, int maxQueuedFrames, CancellationToken serverStopping)
    {
        Id = id;
        _stream = stream;
        _outgoing = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(maxQueuedFrames)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _abort = CancellationTokenSource.CreateLinkedTokenSource(serverStopping);
    }

    public EntityId Id { get; }

    /// <summary>Cancelled when the writer loop has ended and the stream is closed.</summary>
    public CancellationToken Closed => _closed.Token;

    /// <summary>Why the session closed: the first reason given wins.</summary>
    public string? CloseReason => Volatile.Read(ref _closeReason);

    /// <summary>Queues a frame. Ignored after the session is closed; aborts the session when the queue is full.</summary>
    public void Enqueue(byte[] frame)
    {
        if (!_outgoing.Writer.TryWrite(frame) && CloseReason is null)
        {
            Abort("the client is not reading its messages fast enough");
        }
    }

    /// <summary>Stops accepting frames. The writer loop still sends what is queued (for example, a login refusal).</summary>
    public void Close(string reason)
    {
        TrySetCloseReason(reason);
        _outgoing.Writer.TryComplete();
    }

    /// <summary>Stops at once, dropping queued frames and interrupting a write that is waiting for the client.</summary>
    public void Abort(string reason)
    {
        Close(reason);
        try
        {
            _abort.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The session already ended.
        }
    }

    /// <summary>Writes queued frames until the session is closed or aborted, then closes the stream.</summary>
    public async Task RunWriterAsync()
    {
        try
        {
            while (await _outgoing.Reader.WaitToReadAsync(_abort.Token).ConfigureAwait(false))
            {
                while (_outgoing.Reader.TryRead(out var frame))
                {
                    await _stream.WriteAsync(frame, _abort.Token).ConfigureAwait(false);
                }

                await _stream.FlushAsync(_abort.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException)
        {
            TrySetCloseReason("the connection was lost while sending");
        }
        finally
        {
            await _closed.CancelAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _abort.Dispose();
        _closed.Dispose();
    }

    private void TrySetCloseReason(string reason) => Interlocked.CompareExchange(ref _closeReason, reason, null);
}
