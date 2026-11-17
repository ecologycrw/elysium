namespace Elysium.Core.Protocol;

/// <summary>
/// One message as it travels on the wire: its type number and its payload bytes.
/// </summary>
public readonly record struct Frame(ushort Type, ReadOnlyMemory<byte> Payload)
{
    /// <summary>Writes <paramref name="message"/> into a new frame, as if it had come from the network.</summary>
    public static Frame FromMessage(IMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var writer = new MessageWriter();
        message.Write(writer);
        return new Frame(message.MessageType, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Reads the payload as <typeparamref name="T"/>. Throws <see cref="ProtocolException"/>
    /// when the type does not match or the payload is truncated or has extra bytes.
    /// </summary>
    public T Decode<T>()
        where T : IMessage<T>
    {
        if (Type != T.TypeId)
        {
            throw new ProtocolException($"Expected message type {T.TypeId}, got {Type}.");
        }

        var reader = new MessageReader(Payload.Span);
        var message = T.Read(ref reader);
        reader.EnsureEnd();
        return message;
    }
}
