using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Tests.Messages;

public sealed class ClassicMessageTests
{
    [Fact]
    public void Move_request_round_trips()
    {
        AssertRoundTrip<MoveRequest>(new MoveRequest(new TilePosition(65535, 0)));
    }

    [Fact]
    public void Move_rejected_round_trips()
    {
        AssertRoundTrip<MoveRejected>(new MoveRejected(new TilePosition(1, 2), MoveRejectReason.Unreachable));
    }

    [Fact]
    public void Entered_map_round_trips()
    {
        AssertRoundTrip<EnteredMap>(new EnteredMap(new EntityId(3), new TilePosition(10, 20)));
    }

    [Fact]
    public void Entity_appeared_round_trips()
    {
        AssertRoundTrip<EntityAppeared>(new EntityAppeared(new EntityId(4), "Bruno", new TilePosition(5, 6)));
    }

    [Fact]
    public void Entity_moved_round_trips()
    {
        AssertRoundTrip<EntityMoved>(new EntityMoved(new EntityId(5), new TilePosition(7, 8)));
    }

    [Fact]
    public void Entity_disappeared_round_trips()
    {
        AssertRoundTrip<EntityDisappeared>(new EntityDisappeared(new EntityId(6)));
    }

    [Fact]
    public void A_truncated_move_request_throws_protocol_exception()
    {
        var frame = new Frame(MoveRequest.TypeId, new byte[] { 1, 0, 2 });

        Assert.Throws<ProtocolException>(() => frame.Decode<MoveRequest>());
    }

    [Fact]
    public void A_negative_tile_cannot_be_written()
    {
        var writer = new MessageWriter();

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteTile(new TilePosition(-1, 0)));
    }

    [Fact]
    public void Classic_message_type_numbers_never_change()
    {
        Assert.Equal(100, MoveRequest.TypeId);
        Assert.Equal(101, MoveRejected.TypeId);
        Assert.Equal(102, EnteredMap.TypeId);
        Assert.Equal(103, EntityAppeared.TypeId);
        Assert.Equal(104, EntityMoved.TypeId);
        Assert.Equal(105, EntityDisappeared.TypeId);
    }

    // Fixed bytes (little-endian): a change here breaks every client of protocol version 1 (INV-11).
    // Tiles are column then row, 16 bits each; entity IDs are 32 bits; strings are a length byte and UTF-8.

    [Fact]
    public void Move_request_bytes_are_fixed()
    {
        Assert.Equal([0x07, 0x00, 0x08, 0x00], PayloadOf(new MoveRequest(new TilePosition(7, 8))));
    }

    [Fact]
    public void Move_rejected_bytes_are_fixed()
    {
        Assert.Equal(
            [0x01, 0x00, 0x02, 0x00, 0x03],
            PayloadOf(new MoveRejected(new TilePosition(1, 2), MoveRejectReason.Unreachable)));
    }

    [Fact]
    public void Entered_map_bytes_are_fixed()
    {
        Assert.Equal(
            [0x03, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x14, 0x00],
            PayloadOf(new EnteredMap(new EntityId(3), new TilePosition(10, 20))));
    }

    [Fact]
    public void Entity_appeared_bytes_are_fixed()
    {
        Assert.Equal(
            [0x04, 0x00, 0x00, 0x00, 0x02, (byte)'B', (byte)'o', 0x05, 0x00, 0x06, 0x00],
            PayloadOf(new EntityAppeared(new EntityId(4), "Bo", new TilePosition(5, 6))));
    }

    [Fact]
    public void Entity_moved_bytes_are_fixed()
    {
        Assert.Equal(
            [0x05, 0x00, 0x00, 0x00, 0x07, 0x00, 0x08, 0x00],
            PayloadOf(new EntityMoved(new EntityId(5), new TilePosition(7, 8))));
    }

    [Fact]
    public void Entity_disappeared_bytes_are_fixed()
    {
        Assert.Equal([0x06, 0x00, 0x00, 0x00], PayloadOf(new EntityDisappeared(new EntityId(6))));
    }

    private static void AssertRoundTrip<T>(T sent)
        where T : IMessage<T>
    {
        Assert.Equal(sent, Frame.FromMessage(sent).Decode<T>());
    }

    private static byte[] PayloadOf(IMessage message) => Frame.FromMessage(message).Payload.ToArray();
}
