namespace Elysium.Core.Server.Tests.Actors;

/// <summary>
/// A clock that only moves when the test calls <see cref="Advance"/>, so tests never depend on real time.
/// Timers fire synchronously inside <see cref="Advance"/>, once for every period that ends in the advanced span.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly Lock _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => GetUtcNow().UtcTicks;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_gate)
        {
            _timers.Add(timer);
        }

        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan span)
    {
        ManualTimer[] timers;
        DateTimeOffset now;
        lock (_gate)
        {
            _now += span;
            now = _now;
            timers = [.. _timers];
        }

        foreach (var timer in timers)
        {
            timer.FireUntil(now);
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private DateTimeOffset? _next;
        private TimeSpan _period;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _next = dueTime == Timeout.InfiniteTimeSpan ? null : owner.GetUtcNow() + dueTime;
            _period = period;
            return true;
        }

        public void FireUntil(DateTimeOffset now)
        {
            while (_next is { } next && next <= now)
            {
                _next = _period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero ? null : next + _period;
                callback(state);
            }
        }

        public void Dispose() => owner.Remove(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
