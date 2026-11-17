namespace Elysium.Core.Server.Tests.Sessions;

/// <summary>
/// A client connection played from a script: what the client sends, how its side ends,
/// and what happens to what the server writes. Lets a test end a connection in each possible way.
/// </summary>
internal sealed class ScriptedStream(byte[] clientSends, ScriptedStream.InputEnd inputEnd, ScriptedStream.Writes writes) : Stream
{
    private readonly MemoryStream _received = new();
    private int _readPosition;

    /// <summary>What the client does after sending everything in its script.</summary>
    public enum InputEnd
    {
        /// <summary>Closes the connection normally.</summary>
        Closes,

        /// <summary>The connection breaks (an I/O error, like a cable pulled out).</summary>
        Breaks,

        /// <summary>Stays connected and silent.</summary>
        StaysOpen,
    }

    /// <summary>What happens to what the server writes.</summary>
    public enum Writes
    {
        /// <summary>The client receives it.</summary>
        AreReceived,

        /// <summary>Writing fails with an I/O error.</summary>
        Fail,

        /// <summary>The client never reads, so every write waits.</summary>
        Stall,
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;

    public override bool CanSeek => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>Everything the client received from the server, once the connection is over.</summary>
    public byte[] Received => _received.ToArray();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_readPosition < clientSends.Length)
        {
            var count = Math.Min(buffer.Length, clientSends.Length - _readPosition);
            clientSends.AsMemory(_readPosition, count).CopyTo(buffer);
            _readPosition += count;
            return count;
        }

        switch (inputEnd)
        {
            case InputEnd.Closes:
                return 0;
            case InputEnd.Breaks:
                throw new IOException("The connection broke.");
            default:
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        switch (writes)
        {
            case Writes.AreReceived:
                _received.Write(buffer.Span);
                break;
            case Writes.Fail:
                throw new IOException("The connection broke while sending.");
            default:
                await Task.Delay(Timeout.Infinite, cancellationToken);
                break;
        }
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Only the async API is scripted.");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Only the async API is scripted.");

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _received.Dispose();
        }

        base.Dispose(disposing);
    }
}
