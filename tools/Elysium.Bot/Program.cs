using System.Globalization;
using System.Net.Sockets;
using Elysium.Bot;
using Elysium.Classic.Shared.Grid;

// Headless bot for testing the server without graphics.
//   dotnet run --project tools/Elysium.Bot -- demo [--host 127.0.0.1] [--port 7000]
//   dotnet run --project tools/Elysium.Bot -- walk --name Ana --to 12,10 [--host 127.0.0.1] [--port 7000]

const string Usage = """
    Usage:
      demo [--host <host>] [--port <port>]                              run the three-bot scenario
      walk --name <name> --to <x>,<y> [--host <host>] [--port <port>]   log in and walk to a tile
    """;

if (args.Length == 0 || args[0] is not ("demo" or "walk"))
{
    return ShowUsage();
}

var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (var i = 1; i + 1 < args.Length; i += 2)
{
    options[args[i]] = args[i + 1];
}

var host = options.GetValueOrDefault("--host", "127.0.0.1");
if (!int.TryParse(options.GetValueOrDefault("--port", "7000"), NumberStyles.None, CultureInfo.InvariantCulture, out var port))
{
    return ShowUsage();
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    if (args[0] == "demo")
    {
        return await DemoScenario.RunAsync(host, port, Console.Out, cancellation.Token) ? 0 : 1;
    }

    if (!options.TryGetValue("--name", out var name)
        || !options.TryGetValue("--to", out var to)
        || TryParseTile(to) is not { } destination)
    {
        return ShowUsage();
    }

    return await WalkCommand.RunAsync(host, port, name, destination, Console.Out, cancellation.Token) ? 0 : 1;
}
catch (Exception exception) when (exception is InvalidOperationException or SocketException)
{
    Console.WriteLine($"FAILED: {exception.Message}");
    return 1;
}
catch (OperationCanceledException)
{
    return 1;
}

static int ShowUsage()
{
    Console.WriteLine(Usage);
    return 2;
}

static TilePosition? TryParseTile(string text)
{
    var parts = text.Split(',');
    return parts.Length == 2
        && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var x)
        && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var y)
        ? new TilePosition(x, y)
        : null;
}
