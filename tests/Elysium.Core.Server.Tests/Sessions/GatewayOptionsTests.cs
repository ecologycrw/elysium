using Elysium.Core.Server.Sessions;

namespace Elysium.Core.Server.Tests.Sessions;

/// <summary>
/// A game that configures the gateway with an impossible limit finds out when the server starts,
/// not later, with a player connected. The frame size has to fit a login and the 2-byte length of the envelope.
/// </summary>
public sealed class GatewayOptionsTests
{
    [Theory]
    [InlineData(16, 1, 1)]
    [InlineData(ushort.MaxValue, 64, 100_000)]
    public void The_limits_at_the_edges_of_the_allowed_range_are_accepted(int maxFrameBodySize, int maxNameLength, int maxQueuedFrames)
    {
        var options = new GatewayOptions(maxFrameBodySize, maxNameLength, maxQueuedFrames);

        Assert.Equal((maxFrameBodySize, maxNameLength, maxQueuedFrames), (options.MaxFrameBodySize, options.MaxNameLength, options.MaxQueuedFramesPerClient));
    }

    [Theory]
    [InlineData(15, 16, 1, "maxFrameBodySize")]
    [InlineData(ushort.MaxValue + 1, 16, 1, "maxFrameBodySize")]
    [InlineData(512, 0, 1, "maxNameLength")]
    [InlineData(512, 65, 1, "maxNameLength")]
    [InlineData(512, 16, 0, "maxQueuedFramesPerClient")]
    public void An_impossible_limit_is_refused_when_the_server_starts(int maxFrameBodySize, int maxNameLength, int maxQueuedFrames, string refusedLimit)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new GatewayOptions(maxFrameBodySize, maxNameLength, maxQueuedFrames));

        Assert.Equal(refusedLimit, error.ParamName);
    }
}
