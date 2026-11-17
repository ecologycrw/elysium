using Elysium.Core.Server.Actors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elysium.Core.Server.Tests.Actors;

public sealed class TickRunnerTests
{
    [Fact]
    public async Task Ticks_run_one_at_a_time_until_cancelled()
    {
        var actor = new CountingActor();
        var runner = new TickRunner(actor, ticksPerSecond: 20, TimeProvider.System, NullLogger<TickRunner>.Instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var running = runner.RunAsync(cancellation.Token);
        await actor.WaitForTicksAsync(5, TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();
        await running;
        var ticksAfterStop = actor.Ticks;
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.True(actor.Ticks >= 5);
        Assert.Equal(ticksAfterStop, actor.Ticks);
        Assert.False(actor.Overlapped);
    }

    [Theory]
    [InlineData(TickRunner.MinTicksPerSecond - 1)]
    [InlineData(TickRunner.MaxTicksPerSecond + 1)]
    public void A_tick_rate_outside_10_to_20_per_second_is_refused(int ticksPerSecond)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TickRunner(new CountingActor(), ticksPerSecond, TimeProvider.System, NullLogger<TickRunner>.Instance));
    }

    private sealed class CountingActor : ITickable
    {
        private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _ticks;
        private int _inside;
        private int _target = int.MaxValue;

        public int Ticks => Volatile.Read(ref _ticks);

        public bool Overlapped { get; private set; }

        public void Tick()
        {
            if (Interlocked.Increment(ref _inside) > 1)
            {
                Overlapped = true;
            }

            if (Interlocked.Increment(ref _ticks) >= Volatile.Read(ref _target))
            {
                _reached.TrySetResult();
            }

            Interlocked.Decrement(ref _inside);
        }

        public async Task WaitForTicksAsync(int count, TimeSpan timeout)
        {
            Volatile.Write(ref _target, count);
            if (Ticks >= count)
            {
                return;
            }

            await _reached.Task.WaitAsync(timeout);
        }
    }
}
