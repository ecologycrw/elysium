using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Client.Connection;

namespace Elysium.Bot;

/// <summary>
/// A client without a screen: it logs in, walks when told to, and writes what it sees, one line per change.
/// A background loop receives messages and updates its <see cref="WorldView"/>.
/// </summary>
public sealed class HeadlessBot : IAsyncDisposable
{
    private readonly GameConnection _connection;
    private readonly WorldView _view = new();
    private readonly Lock _gate = new();
    private readonly TextWriter _output;
    private readonly CancellationTokenSource _stopping = new();
    private Task _receiving = Task.CompletedTask;

    private HeadlessBot(string name, GameConnection connection, TextWriter output)
    {
        Name = name;
        _connection = connection;
        _output = output;
    }

    public string Name { get; }

    public bool IsConnected { get; private set; } = true;

    public TilePosition? Position
    {
        get
        {
            lock (_gate)
            {
                return _view.Position;
            }
        }
    }

    public IReadOnlyCollection<string> VisibleNames
    {
        get
        {
            lock (_gate)
            {
                return _view.VisibleNames;
            }
        }
    }

    /// <summary>Connects and logs in. Throws <see cref="InvalidOperationException"/> when the server refuses the login.</summary>
    public static async Task<HeadlessBot> ConnectAsync(string host, int port, string name, TextWriter output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        var connection = await GameConnection.ConnectTcpAsync(host, port, cancellationToken).ConfigureAwait(false);
        var result = await connection.LoginAsync(name, cancellationToken).ConfigureAwait(false);
        if (!result.IsAccepted)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"The server refused the login of {name}: {result.RejectReason}.");
        }

        var bot = new HeadlessBot(name, connection, output);
        bot._receiving = bot.ReceiveLoopAsync();
        return bot;
    }

    public bool Sees(string name)
    {
        lock (_gate)
        {
            return _view.Sees(name);
        }
    }

    public async Task WalkToAsync(TilePosition destination, CancellationToken cancellationToken)
    {
        Say($"walks to {destination}");
        await _connection.SendAsync(new MoveRequest(destination), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until <paramref name="condition"/> is true. Returns false on timeout.</summary>
    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(condition);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        while (!condition())
        {
            try
            {
                await Task.Delay(20, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return condition();
            }
        }

        return true;
    }

    public void Say(string text)
    {
        lock (_gate)
        {
            _output.WriteLine($"[{Name}] {text}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        await _receiving.ConfigureAwait(false);
        _stopping.Dispose();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (await _connection.ReceiveAsync(_stopping.Token).ConfigureAwait(false) is { } frame)
            {
                string? line;
                lock (_gate)
                {
                    line = _view.Apply(frame);
                }

                if (line is not null)
                {
                    Say(line);
                }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            // Stopping.
        }

        IsConnected = false;
    }
}
