using System.Globalization;
using Elysium.Bot;
using Elysium.Classic.Shared.Grid;

// Headless bot for testing the server without graphics.
//   dotnet run --project tools/Elysium.Bot -- demo [--host 127.0.0.1] [--port 7000]
//   dotnet run --project tools/Elysium.Bot -- walk --name Ana --to 12,10 [--host 127.0.0.1] [--port 7000]

const string Usage = """
    Usage:
      demo [--host <host>] [--port <port>]                      run the three-bot scenario
      walk --name <name> --to <x>,<y> [--host <host>] [--port <port>]   log in and walk to a tile
    """;

if (args.Length == 0 || args[0] is not ("demo" or "walk"))
{
    Console.WriteLine(Usage);
    return 2;
}

var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (var i = 1; i + 1 < args.Length; i += 2)
{
    options[args[i]] = args[i + 1];
}

var host = options.GetValueOrDefault("--host", "127.0.0.1");
var port = int.Parse(options.GetValueOrDefault("--port", "7000"), CultureInfo.InvariantCulture);
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

    if (!options.TryGetValue("--name", out var name) || !options.TryGetValue("--to", out var to))
    {
        Console.WriteLine(Usage);
        return 2;
    }

    var parts = to.Split(',');
    var destination = new TilePosition(
        int.Parse(parts[0], CultureInfo.InvariantCulture),
        int.Parse(parts[1], CultureInfo.InvariantCulture));

    await using var bot = await HeadlessBot.ConnectAsync(host, port, name, Console.Out, cancellation.Token);
    await HeadlessBot.WaitUntilAsync(() => bot.Position is not null, TimeSpan.FromSeconds(10), cancellation.Token);
    await bot.WalkToAsync(destination, cancellation.Token);
    var arrived = await HeadlessBot.WaitUntilAsync(() => bot.Position == destination || !bot.IsConnected, TimeSpan.FromSeconds(60), cancellation.Token);
    bot.Say(arrived && bot.Position == destination ? $"arrived at {destination}" : $"stopped at {bot.Position}");
    return 0;
}
catch (Exception exception) when (exception is InvalidOperationException or System.Net.Sockets.SocketException)
{
    Console.WriteLine($"FAILED: {exception.Message}");
    return 1;
}
catch (OperationCanceledException)
{
    return 1;
}
