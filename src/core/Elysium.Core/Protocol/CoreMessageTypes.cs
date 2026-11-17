namespace Elysium.Core.Protocol;

/// <summary>
/// Type numbers of the core messages (range 1-99).
/// </summary>
public static class CoreMessageTypes
{
    public const ushort LoginRequest = 1;
    public const ushort LoginAccepted = 2;
    public const ushort LoginRejected = 3;
}
