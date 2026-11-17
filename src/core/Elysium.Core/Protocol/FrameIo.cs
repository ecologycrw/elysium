using System.Buffers.Binary;

namespace Elysium.Core.Protocol;

/// <summary>
/// Reads and writes frames on a stream. Wire format (little-endian):
/// a 2-byte length (of everything after it), a 2-byte message type, then the payload.
/// </summary>
public static class FrameIo
{
    /// <summary>Size of the length field that starts every frame.</summary>
    public const int LengthSize = sizeof(ushort);

    /// <summary>Size of the message type that follows the length.</summary>
    public const int TypeSize = sizeof(ushort);

    /// <summary>The largest frame body (type plus payload) the wire format can express.</summary>
    public const int MaxBodySize = ushort.MaxValue;

    public static byte[] Encode(IMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var writer = new MessageWriter();
        message.Write(writer);
        return Encode(message.MessageType, writer.WrittenSpan);
    }

    public static byte[] Encode(ushort type, ReadOnlySpan<byte> payload)
    {
        var bodySize = TypeSize + payload.Length;
        if (bodySize > MaxBodySize)
        {
            throw new ArgumentException($"A frame body is limited to {MaxBodySize} bytes.", nameof(payload));
        }

        var bytes = new byte[LengthSize + bodySize];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)bodySize);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(LengthSize), type);
        payload.CopyTo(bytes.AsSpan(LengthSize + TypeSize));
        return bytes;
    }

    /// <summary>
    /// Reads the next frame. Returns <see langword="null"/> when the stream ends cleanly between frames.
    /// Throws <see cref="ProtocolException"/> when the frame body is larger than <paramref name="maxBodySize"/>,
    /// too short to hold a type, or cut off in the middle.
    /// </summary>
    public static async ValueTask<Frame?> ReadAsync(Stream stream, int maxBodySize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = new byte[LengthSize];
        var headerRead = await stream.ReadAtLeastAsync(header, LengthSize, throwOnEndOfStream: false, cancellationToken)
            .ConfigureAwait(false);
        if (headerRead == 0)
        {
            return null;
        }

        if (headerRead < LengthSize)
        {
            throw new ProtocolException("The connection ended in the middle of a frame.");
        }

        var bodySize = BinaryPrimitives.ReadUInt16LittleEndian(header);
        if (bodySize < TypeSize)
        {
            throw new ProtocolException($"A frame body must have at least {TypeSize} bytes, got {bodySize}.");
        }

        if (bodySize > maxBodySize)
        {
            throw new ProtocolException($"A frame body of {bodySize} bytes is larger than the limit of {maxBodySize}.");
        }

        var body = new byte[bodySize];
        var bodyRead = await stream.ReadAtLeastAsync(body, bodySize, throwOnEndOfStream: false, cancellationToken)
            .ConfigureAwait(false);
        if (bodyRead < bodySize)
        {
            throw new ProtocolException("The connection ended in the middle of a frame.");
        }

        var type = BinaryPrimitives.ReadUInt16LittleEndian(body);
        return new Frame(type, body.AsMemory(TypeSize));
    }
}
