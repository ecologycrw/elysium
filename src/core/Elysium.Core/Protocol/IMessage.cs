namespace Elysium.Core.Protocol;

/// <summary>
/// A message that can be sent over the network.
/// Message type ranges: core 1-99, Classic 100-999, games 1000 and above.
/// </summary>
public interface IMessage
{
    ushort MessageType { get; }

    void Write(MessageWriter writer);
}

/// <summary>
/// A message that knows its type number and how to read itself, without reflection (INV-13).
/// </summary>
public interface IMessage<TSelf> : IMessage
    where TSelf : IMessage<TSelf>
{
    static abstract ushort TypeId { get; }

    static abstract TSelf Read(ref MessageReader reader);
}
