using System.Net.Sockets;
using Elysium.Core.Login;
using Elysium.Core.Protocol;

namespace Elysium.Core.Client.Connection;

/// <summary>
/// The client side of a connection to the game server: login, then send and receive messages.
/// The bot uses it now, and the graphical client will use it from slice 1.
/// It works over any <see cref="Stream"/>, so the transport (TCP now, TLS later) can change without changing it.
/// Not thread-safe: one caller sends and one caller receives.
/// </summary>
public sealed class GameConnection : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly TcpClient? _tcpClient;

    public GameConnection(Stream stream)
        : this(stream, tcpClient: null)
    {
    }

    private GameConnection(Stream stream, TcpClient? tcpClient)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _tcpClient = tcpClient;
    }

    /// <summary>Opens a TCP connection to the server.</summary>
    public static async Task<GameConnection> ConnectTcpAsync(string host, int port, CancellationToken cancellationToken)
    {
        var client = new TcpClient { NoDelay = true };
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            return new GameConnection(client.GetStream(), client);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sends the login request and waits for the answer. After a refusal the server closes the connection.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string characterName, CancellationToken cancellationToken, ushort protocolVersion = ProtocolVersion.Current)
    {
        await SendAsync(new LoginRequest(protocolVersion, characterName), cancellationToken).ConfigureAwait(false);
        var answer = await ReceiveAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ProtocolException("The server closed the connection without answering the login.");

        return answer.Type switch
        {
            CoreMessageTypes.LoginAccepted => new LoginResult(answer.Decode<LoginAccepted>().EntityId, null),
            CoreMessageTypes.LoginRejected => new LoginResult(null, answer.Decode<LoginRejected>().Reason),
            _ => throw new ProtocolException($"Expected a login answer, got message type {answer.Type}."),
        };
    }

    public async Task SendAsync(IMessage message, CancellationToken cancellationToken)
    {
        await _stream.WriteAsync(FrameIo.Encode(message), cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits for the next message. Returns <see langword="null"/> when the server closed the connection.</summary>
    public async ValueTask<Frame?> ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await FrameIo.ReadAsync(_stream, FrameIo.MaxBodySize, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
        _tcpClient?.Dispose();
    }
}
