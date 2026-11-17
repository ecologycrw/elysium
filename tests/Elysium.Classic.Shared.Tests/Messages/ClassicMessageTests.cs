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
    [Trait("CA", "0000-CA-08")]
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

    private static void AssertRoundTrip<T>(T sent)
        where T : IMessage<T>
    {
        var writer = new MessageWriter();
        sent.Write(writer);

        var received = new Frame(sent.MessageType, writer.WrittenSpan.ToArray()).Decode<T>();

        Assert.Equal(sent, received);
    }
}
