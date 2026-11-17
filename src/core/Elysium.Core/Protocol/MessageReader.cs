using System.Buffers.Binary;
using System.Text;

namespace Elysium.Core.Protocol;

/// <summary>
/// Reads the fields written by <see cref="MessageWriter"/>.
/// Any truncated or invalid data throws <see cref="ProtocolException"/>.
/// </summary>
public ref struct MessageReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public MessageReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    public readonly int Remaining => _data.Length - _position;

    public byte ReadByte() => Take(1)[0];

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(sizeof(ushort)));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(sizeof(uint)));

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));

    public EntityId ReadEntityId() => new(ReadUInt32());

    public string ReadString()
    {
        var byteCount = ReadByte();
        var bytes = Take(byteCount);
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new ProtocolException("A string is not valid UTF-8.", exception);
        }
    }

    /// <summary>Throws when bytes are left over after the last field of a message.</summary>
    public readonly void EnsureEnd()
    {
        if (Remaining != 0)
        {
            throw new ProtocolException($"The message has {Remaining} unexpected extra bytes.");
        }
    }

    private ReadOnlySpan<byte> Take(int count)
    {
        if (count > Remaining)
        {
            throw new ProtocolException("The message ended before all of its fields were read.");
        }

        var span = _data.Slice(_position, count);
        _position += count;
        return span;
    }
}
