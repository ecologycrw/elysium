using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Elysium.Core.Server.Sessions;
using Microsoft.Extensions.Logging;

namespace Elysium.Core.Server.Transport;

/// <summary>
/// The TCP transport (ADR 0005): accepts connections and hands each stream to the <see cref="Gateway"/>.
/// Another transport (TLS, WebSocket) is another class that does the same with its own stream.
/// </summary>
public sealed partial class TcpGatewayListener : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Gateway _gateway;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _stopping = new();
    private readonly ConcurrentDictionary<int, Task> _connections = new();
    private readonly Task _acceptLoop;
    private int _lastConnectionNumber;

    private TcpGatewayListener(TcpListener listener, Gateway gateway, ILogger logger)
    {
        _listener = listener;
        _gateway = gateway;
        _logger = logger;
        _acceptLoop = AcceptLoopAsync();
    }

    /// <summary>The address and port the server is listening on (useful when port 0 asked the system for a free one).</summary>
    public IPEndPoint LocalEndPoint => (IPEndPoint)_listener.LocalEndpoint;

    public static TcpGatewayListener Start(Gateway gateway, IPEndPoint endPoint, ILogger<TcpGatewayListener> logger)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(logger);
        var listener = new TcpListener(endPoint);
        listener.Start();
        var started = new TcpGatewayListener(listener, gateway, logger);
        started.LogListening(started.LocalEndPoint);
        return started;
    }

    /// <summary>Stops accepting, closes every connection and waits for them to finish.</summary>
    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync().ConfigureAwait(false);
        _listener.Stop();
        await _acceptLoop.ConfigureAwait(false);
        await Task.WhenAll(_connections.Values).ConfigureAwait(false);
        _listener.Dispose();
        _stopping.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        await Task.Yield();
        while (!_stopping.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stopping.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException)
            {
                break;
            }

            client.NoDelay = true;
            var connectionNumber = ++_lastConnectionNumber;

            // Registered before it starts, so a connection that ends at once is still removed from the set.
            var connection = new Task<Task>(() => RunAsync(connectionNumber, client));
            _connections[connectionNumber] = connection.Unwrap();
            connection.Start(TaskScheduler.Default);
        }
    }

    private async Task RunAsync(int connectionNumber, TcpClient client)
    {
        try
        {
            await _gateway.RunConnectionAsync(client.GetStream(), _stopping.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The last guard of a connection: an unexpected error is logged, not lost, and does not stop the shutdown.
            LogConnectionFailed(connectionNumber, exception);
        }
        finally
        {
            client.Dispose();
            _connections.TryRemove(connectionNumber, out _);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for connections on {EndPoint}")]
    private partial void LogListening(IPEndPoint endPoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Connection {ConnectionNumber} failed unexpectedly")]
    private partial void LogConnectionFailed(int connectionNumber, Exception exception);
}
