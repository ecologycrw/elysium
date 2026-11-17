namespace Elysium.Core.Server.Sessions;

/// <summary>
/// Limits of the gateway that each game decides (INV-10).
/// </summary>
public sealed record GatewayOptions
{
    public GatewayOptions(int maxFrameBodySize, int maxNameLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrameBodySize, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxFrameBodySize, ushort.MaxValue);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxNameLength, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxNameLength, 64);
        MaxFrameBodySize = maxFrameBodySize;
        MaxNameLength = maxNameLength;
    }

    /// <summary>The largest message (type plus payload, in bytes) a client may send. A larger one closes the connection.</summary>
    public int MaxFrameBodySize { get; }

    /// <summary>The longest character name, in characters. Names use only ASCII letters and digits.</summary>
    public int MaxNameLength { get; }
}
