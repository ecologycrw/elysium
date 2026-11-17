using System.Numerics;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Tests.Movement;

public sealed class TileMotionTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(250);
    private static readonly MotionRules Rules = new(Step, maxStepsBehind: 2);

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void A_confirmed_step_is_shown_sliding_over_exactly_one_step_duration()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        motion.Confirm(new TilePosition(11, 10));

        motion.Advance(Step / 2, Rules);
        var halfway = motion.Position;
        motion.Advance(Step / 2, Rules);

        Assert.Equal(new Vector2(10.5f, 10), halfway);
        Assert.Equal(new Vector2(11, 10), motion.Position);
        Assert.False(motion.IsMoving);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void A_diagonal_step_takes_the_same_time_as_a_straight_one()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        motion.Confirm(new TilePosition(11, 11));

        motion.Advance(Step / 4, Rules);

        Assert.Equal(new Vector2(10.25f, 10.25f), motion.Position);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void The_shown_position_never_goes_past_the_last_confirmed_tile()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        motion.Confirm(new TilePosition(11, 10));

        motion.Advance(Step * 10, Rules);

        Assert.Equal(new Vector2(11, 10), motion.Position);
        Assert.Equal(new TilePosition(11, 10), motion.LastConfirmed);
        Assert.False(motion.IsMoving);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void Without_a_confirmed_step_the_entity_stays_on_its_tile()
    {
        var motion = new TileMotion(new TilePosition(10, 10));

        motion.Advance(Step, Rules);

        Assert.Equal(new Vector2(10, 10), motion.Position);
        Assert.False(motion.IsMoving);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void Several_confirmed_steps_are_shown_in_order_at_the_game_speed()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        motion.Confirm(new TilePosition(11, 10));
        motion.Confirm(new TilePosition(12, 11));

        motion.Advance(Step, Rules);
        var afterOneStep = motion.Position;
        motion.Advance(Step / 2, Rules);
        var halfwayToSecond = motion.Position;
        motion.Advance(Step / 2, Rules);

        Assert.Equal(new Vector2(11, 10), afterOneStep);
        Assert.Equal(new Vector2(11.5f, 10.5f), halfwayToSecond);
        Assert.Equal(new Vector2(12, 11), motion.Position);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void A_long_frame_catches_up_without_going_past_the_server()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        motion.Confirm(new TilePosition(11, 10));
        motion.Confirm(new TilePosition(12, 10));

        motion.Advance(TimeSpan.FromSeconds(3), Rules);

        Assert.Equal(new Vector2(12, 10), motion.Position);
    }

    [Fact]
    [Trait("CA", "0001-CA-03")]
    public void When_it_falls_too_far_behind_it_jumps_ahead_instead_of_piling_up_delay()
    {
        var motion = new TileMotion(new TilePosition(10, 10));
        for (var column = 11; column <= 15; column++)
        {
            motion.Confirm(new TilePosition(column, 10));
        }

        motion.Advance(TimeSpan.Zero, Rules);
        var afterCatchingUp = motion.Position;
        motion.Advance(Step * 2, Rules);

        Assert.Equal(new Vector2(13, 10), afterCatchingUp);
        Assert.Equal(new Vector2(15, 10), motion.Position);
    }

    [Fact]
    public void Rules_that_cannot_work_are_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MotionRules(TimeSpan.Zero, maxStepsBehind: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MotionRules(Step, maxStepsBehind: 0));
    }
}
