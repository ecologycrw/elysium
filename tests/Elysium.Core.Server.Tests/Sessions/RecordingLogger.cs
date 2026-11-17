using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Elysium.Core.Server.Tests.Sessions;

/// <summary>
/// Keeps every log line, as the server operator would read it, from any thread.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyList<string> Lines => [.. _lines];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _lines.Enqueue($"{logLevel}: {formatter(state, exception)}");
    }
}
