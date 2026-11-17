using Elysium.Core.Login;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Sessions;

namespace Elysium.Core.Server.Tests.Sessions;

/// <summary>
/// How a connection ends: the server always releases it at once, takes the player out of the world,
/// and tells the operator why in the log (the "left: ..." line the server prints).
/// </summary>
public sealed class GatewayConnectionEndTests : IDisposable
{
    private static readonly EntityId FirstPlayer = new(1);
    private static readonly TimeSpan EndsWithin = TimeSpan.FromSeconds(5);

    private readonly FakeWorld _world = new();
    private readonly SessionRegistry _registry = new();
    private readonly RecordingLogger<Gateway> _log = new();
    private readonly CancellationTokenSource _stopping = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
    }

    [Fact]
    public async Task A_connection_that_ends_before_the_login_is_released_at_once()
    {
        using var client = new ScriptedStream([], ScriptedStream.InputEnd.Closes, ScriptedStream.Writes.AreReceived);

        await RunAsync(client).WaitAsync(EndsWithin, Token);

        Assert.Empty(_world.Events);
        Assert.Empty(client.Received);
    }

    [Fact]
    public async Task When_the_client_closes_the_connection_the_player_leaves_and_the_log_says_why()
    {
        using var client = new ScriptedStream(Login("Ana"), ScriptedStream.InputEnd.Closes, ScriptedStream.Writes.AreReceived);

        await RunAsync(client).WaitAsync(EndsWithin, Token);

        Assert.Equal(FrameIo.Encode(new LoginAccepted(FirstPlayer)), client.Received);
        Assert.Equal([$"joined {FirstPlayer} Ana", $"left {FirstPlayer}"], _world.Events);
        Assert.Contains($"Information: Entity {FirstPlayer} left: the client closed the connection", _log.Lines);
    }

    [Fact]
    public async Task When_the_connection_breaks_the_player_leaves_and_the_log_says_why()
    {
        using var client = new ScriptedStream(Login("Ana"), ScriptedStream.InputEnd.Breaks, ScriptedStream.Writes.AreReceived);

        await RunAsync(client).WaitAsync(EndsWithin, Token);

        Assert.Equal([$"joined {FirstPlayer} Ana", $"left {FirstPlayer}"], _world.Events);
        Assert.Contains($"Information: Entity {FirstPlayer} left: the connection was closed", _log.Lines);
    }

    [Fact]
    public async Task When_sending_fails_the_player_leaves_and_the_log_says_why()
    {
        using var client = new ScriptedStream(Login("Ana"), ScriptedStream.InputEnd.StaysOpen, ScriptedStream.Writes.Fail);

        await RunAsync(client).WaitAsync(EndsWithin, Token);

        Assert.Equal([$"joined {FirstPlayer} Ana", $"left {FirstPlayer}"], _world.Events);
        Assert.Contains($"Information: Entity {FirstPlayer} left: the connection was lost while sending", _log.Lines);
    }

    [Fact]
    public async Task A_client_that_stops_reading_is_disconnected_and_the_log_says_why()
    {
        using var client = new ScriptedStream(Login("Ana"), ScriptedStream.InputEnd.StaysOpen, ScriptedStream.Writes.Stall);
        var run = RunAsync(client, maxQueuedFrames: 2);
        await _world.WaitForAsync($"joined {FirstPlayer} Ana");

        for (var i = 0; i < 5; i++)
        {
            _registry.Send(FirstPlayer, new LoginAccepted(FirstPlayer));
        }

        await run.WaitAsync(EndsWithin, Token);
        Assert.Equal($"left {FirstPlayer}", _world.Events[^1]);
        Assert.Contains($"Information: Entity {FirstPlayer} left: the client is not reading its messages fast enough", _log.Lines);
    }

    [Fact]
    public async Task A_message_before_the_login_is_logged_as_a_protocol_violation()
    {
        using var client = new ScriptedStream(
            FrameIo.Encode(new LoginAccepted(FirstPlayer)), ScriptedStream.InputEnd.StaysOpen, ScriptedStream.Writes.AreReceived);

        await RunAsync(client).WaitAsync(EndsWithin, Token);

        Assert.Empty(_world.Events);
        var warning = Assert.Single(_log.Lines, line => line.StartsWith("Warning:", StringComparison.Ordinal));
        Assert.Contains(
            $"Entity {FirstPlayer} sent an invalid message and was disconnected: The first message must be a login request, got message type {CoreMessageTypes.LoginAccepted}.",
            warning,
            StringComparison.Ordinal);
    }

    private static byte[] Login(string name) => FrameIo.Encode(new LoginRequest(ProtocolVersion.Current, name));

    private Task RunAsync(ScriptedStream client, int maxQueuedFrames = 16)
    {
        var gateway = new Gateway(_registry, _world, new GatewayOptions(maxFrameBodySize: 64, maxNameLength: 8, maxQueuedFrames), _log);
        return gateway.RunConnectionAsync(client, _stopping.Token);
    }
}
