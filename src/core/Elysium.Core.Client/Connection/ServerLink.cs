using System.Collections.Concurrent;
using System.Threading.Channels;
using Elysium.Core.Protocol;

namespace Elysium.Core.Client.Connection;

/// <summary>
/// The game's link to the server once the player is logged in. Two background loops talk to the network:
/// one receives messages into a queue, the other sends what the game asked for. The frame loop never waits
/// for the network: it takes what already arrived (<see cref="TryTakeMessage"/>) and asks to send (<see cref="Send"/>).
/// When the connection ends, <see cref="IsConnected"/> turns false and <see cref="LostReason"/> says why;
/// messages that arrived before that can still be taken.
/// </summary>
public sealed class ServerLink : IAsyncDisposable
{
    private readonly GameConnection _connection;
    private readonly ConcurrentQueue<Frame> _received = new();
    private readonly Channel<IMessage> _outgoing = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
    });

    private readonly CancellationTokenSource _stopping = new();
    private readonly Task _receiving;
    private readonly Task _sending;
    private string? _lostReason;

    /// <summary>Starts the background loops on a connection that has already logged in. The link owns the connection from now on.</summary>
    public ServerLink(GameConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _receiving = Task.Run(ReceiveLoopAsync);
        _sending = Task.Run(SendLoopAsync);
    }

    /// <summary>False once the connection ended, for any reason.</summary>
    public bool IsConnected => LostReason is null;

    /// <summary>Why the connection ended, or null while it is up. The first reason wins.</summary>
    public string? LostReason => Volatile.Read(ref _lostReason);

    /// <summary>Takes the oldest message that arrived, if any. Never waits.</summary>
    public bool TryTakeMessage(out Frame frame) => _received.TryDequeue(out frame);

    /// <summary>Asks to send a message. Never waits; ignored once the connection ended.</summary>
    public void Send(IMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _outgoing.Writer.TryWrite(message);
    }

    /// <summary>Stops both loops and closes the connection.</summary>
    public async ValueTask DisposeAsync()
    {
        MarkLost("the game closed the connection");
        await _stopping.CancelAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        await Task.WhenAll(_receiving, _sending).ConfigureAwait(false);
        _stopping.Dispose();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (await _connection.ReceiveAsync(_stopping.Token).ConfigureAwait(false) is { } frame)
            {
                _received.Enqueue(frame);
            }

            MarkLost("the server closed the connection");
        }
        catch (ProtocolException exception)
        {
            MarkLost($"the server sent an invalid message: {exception.Message}");
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            MarkLost("the game closed the connection");
        }
    }

    private async Task SendLoopAsync()
    {
        try
        {
            while (await _outgoing.Reader.WaitToReadAsync(_stopping.Token).ConfigureAwait(false))
            {
                while (_outgoing.Reader.TryRead(out var message))
                {
                    await _connection.SendAsync(message, _stopping.Token).ConfigureAwait(false);
                }
            }
        }
        catch (IOException exception)
        {
            MarkLost($"the connection was lost while sending: {exception.Message}");
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            // Stopping, or the receive loop already found the connection closed.
        }
    }

    /// <summary>Records why the link ended (the first reason wins) and stops accepting messages to send.</summary>
    private void MarkLost(string reason)
    {
        Interlocked.CompareExchange(ref _lostReason, reason, null);
        _outgoing.Writer.TryComplete();
    }
}
