using System.Net;
using Elysium.Demo.Server;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Demo.Client.Tests;

/// <summary>Logging in to the real demo server over the network, before any window opens (CA-01).</summary>
public sealed class DemoLoginTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("CA", "0001-CA-01")]
    public async Task A_free_name_logs_in_and_gives_the_link_to_the_server()
    {
        await using var server = StartServer();

        var outcome = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", server.Port), Token);

        Assert.Null(outcome.Failure);
        Assert.NotNull(outcome.Link);
        await outcome.Link.DisposeAsync();
    }

    [Fact]
    [Trait("CA", "0001-CA-01")]
    public async Task A_name_in_use_is_refused_with_the_reason()
    {
        await using var server = StartServer();
        var first = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", server.Port), Token);

        var second = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", server.Port), Token);

        Assert.Null(second.Link);
        Assert.Equal("the name 'Ana' is already in use by another player", second.Failure);
        await first.Link!.DisposeAsync();
    }

    [Theory]
    [Trait("CA", "0001-CA-01")]
    [InlineData("")]
    [InlineData("AVeryLongCharacterName")]
    [InlineData("Ana Maria")]
    public async Task An_empty_long_or_malformed_name_is_refused_with_the_reason(string name)
    {
        await using var server = StartServer();

        var outcome = await DemoLogin.LogInAsync(new ClientArguments(name, "127.0.0.1", server.Port), Token);

        Assert.Null(outcome.Link);
        Assert.Equal($"the name '{name}' is not valid: it must have letters and digits only, and not be too long", outcome.Failure);
    }

    [Fact]
    public async Task Without_a_server_the_player_is_told_it_could_not_be_reached()
    {
        int closedPort;
        await using (var server = StartServer())
        {
            closedPort = server.Port;
        }

        var outcome = await DemoLogin.LogInAsync(new ClientArguments("Ana", "127.0.0.1", closedPort), Token);

        Assert.Null(outcome.Link);
        Assert.StartsWith($"could not reach the server at 127.0.0.1:{closedPort}", outcome.Failure, StringComparison.Ordinal);
    }

    private static DemoServer StartServer() =>
        DemoServer.Start(IPAddress.Loopback, port: 0, NullLoggerFactory.Instance, TimeProvider.System);
}
