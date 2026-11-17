using Microsoft.Extensions.Logging;

namespace Elysium.Core.Server.Actors;

/// <summary>
/// Runs an actor's ticks at a fixed rate, one at a time, until cancelled.
/// Waiting between ticks is the async boundary; the tick itself is synchronous (INV-03).
/// The real time comes from the injected <see cref="TimeProvider"/>, used only here: the simulation
/// itself counts ticks and never reads a clock (INV-04).
/// If ticks run late, the timer skips the missed ones instead of running them in a burst.
/// </summary>
public sealed partial class TickRunner
{
    public const int MinTicksPerSecond = 10;
    public const int MaxTicksPerSecond = 20;

    private readonly ITickable _actor;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    public TickRunner(ITickable actor, int ticksPerSecond, TimeProvider timeProvider, ILogger<TickRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerSecond, MinTicksPerSecond);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ticksPerSecond, MaxTicksPerSecond);
        _actor = actor;
        _interval = TimeSpan.FromSeconds(1.0 / ticksPerSecond);
        _timeProvider = timeProvider;
        _logger = logger;
        TicksPerSecond = ticksPerSecond;
    }

    public int TicksPerSecond { get; }

    /// <summary>Runs ticks until <paramref name="cancellationToken"/> is cancelled. A tick that throws stops the runner.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);
        LogStarted(TicksPerSecond);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                _actor.Tick();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogStopped();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tick loop started at {TicksPerSecond} ticks per second")]
    private partial void LogStarted(int ticksPerSecond);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tick loop stopped")]
    private partial void LogStopped();
}
