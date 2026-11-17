using System.Buffers.Binary;
using System.Text;

namespace Elysium.Core.Protocol;

/// <summary>
/// Writes the fields of one message as little-endian binary.
/// Strings are written as one length byte followed by UTF-8 bytes.
/// </summary>
public sealed class MessageWriter
{
    /// <summary>The longest string a message can carry, in UTF-8 bytes.</summary>
    public const int MaxStringBytes = byte.MaxValue;

    private byte[] _buffer;
    private int _length;

    public MessageWriter(int initialCapacity = 64)
    {
        _buffer = new byte[Math.Max(initialCapacity, 16)];
    }

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _length);

    public int Length => _length;

    public void Clear() => _length = 0;

    public void WriteByte(byte value)
    {
        Reserve(1)[0] = value;
    }

    public void WriteUInt16(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(Reserve(sizeof(ushort)), value);
    }

    public void WriteUInt32(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(Reserve(sizeof(uint)), value);
    }

    public void WriteInt32(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(Reserve(sizeof(int)), value);
    }

    public void WriteEntityId(EntityId value) => WriteUInt32(value.Value);

    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > MaxStringBytes)
        {
            throw new ArgumentException($"Strings are limited to {MaxStringBytes} UTF-8 bytes.", nameof(value));
        }

        WriteByte((byte)byteCount);
        Encoding.UTF8.GetBytes(value, Reserve(byteCount));
    }

    private Span<byte> Reserve(int count)
    {
        if (_length + count > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _length + count));
        }

        var span = _buffer.AsSpan(_length, count);
        _length += count;
        return span;
    }
}
