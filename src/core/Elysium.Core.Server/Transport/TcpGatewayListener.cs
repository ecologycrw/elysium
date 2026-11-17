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
    private int _lastConnection;

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
            var number = Interlocked.Increment(ref _lastConnection);
            _connections[number] = RunAsync(number, client);
        }
    }

    private async Task RunAsync(int number, TcpClient client)
    {
        await Task.Yield();
        using (client)
        {
            await _gateway.RunConnectionAsync(client.GetStream(), _stopping.Token).ConfigureAwait(false);
        }

        _connections.TryRemove(number, out _);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for connections on {EndPoint}")]
    private partial void LogListening(IPEndPoint endPoint);
}
