using Elysium.Core.Protocol;

namespace Elysium.Core.Tests.Protocol;

public sealed class MessageReaderWriterTests
{
    [Fact]
    public void Fields_round_trip_in_order()
    {
        var writer = new MessageWriter(initialCapacity: 1);
        writer.WriteByte(7);
        writer.WriteUInt16(65000);
        writer.WriteUInt32(4_000_000_000);
        writer.WriteInt32(-123);
        writer.WriteEntityId(new EntityId(42));
        writer.WriteString("Ação");

        var reader = new MessageReader(writer.WrittenSpan);

        Assert.Equal(7, reader.ReadByte());
        Assert.Equal(65000, reader.ReadUInt16());
        Assert.Equal(4_000_000_000u, reader.ReadUInt32());
        Assert.Equal(-123, reader.ReadInt32());
        Assert.Equal(new EntityId(42), reader.ReadEntityId());
        Assert.Equal("Ação", reader.ReadString());
        reader.EnsureEnd();
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void Reading_past_the_end_throws_protocol_exception()
    {
        var writer = new MessageWriter();
        writer.WriteUInt16(1);

        Assert.Throws<ProtocolException>(() =>
        {
            var reader = new MessageReader(writer.WrittenSpan);
            reader.ReadUInt32();
        });
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void A_string_longer_than_the_remaining_bytes_throws_protocol_exception()
    {
        byte[] data = [10, (byte)'a', (byte)'b'];

        Assert.Throws<ProtocolException>(() =>
        {
            var reader = new MessageReader(data);
            reader.ReadString();
        });
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void Invalid_utf8_throws_protocol_exception()
    {
        byte[] data = [2, 0xC3, 0x28];

        Assert.Throws<ProtocolException>(() =>
        {
            var reader = new MessageReader(data);
            reader.ReadString();
        });
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void Extra_bytes_after_the_last_field_throw_protocol_exception()
    {
        byte[] data = [1, 2, 3];

        Assert.Throws<ProtocolException>(() =>
        {
            var reader = new MessageReader(data);
            reader.ReadByte();
            reader.EnsureEnd();
        });
    }

    [Fact]
    public void Writing_a_string_longer_than_the_limit_is_refused()
    {
        var writer = new MessageWriter();

        Assert.Throws<ArgumentException>(() => writer.WriteString(new string('x', MessageWriter.MaxStringBytes + 1)));
    }
}
