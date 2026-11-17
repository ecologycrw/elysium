namespace Elysium.Core.Protocol;

/// <summary>
/// One message as it travels on the wire: its type number and its payload bytes.
/// </summary>
public readonly record struct Frame(ushort Type, ReadOnlyMemory<byte> Payload)
{
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
