using System.Numerics;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;
using Elysium.Core;

namespace Elysium.Classic.Client.Tests.Movement;

public sealed class WorldMotionTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(250);
    private static readonly EntityId Ana = new(1);
    private static readonly EntityId Bruno = new(2);

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void An_entity_that_appears_is_shown_on_its_tile_at_once()
    {
        var motion = new WorldMotion(new MotionRules(Step, maxStepsBehind: 2));

        motion.Apply(new WorldChange(WorldChangeKind.EntityAppeared, Bruno, "Bruno", new TilePosition(13, 11)));

        Assert.True(motion.TryGetPosition(Bruno, out var position));
        Assert.Equal(new Vector2(13, 11), position);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void The_own_character_and_the_others_slide_to_their_confirmed_steps()
    {
        var motion = new WorldMotion(new MotionRules(Step, maxStepsBehind: 2));
        motion.Apply(new WorldChange(WorldChangeKind.EnteredMap, Ana, string.Empty, new TilePosition(10, 10)));
        motion.Apply(new WorldChange(WorldChangeKind.EntityAppeared, Bruno, "Bruno", new TilePosition(13, 11)));

        motion.Apply(new WorldChange(WorldChangeKind.EntityMoved, Ana, string.Empty, new TilePosition(11, 10)));
        motion.Apply(new WorldChange(WorldChangeKind.EntityMoved, Bruno, "Bruno", new TilePosition(12, 11)));
        motion.Advance(Step / 2);

        Assert.True(motion.TryGetPosition(Ana, out var ana));
        Assert.True(motion.TryGetPosition(Bruno, out var bruno));
        Assert.Equal(new Vector2(10.5f, 10), ana);
        Assert.Equal(new Vector2(12.5f, 11), bruno);
    }

    [Fact]
    public void An_entity_that_disappears_is_no_longer_shown()
    {
        var motion = new WorldMotion(new MotionRules(Step, maxStepsBehind: 2));
        motion.Apply(new WorldChange(WorldChangeKind.EntityAppeared, Bruno, "Bruno", new TilePosition(13, 11)));

        motion.Apply(new WorldChange(WorldChangeKind.EntityDisappeared, Bruno, "Bruno", new TilePosition(13, 11)));

        Assert.False(motion.TryGetPosition(Bruno, out _));
    }

    [Fact]
    public void A_refused_destination_does_not_move_anyone()
    {
        var motion = new WorldMotion(new MotionRules(Step, maxStepsBehind: 2));
        motion.Apply(new WorldChange(WorldChangeKind.EnteredMap, Ana, string.Empty, new TilePosition(10, 10)));

        motion.Apply(new WorldChange(WorldChangeKind.MoveRefused, Ana, string.Empty, new TilePosition(25, 20)));
        motion.Advance(Step);

        Assert.True(motion.TryGetPosition(Ana, out var ana));
        Assert.Equal(new Vector2(10, 10), ana);
    }
}
