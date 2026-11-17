using System.Collections.Concurrent;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Sessions;

namespace Elysium.Core.Server.Tests.Sessions;

/// <summary>
/// A world that only records what the gateway told it, from any thread.
/// </summary>
internal sealed class FakeWorld : IWorld
{
    private readonly ConcurrentQueue<string> _events = new();

    public IReadOnlyList<string> Events => [.. _events];

    public void PlayerJoined(EntityId id, string name) => _events.Enqueue($"joined {id} {name}");

    public void PlayerFrame(EntityId id, Frame frame) => _events.Enqueue($"frame {id} type {frame.Type} size {frame.Payload.Length}");

    public void PlayerLeft(EntityId id) => _events.Enqueue($"left {id}");

    public async Task WaitForAsync(string expectedEvent)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!_events.Contains(expectedEvent))
        {
            try
            {
                await Task.Delay(10, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail($"Timed out waiting for '{expectedEvent}'. Events so far: {string.Join(", ", _events)}");
            }
        }
    }
}
