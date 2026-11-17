using Elysium.Core.Login;
using Elysium.Core.Protocol;
using Microsoft.Extensions.Logging;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// Runs every client connection: reads messages outside the tick, does the login, and then forwards the
/// player's messages to the <see cref="IWorld"/>, which queues them for its actor.
/// Anything invalid (a message before the login, a frame over the size limit, a truncated message)
/// closes only that connection. The client always receives <see cref="LoginAccepted"/> before anything
/// the world sends.
/// </summary>
public sealed partial class Gateway
{
    private readonly SessionRegistry _registry;
    private readonly IWorld _world;
    private readonly GatewayOptions _options;
    private readonly ILogger _logger;
    private uint _lastEntityId;

    public Gateway(SessionRegistry registry, IWorld world, GatewayOptions options, ILogger<Gateway> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _registry = registry;
        _world = world;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Runs one connection until the client leaves, is disconnected, or <paramref name="stopping"/> is cancelled.
    /// The transport (TCP now) hands over the connected stream; this method owns it from then on.
    /// </summary>
    public async Task RunConnectionAsync(Stream stream, CancellationToken stopping)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var id = new EntityId(Interlocked.Increment(ref _lastEntityId));
        using var session = new Session(id, stream, _options.MaxQueuedFramesPerClient, stopping);
        var writer = session.RunWriterAsync();
        using var reading = CancellationTokenSource.CreateLinkedTokenSource(stopping, session.Closed);

        try
        {
            var name = await LoginAsync(session, stream, reading.Token).ConfigureAwait(false);
            if (name is not null)
            {
                await PlayAsync(session, stream, name, reading.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (IsConnectionEnd(exception))
        {
            EndSession(session, exception);
        }
        finally
        {
            await writer.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the first message. Returns the registered name, or null when the login was refused
    /// (the refusal is queued and the session is closed).
    /// </summary>
    private async Task<string?> LoginAsync(Session session, Stream stream, CancellationToken cancellationToken)
    {
        var first = await FrameIo.ReadAsync(stream, _options.MaxFrameBodySize, cancellationToken).ConfigureAwait(false);
        if (first is not { } frame)
        {
            session.Close("the client left before the login");
            return null;
        }

        if (frame.Type != LoginRequest.TypeId)
        {
            throw new ProtocolException($"The first message must be a login request, got message type {frame.Type}.");
        }

        if (LoginRequest.ReadProtocolVersion(frame.Payload.Span) != ProtocolVersion.Current)
        {
            return Refuse(session, LoginRejectReason.VersionMismatch);
        }

        var request = frame.Decode<LoginRequest>();
        if (!IsValidName(request.CharacterName))
        {
            return Refuse(session, LoginRejectReason.InvalidName);
        }

        if (!_registry.TryRegister(session, request.CharacterName))
        {
            return Refuse(session, LoginRejectReason.NameTaken);
        }

        session.Enqueue(FrameIo.Encode(new LoginAccepted(session.Id)));
        return request.CharacterName;
    }

    /// <summary>
    /// The player is in the world: forwards every message until the connection ends, then takes the player out.
    /// The name is free again before the world hears that the player left.
    /// </summary>
    private async Task PlayAsync(Session session, Stream stream, string name, CancellationToken cancellationToken)
    {
        _world.PlayerJoined(session.Id, name);
        LogJoined(session.Id, name);
        try
        {
            while (await FrameIo.ReadAsync(stream, _options.MaxFrameBodySize, cancellationToken).ConfigureAwait(false) is { } frame)
            {
                _world.PlayerFrame(session.Id, frame);
            }

            session.Close("the client closed the connection");
        }
        catch (Exception exception) when (IsConnectionEnd(exception))
        {
            EndSession(session, exception);
        }
        finally
        {
            _registry.Unregister(session, name);
            _world.PlayerLeft(session.Id);
            LogLeft(session.Id, session.CloseReason ?? "unknown");
        }
    }

    private string? Refuse(Session session, LoginRejectReason reason)
    {
        LogLoginRefused(session.Id, reason);
        session.Enqueue(FrameIo.Encode(new LoginRejected(reason)));
        session.Close($"login refused: {reason}");
        return null;
    }

    private void EndSession(Session session, Exception exception)
    {
        if (exception is ProtocolException protocolError)
        {
            LogProtocolViolation(session.Id, protocolError.Message);
            session.Abort(protocolError.Message);
        }
        else
        {
            session.Close("the connection was closed");
        }
    }

    private static bool IsConnectionEnd(Exception exception) =>
        exception is ProtocolException or IOException or ObjectDisposedException or OperationCanceledException;

    private bool IsValidName(string name) =>
        name.Length >= 1 && name.Length <= _options.MaxNameLength && name.All(char.IsAsciiLetterOrDigit);

    [LoggerMessage(Level = LogLevel.Information, Message = "Player {Name} joined as entity {Id}")]
    private partial void LogJoined(EntityId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Entity {Id} left: {Reason}")]
    private partial void LogLeft(EntityId id, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login refused for entity {Id}: {Reason}")]
    private partial void LogLoginRefused(EntityId id, LoginRejectReason reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Entity {Id} sent an invalid message and was disconnected: {Detail}")]
    private partial void LogProtocolViolation(EntityId id, string detail);
}
