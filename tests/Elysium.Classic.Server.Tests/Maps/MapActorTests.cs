using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Protocol;
using static Elysium.Classic.Server.Tests.Maps.TestMaps;

namespace Elysium.Classic.Server.Tests.Maps;

public sealed class MapActorTests
{
    [Fact]
    public void Commands_wait_in_the_mailbox_until_the_next_tick()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);

        actor.PlayerJoined(Ana, "Ana");

        Assert.Empty(outbox.Sent);
        actor.Tick();
        Assert.Equal([new EnteredMap(Ana, new TilePosition(2, 5))], outbox.To(Ana));
    }

    [Fact]
    [Trait("CA", "0000-CA-03")]
    public void A_move_request_frame_moves_the_character()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");

        actor.PlayerFrame(Ana, ToFrame(new MoveRequest(new TilePosition(3, 5))));
        actor.Tick();

        Assert.Contains(new EntityMoved(Ana, new TilePosition(3, 5)), outbox.To(Ana));
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void A_malformed_message_disconnects_only_that_player()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.Tick();
        outbox.Clear();

        actor.PlayerFrame(Ana, new Frame(ClassicMessageTypes.MoveRequest, new byte[] { 1 }));
        actor.PlayerFrame(Bruno, ToFrame(new MoveRequest(new TilePosition(4, 6))));
        actor.Tick();

        Assert.Equal(Ana, Assert.Single(outbox.Disconnected).Client);
        Assert.Contains(new EntityDisappeared(Ana), outbox.To(Bruno));
        Assert.Contains(new EntityMoved(Bruno, new TilePosition(4, 6)), outbox.To(Bruno));
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void A_message_type_the_map_does_not_accept_disconnects_the_player()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");

        actor.PlayerFrame(Ana, ToFrame(new EntityDisappeared(Bruno)));
        actor.Tick();

        Assert.Equal(Ana, Assert.Single(outbox.Disconnected).Client);
    }

    [Fact]
    [Trait("CA", "0000-CA-07")]
    public void A_player_who_leaves_through_the_mailbox_disappears_for_the_others()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.Tick();
        outbox.Clear();

        actor.PlayerLeft(Bruno);
        actor.Tick();

        Assert.Equal([new EntityDisappeared(Bruno)], outbox.To(Ana));
    }

    [Fact]
    [Trait("CA", "0000-CA-09")]
    public void The_same_commands_produce_exactly_the_same_messages_in_two_runs()
    {
        var first = RunScript();
        var second = RunScript();

        Assert.NotEmpty(first.Sent);
        Assert.Equal(first.Sent, second.Sent);
        Assert.Equal(first.Disconnected, second.Disconnected);
    }

    private static RecordingOutbox RunScript()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.PlayerJoined(Caio, "Caio");
        actor.Tick();

        actor.PlayerFrame(Caio, ToFrame(new MoveRequest(new TilePosition(6, 10))));
        actor.PlayerFrame(Ana, ToFrame(new MoveRequest(new TilePosition(10, 2))));
        for (var tick = 0; tick < 200; tick++)
        {
            if (tick == 40)
            {
                actor.PlayerFrame(Bruno, ToFrame(new MoveRequest(new TilePosition(35, 1))));
                actor.PlayerFrame(Bruno, ToFrame(new MoveRequest(new TilePosition(12, 11))));
            }

            if (tick == 80)
            {
                actor.PlayerFrame(Caio, ToFrame(new MoveRequest(new TilePosition(30, 5))));
            }

            if (tick == 120)
            {
                actor.PlayerLeft(Ana);
            }

            actor.Tick();
        }

        return outbox;
    }

    private static MapActor CreateActor(RecordingOutbox outbox) => new(GridMapText.Parse(Open), Rules, outbox);

    private static Frame ToFrame(IMessage message)
    {
        var writer = new MessageWriter();
        message.Write(writer);
        return new Frame(message.MessageType, writer.WrittenSpan.ToArray());
    }
}
