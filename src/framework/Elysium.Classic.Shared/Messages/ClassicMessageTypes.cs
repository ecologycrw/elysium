namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Type numbers of the Classic messages (range 100-999).
/// </summary>
public static class ClassicMessageTypes
{
    public const ushort MoveRequest = 100;
    public const ushort MoveRejected = 101;
    public const ushort EnteredMap = 102;
    public const ushort EntityAppeared = 103;
    public const ushort EntityMoved = 104;
    public const ushort EntityDisappeared = 105;
}
