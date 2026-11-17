using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using static Elysium.Classic.Server.Tests.Maps.TestMaps;

namespace Elysium.Classic.Server.Tests.Maps;

public sealed class MapVisibilityTests
{
    [Fact]
    [Trait("CA", "0000-CA-01")]
    public void A_player_enters_at_the_spawn_point_and_is_told_its_position()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);

        map.Join(Ana, "Ana");

        Assert.Equal([new EnteredMap(Ana, new TilePosition(2, 5))], outbox.To(Ana));
    }

    [Fact]
    public void Players_enter_at_the_spawn_points_in_turn()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);

        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        map.Join(Caio, "Caio");

        Assert.Equal(new EnteredMap(Bruno, new TilePosition(4, 5)), outbox.To<EnteredMap>(Bruno).Single());
        Assert.Equal(new EnteredMap(Caio, new TilePosition(30, 5)), outbox.To<EnteredMap>(Caio).Single());
    }

    [Fact]
    [Trait("CA", "0000-CA-05")]
    public void Two_players_within_view_see_each_other_appear_with_the_right_position()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);

        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");

        Assert.Equal([new EntityAppeared(Bruno, "Bruno", new TilePosition(4, 5))], outbox.To<EntityAppeared>(Ana));
        Assert.Equal([new EntityAppeared(Ana, "Ana", new TilePosition(2, 5))], outbox.To<EntityAppeared>(Bruno));
    }

    [Fact]
    [Trait("CA", "0000-CA-06")]
    public void A_player_out_of_view_receives_nothing_about_the_others()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);

        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        map.Join(Caio, "Caio");

        Assert.Equal([new EnteredMap(Caio, new TilePosition(30, 5))], outbox.To(Caio));
        Assert.DoesNotContain(outbox.To(Ana), message => message is EntityAppeared { Id: var id } && id == Caio);
        Assert.DoesNotContain(outbox.To(Bruno), message => message is EntityAppeared { Id: var id } && id == Caio);
    }

    [Fact]
    [Trait("CA", "0000-CA-07")]
    public void A_player_who_leaves_disappears_for_everyone_who_saw_it()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        map.Join(Caio, "Caio");
        outbox.Clear();

        map.Leave(Ana);

        Assert.Equal([new EntityDisappeared(Ana)], outbox.To(Bruno));
        Assert.Empty(outbox.To(Caio));
        Assert.False(map.TryGetPosition(Ana, out _));
        Assert.Equal(2, map.PlayerCount);
    }

    [Fact]
    [Trait("CA", "0000-CA-07")]
    public void Leaving_twice_or_leaving_without_joining_does_nothing()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.Leave(Ana);
        outbox.Clear();

        map.Leave(Ana);
        map.Leave(Bruno);

        Assert.Empty(outbox.Sent);
    }

    [Fact]
    public void Joining_twice_with_the_same_entity_is_a_programming_error()
    {
        var map = Create(new RecordingOutbox());
        map.Join(Ana, "Ana");

        Assert.Throws<InvalidOperationException>(() => map.Join(Ana, "Ana"));
    }
}
