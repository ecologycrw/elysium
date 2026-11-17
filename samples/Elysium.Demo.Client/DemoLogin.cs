using System.Net.Sockets;
using Elysium.Core.Client.Connection;
using Elysium.Core.Login;

namespace Elysium.Demo.Client;

/// <summary>The result of trying to log in: the link to the server, or why there is none. Never both.</summary>
/// <param name="Link">The link to the server, once logged in.</param>
/// <param name="Failure">Why the player could not log in, in words for the terminal.</param>
public sealed record LoginOutcome(ServerLink? Link, string? Failure);

/// <summary>Connects to the demo server and logs in, before any window opens.</summary>
public static class DemoLogin
{
    public static async Task<LoginOutcome> LogInAsync(ClientArguments arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        GameConnection connection;
        try
        {
            connection = await GameConnection.ConnectTcpAsync(arguments.Host, arguments.Port, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException exception)
        {
            return new LoginOutcome(null, $"could not reach the server at {arguments.Host}:{arguments.Port} ({exception.Message}). Is it running?");
        }

        var result = await connection.LoginAsync(arguments.Name, cancellationToken).ConfigureAwait(false);
        if (result.RejectReason is { } reason)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            return new LoginOutcome(null, Describe(reason, arguments.Name));
        }

        return new LoginOutcome(new ServerLink(connection), null);
    }

    /// <summary>Why the server refused the name, in words the player understands.</summary>
    public static string Describe(LoginRejectReason reason, string name) => reason switch
    {
        LoginRejectReason.NameTaken => $"the name '{name}' is already in use by another player",
        LoginRejectReason.InvalidName => $"the name '{name}' is not valid: it must have letters and digits only, and not be too long",
        LoginRejectReason.VersionMismatch => "this client and the server are different versions of the game",
        _ => $"the server refused the login ({reason})",
    };
}
