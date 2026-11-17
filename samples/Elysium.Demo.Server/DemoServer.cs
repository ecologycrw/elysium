using System.Net;
using Elysium.Classic.Server.Maps;
using Elysium.Core.Server.Actors;
using Elysium.Core.Server.Sessions;
using Elysium.Core.Server.Transport;
using Microsoft.Extensions.Logging;

namespace Elysium.Demo.Server;

/// <summary>
/// Puts the demonstration server together: one map actor, its tick loop, the gateway and the TCP listener.
/// The program and the end-to-end test both start the server through here.
/// </summary>
public sealed class DemoServer : IAsyncDisposable
{
    private readonly TcpGatewayListener _listener;
    private readonly CancellationTokenSource _stopping;
    private readonly Task _ticking;

    private DemoServer(TcpGatewayListener listener, CancellationTokenSource stopping, Task ticking)
    {
        _listener = listener;
        _stopping = stopping;
        _ticking = ticking;
    }

    public int Port => _listener.LocalEndPoint.Port;

    /// <summary>Starts the server. Port 0 asks the system for any free port.</summary>
    public static DemoServer Start(IPAddress address, int port, ILoggerFactory loggerFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        var registry = new SessionRegistry();
        var map = new MapActor(DemoGame.LoadMap(), DemoGame.MapRules, registry);
        var runner = new TickRunner(map, DemoGame.TicksPerSecond, timeProvider, loggerFactory.CreateLogger<TickRunner>());
        var gateway = new Gateway(registry, map, DemoGame.GatewayOptions, loggerFactory.CreateLogger<Gateway>());

        var stopping = new CancellationTokenSource();
        var ticking = runner.RunAsync(stopping.Token);
        var listener = TcpGatewayListener.Start(gateway, new IPEndPoint(address, port), loggerFactory.CreateLogger<TcpGatewayListener>());
        return new DemoServer(listener, stopping, ticking);
    }

    /// <summary>Completes when the tick loop stops, which only happens on shutdown or when a tick fails.</summary>
    public Task Ticking => _ticking;

    public async ValueTask DisposeAsync()
    {
        await _listener.DisposeAsync().ConfigureAwait(false);
        await _stopping.CancelAsync().ConfigureAwait(false);
        await _ticking.ConfigureAwait(false);
        _stopping.Dispose();
    }
}
