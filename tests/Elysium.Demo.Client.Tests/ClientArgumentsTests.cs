namespace Elysium.Demo.Client.Tests;

public sealed class ClientArgumentsTests
{
    [Fact]
    public void The_name_alone_connects_to_the_local_server_on_the_default_port()
    {
        Assert.True(ClientArguments.TryParse(["--name", "Ana"], out var parsed, out _));

        Assert.Equal(new ClientArguments("Ana", "127.0.0.1", 7000), parsed);
    }

    [Fact]
    public void Host_and_port_can_be_given()
    {
        Assert.True(ClientArguments.TryParse(["--port", "7100", "--name", "Bruno", "--host", "10.0.0.5"], out var parsed, out _));

        Assert.Equal(new ClientArguments("Bruno", "10.0.0.5", 7100), parsed);
    }

    [Fact]
    public void Without_a_name_the_client_explains_how_to_start_it()
    {
        Assert.False(ClientArguments.TryParse(["--port", "7000"], out var parsed, out var error));

        Assert.Null(parsed);
        Assert.Contains("name is missing", error, StringComparison.Ordinal);
        Assert.Contains("--name", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_port_that_is_not_a_number_is_refused()
    {
        Assert.False(ClientArguments.TryParse(["--name", "Ana", "--port", "seven"], out _, out var error));

        Assert.Contains("'seven' is not a number", error, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "0001-CA-01")]
    public void An_empty_name_is_passed_on_for_the_server_to_judge()
    {
        Assert.True(ClientArguments.TryParse(["--name", ""], out var parsed, out _));

        Assert.Equal(string.Empty, parsed!.Name);
    }
}
