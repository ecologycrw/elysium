using Elysium.Core.Client.Connection;
using Elysium.Core.Login;
using Elysium.Core.Protocol;

namespace Elysium.Core.Client.Tests.Connection;

public sealed class ServerLinkTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Messages_from_the_server_are_taken_in_the_order_they_arrived()
    {
        await using var network = await LoopbackServer.ConnectAsync(Token);
        await using var link = new ServerLink(new GameConnection(network.ClientStream));

        foreach (var id in new uint[] { 1, 2, 3 })
        {
            await network.ServerStream.WriteAsync(FrameIo.Encode(new LoginAccepted(new EntityId(id))), Token);
        }

        var taken = new List<LoginAccepted>();
        await WaitUntilAsync(() =>
        {
            while (link.TryTakeMessage(out var frame))
            {
                taken.Add(frame.Decode<LoginAccepted>());
            }

            return taken.Count == 3;
        });
        Assert.Equal([new(new EntityId(1)), new(new EntityId(2)), new(new EntityId(3))], taken);
    }

    [Fact]
    public async Task Taking_a_message_never_waits_for_the_network()
    {
        await using var network = await LoopbackServer.ConnectAsync(Token);
        await using var link = new ServerLink(new GameConnection(network.ClientStream));

        var took = link.TryTakeMessage(out _);

        Assert.False(took);
        Assert.True(link.IsConnected);
    }

    [Fact]
    public async Task Messages_the_game_sends_reach_the_server_in_order()
    {
        await using var network = await LoopbackServer.ConnectAsync(Token);
        await using var link = new ServerLink(new GameConnection(network.ClientStream));

        link.Send(new LoginRejected(LoginRejectReason.NameTaken));
        link.Send(new LoginAccepted(new EntityId(7)));

        var first = await FrameIo.ReadAsync(network.ServerStream, FrameIo.MaxBodySize, Token);
        var second = await FrameIo.ReadAsync(network.ServerStream, FrameIo.MaxBodySize, Token);
        Assert.Equal(new LoginRejected(LoginRejectReason.NameTaken), first!.Value.Decode<LoginRejected>());
        Assert.Equal(new LoginAccepted(new EntityId(7)), second!.Value.Decode<LoginAccepted>());
    }

    [Fact]
    [Trait("CA", "0001-CA-06")]
    public async Task When_the_server_closes_the_link_says_so_and_keeps_what_already_arrived()
    {
        await using var network = await LoopbackServer.ConnectAsync(Token);
        await using var link = new ServerLink(new GameConnection(network.ClientStream));
        await network.ServerStream.WriteAsync(FrameIo.Encode(new LoginAccepted(new EntityId(1))), Token);

        network.CloseServerSide();

        await WaitUntilAsync(() => !link.IsConnected);
        Assert.Equal("the server closed the connection", link.LostReason);
        Assert.True(link.TryTakeMessage(out var frame));
        Assert.Equal(new LoginAccepted(new EntityId(1)), frame.Decode<LoginAccepted>());
        link.Send(new LoginAccepted(new EntityId(2)));
    }

    [Fact]
    [Trait("CA", "0001-CA-06")]
    public async Task An_invalid_message_from_the_server_ends_the_link_with_the_reason()
    {
        await using var network = await LoopbackServer.ConnectAsync(Token);
        await using var link = new ServerLink(new GameConnection(network.ClientStream));

        await network.ServerStream.WriteAsync(new byte[] { 1, 0, 0 }, Token);

        await WaitUntilAsync(() => !link.IsConnected);
        Assert.StartsWith("the server sent an invalid message:", link.LostReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Closing_the_link_closes_the_connection_for_the_server()
    {
        var network = await LoopbackServer.ConnectAsync(Token);
        await using (network)
        {
            var link = new ServerLink(new GameConnection(network.ClientStream));

            await link.DisposeAsync().AsTask().WaitAsync(Timeout, Token);

            Assert.False(link.IsConnected);
            Assert.Equal("the game closed the connection", link.LostReason);
            Assert.Null(await FrameIo.ReadAsync(network.ServerStream, FrameIo.MaxBodySize, Token));
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(Timeout);
        while (!condition())
        {
            await Task.Delay(10, deadline.Token);
        }
    }
}
