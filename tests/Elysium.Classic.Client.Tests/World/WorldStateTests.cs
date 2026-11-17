using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Login;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Client.Tests.World;

public sealed class WorldStateTests
{
    private static readonly EntityId Ana = new(1);
    private static readonly EntityId Bruno = new(2);

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void Entering_the_map_tells_the_client_who_it_is_and_where()
    {
        var world = new WorldState();

        var change = world.Apply(Frame.FromMessage(new EnteredMap(Ana, new TilePosition(10, 10))));

        Assert.Equal(new WorldChange(WorldChangeKind.EnteredMap, Ana, string.Empty, new TilePosition(10, 10)), change);
        Assert.Equal(Ana, world.Self);
        Assert.Equal(new TilePosition(10, 10), world.SelfPosition);
        Assert.Empty(world.VisibleEntities);
    }

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void An_entity_that_appears_is_in_view_with_its_name_and_tile()
    {
        var world = EnteredAsAna();

        var change = world.Apply(Frame.FromMessage(new EntityAppeared(Bruno, "Bruno", new TilePosition(13, 11))));

        Assert.Equal(new WorldChange(WorldChangeKind.EntityAppeared, Bruno, "Bruno", new TilePosition(13, 11)), change);
        Assert.Equal([new VisibleEntity(Bruno, "Bruno", new TilePosition(13, 11))], world.VisibleEntities);
    }

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void A_step_of_an_entity_in_view_moves_it_to_the_confirmed_tile()
    {
        var world = EnteredAsAna();
        world.Apply(Frame.FromMessage(new EntityAppeared(Bruno, "Bruno", new TilePosition(13, 11))));

        var change = world.Apply(Frame.FromMessage(new EntityMoved(Bruno, new TilePosition(12, 11))));

        Assert.Equal(new WorldChange(WorldChangeKind.EntityMoved, Bruno, "Bruno", new TilePosition(12, 11)), change);
        Assert.True(world.TryGetEntity(Bruno, out var bruno));
        Assert.Equal(new TilePosition(12, 11), bruno.Position);
    }

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void A_step_of_the_own_character_moves_the_player()
    {
        var world = EnteredAsAna();

        var change = world.Apply(Frame.FromMessage(new EntityMoved(Ana, new TilePosition(11, 10))));

        Assert.Equal(new WorldChange(WorldChangeKind.EntityMoved, Ana, string.Empty, new TilePosition(11, 10)), change);
        Assert.Equal(new TilePosition(11, 10), world.SelfPosition);
    }

    [Fact]
    public void A_step_of_an_entity_out_of_view_changes_nothing()
    {
        var world = EnteredAsAna();

        var change = world.Apply(Frame.FromMessage(new EntityMoved(Bruno, new TilePosition(12, 11))));

        Assert.Null(change);
        Assert.Empty(world.VisibleEntities);
        Assert.Equal(new TilePosition(10, 10), world.SelfPosition);
    }

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void An_entity_that_disappears_leaves_the_view_and_the_change_says_who_it_was()
    {
        var world = EnteredAsAna();
        world.Apply(Frame.FromMessage(new EntityAppeared(Bruno, "Bruno", new TilePosition(13, 11))));

        var change = world.Apply(Frame.FromMessage(new EntityDisappeared(Bruno)));

        Assert.Equal(new WorldChange(WorldChangeKind.EntityDisappeared, Bruno, "Bruno", new TilePosition(13, 11)), change);
        Assert.Empty(world.VisibleEntities);
        Assert.False(world.TryGetEntity(Bruno, out _));
    }

    [Fact]
    public void A_disappearance_of_an_entity_never_seen_changes_nothing()
    {
        var world = EnteredAsAna();

        Assert.Null(world.Apply(Frame.FromMessage(new EntityDisappeared(Bruno))));
    }

    [Fact]
    [Trait("CA", "0001-CA-09")]
    public void A_refused_destination_is_counted_and_described_with_the_reason()
    {
        var world = EnteredAsAna();

        var change = world.Apply(Frame.FromMessage(new MoveRejected(new TilePosition(25, 20), MoveRejectReason.Blocked)));

        Assert.Equal(new WorldChange(WorldChangeKind.MoveRefused, Ana, string.Empty, new TilePosition(25, 20), MoveRejectReason.Blocked), change);
        Assert.Equal(1, world.MovesRefused);
        Assert.Equal(new TilePosition(10, 10), world.SelfPosition);
    }

    [Fact]
    public void Only_the_world_messages_are_handled_here()
    {
        Assert.True(WorldState.Handles(ClassicMessageTypes.EnteredMap));
        Assert.True(WorldState.Handles(ClassicMessageTypes.EntityAppeared));
        Assert.True(WorldState.Handles(ClassicMessageTypes.EntityMoved));
        Assert.True(WorldState.Handles(ClassicMessageTypes.EntityDisappeared));
        Assert.True(WorldState.Handles(ClassicMessageTypes.MoveRejected));
        Assert.False(WorldState.Handles(ClassicMessageTypes.MoveRequest));
        Assert.False(WorldState.Handles(CoreMessageTypes.LoginAccepted));
    }

    [Fact]
    public void A_message_that_is_not_about_the_world_is_a_protocol_error()
    {
        var world = new WorldState();

        Assert.Throws<ProtocolException>(() => world.Apply(Frame.FromMessage(new LoginAccepted(Ana))));
    }

    [Fact]
    public void A_truncated_world_message_is_a_protocol_error_and_changes_nothing()
    {
        var world = new WorldState();

        Assert.Throws<ProtocolException>(() => world.Apply(new Frame(ClassicMessageTypes.EnteredMap, new byte[] { 1, 0 })));
        Assert.Null(world.Self);
    }

    private static WorldState EnteredAsAna()
    {
        var world = new WorldState();
        world.Apply(Frame.FromMessage(new EnteredMap(Ana, new TilePosition(10, 10))));
        return world;
    }
}
