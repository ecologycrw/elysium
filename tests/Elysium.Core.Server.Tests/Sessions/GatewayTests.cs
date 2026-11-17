using System.Net;
using System.Net.Sockets;
using Elysium.Core.Client.Connection;
using Elysium.Core.Login;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Sessions;
using Elysium.Core.Server.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Core.Server.Tests.Sessions;

public sealed class GatewayTests : IAsyncLifetime
{
    private const int MaxFrameBodySize = 64;
    private const ushort SomeGameMessage = 1000;

    private readonly FakeWorld _world = new();
    private readonly SessionRegistry _registry = new();
    private TcpGatewayListener? _listener;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private int Port => _listener!.LocalEndPoint.Port;

    public ValueTask InitializeAsync()
    {
        var gateway = new Gateway(_registry, _world, Options(maxQueuedFramesPerClient: 16), NullLogger<Gateway>.Instance);
        _listener = TcpGatewayListener.Start(gateway, new IPEndPoint(IPAddress.Loopback, 0), NullLogger<TcpGatewayListener>.Instance);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is not null)
        {
            await _listener.DisposeAsync();
        }
    }

    [Fact]
    [Trait("CA", "0000-CA-01")]
    public async Task A_valid_login_is_accepted_with_an_entity_id_and_the_player_joins_the_world()
    {
        await using var client = await ConnectAsync();

        var result = await client.LoginAsync("Ana", Token);

        Assert.True(result.IsAccepted);
        var id = result.EntityId!.Value;
        await _world.WaitForAsync($"joined {id} Ana");
    }

    [Fact]
    [Trait("CA", "0000-CA-02")]
    public async Task Another_protocol_version_is_refused_and_the_connection_closed()
    {
        await using var client = await ConnectAsync();

        await client.SendAsync(new LoginRequest(ProtocolVersion.Current + 1, "Ana"), Token);
        var answer = await client.ReceiveAsync(Token);

        Assert.Equal(new LoginRejected(LoginRejectReason.VersionMismatch), answer!.Value.Decode<LoginRejected>());
        Assert.Null(await client.ReceiveAsync(Token));
        Assert.Empty(_world.Events);
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public async Task A_client_that_stops_reading_is_disconnected_instead_of_filling_the_server_memory()
    {
        using var raw = await ConnectRawAsync(receiveBufferSize: 1024);
        await using var client = new GameConnection(raw.GetStream());
        var id = (await client.LoginAsync("Slow", Token)).EntityId!.Value;

        for (var i = 0; i < 2000 && _registry.Count > 0; i++)
        {
            _registry.Send(id, new BlobMessage(60_000));
        }

        await _world.WaitForAsync($"left {id}");
    }

    [Fact]
    public async Task The_server_stops_even_when_a_client_stopped_reading()
    {
        var registry = new SessionRegistry();
        var world = new FakeWorld();
        var gateway = new Gateway(registry, world, Options(maxQueuedFramesPerClient: 100_000), NullLogger<Gateway>.Instance);
        var listener = TcpGatewayListener.Start(gateway, new IPEndPoint(IPAddress.Loopback, 0), NullLogger<TcpGatewayListener>.Instance);
        using var raw = new TcpClient { ReceiveBufferSize = 1024 };
        await raw.ConnectAsync(IPAddress.Loopback, listener.LocalEndPoint.Port, Token);
        await using var client = new GameConnection(raw.GetStream());
        var id = (await client.LoginAsync("Slow", Token)).EntityId!.Value;
        for (var i = 0; i < 500; i++)
        {
            registry.Send(id, new BlobMessage(60_000));
        }

        await listener.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), Token);

        await world.WaitForAsync($"left {id}");
    }

    [Theory]
    [Trait("CA", "0000-CA-02")]
    [InlineData("")]
    [InlineData("TooLongName")]
    [InlineData("Ana Maria")]
    [InlineData("Ana!")]
    public async Task An_invalid_name_is_refused_and_the_connection_closed(string name)
    {
        await using var client = await ConnectAsync();

        var result = await client.LoginAsync(name, Token);

        Assert.Equal(LoginRejectReason.InvalidName, result.RejectReason);
        Assert.Null(await client.ReceiveAsync(Token));
    }

    [Theory]
    [Trait("CA", "0000-CA-02")]
    [InlineData("A")]
    [InlineData("Abcdefg8")]
    public async Task Names_of_one_character_and_of_the_maximum_length_are_accepted(string name)
    {
        await using var client = await ConnectAsync();

        var result = await client.LoginAsync(name, Token);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    [Trait("CA", "0000-CA-02")]
    public async Task A_name_already_online_is_refused_ignoring_case_and_free_again_after_leaving()
    {
        var first = await ConnectAsync();
        var accepted = await first.LoginAsync("Ana", Token);
        await using var second = await ConnectAsync();

        var refused = await second.LoginAsync("ANA", Token);
        await first.DisposeAsync();
        await _world.WaitForAsync($"left {accepted.EntityId}");
        await using var third = await ConnectAsync();
        var acceptedAgain = await third.LoginAsync("Ana", Token);

        Assert.Equal(LoginRejectReason.NameTaken, refused.RejectReason);
        Assert.True(acceptedAgain.IsAccepted);
    }

    [Fact]
    [Trait("CA", "0000-CA-07")]
    public async Task A_client_that_disconnects_leaves_the_world()
    {
        var client = await ConnectAsync();
        var id = (await client.LoginAsync("Ana", Token)).EntityId;

        await client.DisposeAsync();

        await _world.WaitForAsync($"left {id}");
        Assert.Equal(0, _registry.Count);
    }

    [Fact]
    public async Task Messages_after_the_login_go_to_the_world_and_the_world_can_answer()
    {
        await using var client = await ConnectAsync();
        var id = (await client.LoginAsync("Ana", Token)).EntityId!.Value;

        await client.SendAsync(new LoginAccepted(new EntityId(9)), Token);
        await _world.WaitForAsync($"frame {id} type {CoreMessageTypes.LoginAccepted} size 4");
        _registry.Send(id, new LoginRejected(LoginRejectReason.NameTaken));
        var answer = await client.ReceiveAsync(Token);

        Assert.Equal(new LoginRejected(LoginRejectReason.NameTaken), answer!.Value.Decode<LoginRejected>());
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public async Task A_message_before_the_login_closes_the_connection()
    {
        await using var client = await ConnectAsync();

        await client.SendAsync(new LoginAccepted(new EntityId(1)), Token);

        Assert.Null(await client.ReceiveAsync(Token));
        Assert.Empty(_world.Events);
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public async Task A_truncated_login_closes_the_connection()
    {
        using var raw = await ConnectRawAsync();

        await raw.GetStream().WriteAsync(FrameIo.Encode(LoginRequest.TypeId, [1, 0, 5, (byte)'A']), Token);

        Assert.Equal(0, await raw.GetStream().ReadAsync(new byte[16], Token));
        Assert.Empty(_world.Events);
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public async Task A_frame_over_the_size_limit_closes_only_that_connection()
    {
        await using var bad = await ConnectAsync();
        await using var good = await ConnectAsync();
        var badId = (await bad.LoginAsync("Bad", Token)).EntityId;
        var goodId = (await good.LoginAsync("Good", Token)).EntityId;

        await bad.SendAsync(new BlobMessage(MaxFrameBodySize), Token);
        await good.SendAsync(new LoginAccepted(new EntityId(1)), Token);

        Assert.Null(await bad.ReceiveAsync(Token));
        await _world.WaitForAsync($"left {badId}");
        await _world.WaitForAsync($"frame {goodId} type {CoreMessageTypes.LoginAccepted} size 4");
        Assert.DoesNotContain($"left {goodId}", _world.Events);
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public async Task The_world_can_disconnect_a_player()
    {
        await using var client = await ConnectAsync();
        var id = (await client.LoginAsync("Ana", Token)).EntityId!.Value;

        _registry.Disconnect(id, "invalid message");

        Assert.Null(await client.ReceiveAsync(Token));
        await _world.WaitForAsync($"left {id}");
    }

    private Task<GameConnection> ConnectAsync() => GameConnection.ConnectTcpAsync("127.0.0.1", Port, Token);

    private static GatewayOptions Options(int maxQueuedFramesPerClient) =>
        new(MaxFrameBodySize, maxNameLength: 8, maxQueuedFramesPerClient);

    private async Task<TcpClient> ConnectRawAsync(int receiveBufferSize = 8192)
    {
        var client = new TcpClient { ReceiveBufferSize = receiveBufferSize };
        await client.ConnectAsync(IPAddress.Loopback, Port, Token);
        return client;
    }

    /// <summary>A game message of any size, used to go over limits.</summary>
    private sealed class BlobMessage(int payloadSize) : IMessage
    {
        public ushort MessageType => SomeGameMessage;

        public void Write(MessageWriter writer)
        {
            for (var i = 0; i < payloadSize; i++)
            {
                writer.WriteByte(0);
            }
        }
    }
}
