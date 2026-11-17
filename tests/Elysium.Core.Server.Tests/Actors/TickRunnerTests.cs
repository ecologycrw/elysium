using Elysium.Core.Server.Actors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Core.Server.Tests.Actors;

public sealed class TickRunnerTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(TickRunner.MinTicksPerSecond)]
    [InlineData(TickRunner.MaxTicksPerSecond)]
    public async Task One_second_of_time_runs_exactly_the_configured_number_of_ticks(int ticksPerSecond)
    {
        var time = new ManualTimeProvider();
        using var actor = new CountingActor();
        var runner = new TickRunner(actor, ticksPerSecond, time, NullLogger<TickRunner>.Instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var running = runner.RunAsync(cancellation.Token);
        var interval = TimeSpan.FromSeconds(1.0 / ticksPerSecond);

        for (var tick = 1; tick <= ticksPerSecond; tick++)
        {
            time.Advance(interval);
            await actor.WaitForTickAsync(WaitLimit);
        }

        await cancellation.CancelAsync();
        await running.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ticksPerSecond, actor.Ticks);
    }

    [Fact]
    public async Task No_tick_runs_before_the_first_interval_ends_or_after_cancellation()
    {
        var time = new ManualTimeProvider();
        using var actor = new CountingActor();
        var runner = new TickRunner(actor, ticksPerSecond: 20, time, NullLogger<TickRunner>.Instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var running = runner.RunAsync(cancellation.Token);

        time.Advance(TimeSpan.FromMilliseconds(49));
        var ticksBeforeInterval = actor.Ticks;
        time.Advance(TimeSpan.FromMilliseconds(1));
        await actor.WaitForTickAsync(WaitLimit);
        await cancellation.CancelAsync();
        await running.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(0, ticksBeforeInterval);
        Assert.Equal(1, actor.Ticks);
    }

    [Theory]
    [InlineData(TickRunner.MinTicksPerSecond - 1)]
    [InlineData(TickRunner.MaxTicksPerSecond + 1)]
    public void A_tick_rate_outside_10_to_20_per_second_is_refused(int ticksPerSecond)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TickRunner(new NoOpActor(), ticksPerSecond, TimeProvider.System, NullLogger<TickRunner>.Instance));
    }

    private sealed class CountingActor : ITickable, IDisposable
    {
        private readonly SemaphoreSlim _ticked = new(0);
        private int _ticks;

        public int Ticks => Volatile.Read(ref _ticks);

        public void Tick()
        {
            Interlocked.Increment(ref _ticks);
            _ticked.Release();
        }

        public async Task WaitForTickAsync(TimeSpan limit)
        {
            Assert.True(await _ticked.WaitAsync(limit, TestContext.Current.CancellationToken), "The expected tick did not run.");
        }

        public void Dispose() => _ticked.Dispose();
    }

    private sealed class NoOpActor : ITickable
    {
        public void Tick()
        {
        }
    }
}
