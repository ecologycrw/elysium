using System.Net;
using System.Net.Sockets;

namespace Elysium.Core.Client.Tests.Connection;

/// <summary>
/// A real TCP connection on this machine: the client side for the code under test,
/// and the server side for the test to play the server.
/// </summary>
internal sealed class LoopbackServer : IAsyncDisposable
{
    private readonly TcpClient _server;
    private readonly TcpClient _client;

    private LoopbackServer(TcpClient server, TcpClient client)
    {
        _server = server;
        _client = client;
    }

    /// <summary>What the server side writes and reads.</summary>
    public NetworkStream ServerStream => _server.GetStream();

    /// <summary>The client side, for the code under test.</summary>
    public NetworkStream ClientStream => _client.GetStream();

    public static async Task<LoopbackServer> ConnectAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var client = new TcpClient { NoDelay = true };
            var accepting = listener.AcceptTcpClientAsync(cancellationToken);
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port, cancellationToken);
            return new LoopbackServer(await accepting, client);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>The server closes its side of the connection.</summary>
    public void CloseServerSide() => _server.Close();

    public ValueTask DisposeAsync()
    {
        _server.Dispose();
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
