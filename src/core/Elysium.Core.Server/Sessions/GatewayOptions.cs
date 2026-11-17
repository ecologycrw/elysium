namespace Elysium.Core.Server.Sessions;

/// <summary>
/// Limits of the gateway that each game decides (INV-10).
/// </summary>
public sealed record GatewayOptions
{
    public GatewayOptions(int maxFrameBodySize, int maxNameLength, int maxQueuedFramesPerClient)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrameBodySize, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFrameBodySize, ushort.MaxValue);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxNameLength, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxNameLength, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueuedFramesPerClient, 1);
        MaxFrameBodySize = maxFrameBodySize;
        MaxNameLength = maxNameLength;
        MaxQueuedFramesPerClient = maxQueuedFramesPerClient;
    }

    /// <summary>The largest message (type plus payload, in bytes) a client may send. A larger one closes the connection.</summary>
    public int MaxFrameBodySize { get; }

    /// <summary>The longest character name, in characters. Names use only ASCII letters and digits.</summary>
    public int MaxNameLength { get; }

    /// <summary>
    /// How many messages may wait to be sent to one client. A client that falls this far behind is disconnected.
    /// </summary>
    public int MaxQueuedFramesPerClient { get; }
}
