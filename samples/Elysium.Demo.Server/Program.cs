using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using Elysium.Demo.Server;
using Microsoft.Extensions.Logging;

// The Elysium Demo server. Usage: dotnet run --project samples/Elysium.Demo.Server [-- --port 7000]
// Stop it with Ctrl+C.

var port = DemoGame.DefaultPort;
var portIndex = Array.IndexOf(args, "--port");
if (portIndex >= 0 && portIndex + 1 < args.Length)
{
    port = int.Parse(args[portIndex + 1], CultureInfo.InvariantCulture);
}

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));

var stop = new TaskCompletionSource();
using var ctrlC = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
{
    context.Cancel = true;
    stop.TrySetResult();
});
using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;
    stop.TrySetResult();
});

await using var server = DemoServer.Start(IPAddress.Loopback, port, loggerFactory, TimeProvider.System);
await Task.WhenAny(stop.Task, server.Ticking);
