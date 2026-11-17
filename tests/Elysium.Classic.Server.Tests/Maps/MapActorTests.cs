using System.Security.Cryptography;
using System.Text;
using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Protocol;
using static Elysium.Classic.Server.Tests.Maps.TestMaps;

namespace Elysium.Classic.Server.Tests.Maps;

public sealed class MapActorTests
{
    /// <summary>
    /// SHA-256 of every message the determinism script produces, recorded once.
    /// Two runs in one process cannot catch order that changes between processes (for example, string hashing);
    /// this fixed value can. If a deliberate rule change alters the script's messages, record the new value.
    /// </summary>
    private const string RecordedScriptHash = "39073835D21447FD122DE55A9760EC5AD4377375D9F61C6DF3DB8BCC0EE6E1A3";

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

        actor.PlayerFrame(Ana, Frame.FromMessage(new MoveRequest(new TilePosition(3, 5))));
        actor.Tick();

        Assert.Contains(new EntityMoved(Ana, new TilePosition(3, 5)), outbox.To(Ana));
    }

    [Fact]
    public void Only_the_last_move_request_of_a_player_in_a_tick_counts()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");

        actor.PlayerFrame(Ana, Frame.FromMessage(new MoveRequest(new TilePosition(20, 3))));
        actor.PlayerFrame(Ana, Frame.FromMessage(new MoveRequest(new TilePosition(2, 6))));
        actor.Tick();

        Assert.Empty(outbox.To<MoveRejected>(Ana));
        Assert.Contains(new EntityMoved(Ana, new TilePosition(2, 6)), outbox.To(Ana));
    }

    [Fact]
    public void A_tick_takes_at_most_the_command_limit_and_leaves_the_rest_for_the_next_tick()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox, new MapActorLimits(maxCommandsPerTick: 2, maxPendingFramesPerPlayer: 8));
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.PlayerJoined(Caio, "Caio");

        actor.Tick();
        var enteredAfterFirstTick = outbox.Sent.Count(entry => entry.Message is EnteredMap);
        actor.Tick();

        Assert.Equal(2, enteredAfterFirstTick);
        Assert.Single(outbox.To<EnteredMap>(Caio));
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void A_player_who_floods_the_mailbox_is_disconnected_and_the_others_keep_playing()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox, new MapActorLimits(maxCommandsPerTick: 1000, maxPendingFramesPerPlayer: 4));
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.Tick();
        outbox.Clear();

        for (var i = 0; i < 100; i++)
        {
            actor.PlayerFrame(Ana, Frame.FromMessage(new MoveRequest(new TilePosition(2, 6))));
        }

        actor.PlayerFrame(Bruno, Frame.FromMessage(new MoveRequest(new TilePosition(4, 6))));
        actor.PlayerLeft(Ana);
        actor.Tick();

        Assert.All(outbox.Disconnected, entry => Assert.Equal(Ana, entry.Client));
        Assert.NotEmpty(outbox.Disconnected);
        Assert.Contains(new EntityMoved(Bruno, new TilePosition(4, 6)), outbox.To(Bruno));
        Assert.Contains(new EntityDisappeared(Ana), outbox.To(Bruno));
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
        actor.PlayerFrame(Bruno, Frame.FromMessage(new MoveRequest(new TilePosition(4, 6))));
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

        actor.PlayerFrame(Ana, Frame.FromMessage(new EntityDisappeared(Bruno)));
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

    [Fact]
    [Trait("CA", "0000-CA-09")]
    public void The_same_commands_produce_the_recorded_messages_in_any_process()
    {
        var outbox = RunScript();
        var transcript = string.Join('\n', outbox.Sent.Select(entry => $"{entry.Recipient} {entry.Message}"));

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(transcript)));

        Assert.Equal(RecordedScriptHash, hash);
    }

    private static RecordingOutbox RunScript()
    {
        var outbox = new RecordingOutbox();
        var actor = CreateActor(outbox);
        actor.PlayerJoined(Ana, "Ana");
        actor.PlayerJoined(Bruno, "Bruno");
        actor.PlayerJoined(Caio, "Caio");
        actor.Tick();

        actor.PlayerFrame(Caio, Frame.FromMessage(new MoveRequest(new TilePosition(6, 10))));
        actor.PlayerFrame(Ana, Frame.FromMessage(new MoveRequest(new TilePosition(10, 2))));
        for (var tick = 0; tick < 200; tick++)
        {
            if (tick == 40)
            {
                actor.PlayerFrame(Bruno, Frame.FromMessage(new MoveRequest(new TilePosition(35, 1))));
            }

            if (tick == 41)
            {
                actor.PlayerFrame(Bruno, Frame.FromMessage(new MoveRequest(new TilePosition(12, 11))));
            }

            if (tick == 80)
            {
                actor.PlayerFrame(Caio, Frame.FromMessage(new MoveRequest(new TilePosition(30, 5))));
            }

            if (tick == 120)
            {
                actor.PlayerLeft(Ana);
            }

            actor.Tick();
        }

        return outbox;
    }

    private static MapActor CreateActor(RecordingOutbox outbox, MapActorLimits? limits = null) =>
        new(GridMapText.Parse(Open), Rules, limits ?? new MapActorLimits(maxCommandsPerTick: 100, maxPendingFramesPerPlayer: 8), outbox);
}
