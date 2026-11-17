using System.Globalization;
using Elysium.Demo.Shared;

namespace Elysium.Demo.Client;

/// <summary>What the player gave on the command line: <c>--name Ana [--host 127.0.0.1] [--port 7000]</c>.</summary>
public sealed record ClientArguments(string Name, string Host, int Port)
{
    public const string Usage = "Usage: dotnet run --project samples/Elysium.Demo.Client -- --name <character> [--host 127.0.0.1] [--port 7000]";

    /// <summary>
    /// Reads the arguments. The name is required, but may be anything, even empty: whether it is valid is the
    /// server's decision (INV-01). False, with the reason in <paramref name="error"/>, when they cannot be read.
    /// </summary>
    public static bool TryParse(IReadOnlyList<string> args, out ClientArguments? parsed, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);
        parsed = null;
        if (!TryGetValue(args, "--name", out var name))
        {
            error = $"The character name is missing. {Usage}";
            return false;
        }

        var host = TryGetValue(args, "--host", out var givenHost) ? givenHost : "127.0.0.1";
        var port = DemoWorld.DefaultPort;
        if (TryGetValue(args, "--port", out var givenPort)
            && !int.TryParse(givenPort, NumberStyles.None, CultureInfo.InvariantCulture, out port))
        {
            error = $"The port '{givenPort}' is not a number. {Usage}";
            return false;
        }

        parsed = new ClientArguments(name, host, port);
        error = string.Empty;
        return true;
    }

    private static bool TryGetValue(IReadOnlyList<string> args, string option, out string value)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == option)
            {
                value = args[i + 1];
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
