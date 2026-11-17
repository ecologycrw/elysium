using Elysium.Classic.Server.Maps;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using static Elysium.Classic.Server.Tests.Maps.TestMaps;

namespace Elysium.Classic.Server.Tests.Maps;

public sealed class MapMovementTests
{
    [Fact]
    [Trait("CA", "0000-CA-03")]
    public void A_character_walks_one_tile_per_step_interval_until_the_destination()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        outbox.Clear();

        map.RequestMove(Ana, new TilePosition(8, 5));
        var stepTicks = RunUntilIdle(map, outbox, Ana, maxTicks: 100);

        Assert.Equal([1L, 4, 7, 10, 13, 16], stepTicks.Select(step => step.Tick));
        Assert.Equal(
            [new(3, 5), new(4, 5), new(5, 5), new(6, 5), new(7, 5), new(8, 5)],
            stepTicks.Select(step => step.Position));
        Assert.True(map.TryGetPosition(Ana, out var position));
        Assert.Equal(new TilePosition(8, 5), position);
    }

    [Fact]
    [Trait("CA", "0000-CA-03")]
    public void Repeating_the_request_every_tick_never_makes_the_character_faster()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        outbox.Clear();
        var steps = new List<long>();

        for (var i = 0; i < 30; i++)
        {
            map.RequestMove(Ana, new TilePosition(30, 11));
            var before = outbox.To<EntityMoved>(Ana).Count;
            map.Tick();
            if (outbox.To<EntityMoved>(Ana).Count > before)
            {
                steps.Add(map.CurrentTick);
            }
        }

        Assert.Equal(10, steps.Count);
        Assert.All(steps.Zip(steps.Skip(1)), pair => Assert.True(pair.Second - pair.First >= TicksPerStep));
    }

    [Fact]
    [Trait("CA", "0000-CA-03")]
    public void Every_step_goes_to_an_adjacent_tile()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        outbox.Clear();

        map.RequestMove(Ana, new TilePosition(30, 2));
        var steps = RunUntilIdle(map, outbox, Ana, maxTicks: 500);

        Assert.Equal(new TilePosition(30, 2), steps[^1].Position);
        var previous = new TilePosition(2, 5);
        foreach (var (_, position) in steps)
        {
            Assert.Equal(1, previous.ChebyshevDistance(position));
            previous = position;
        }
    }

    [Theory]
    [Trait("CA", "0000-CA-04")]
    [InlineData(40, 0, MoveRejectReason.OutOfMap)]
    [InlineData(0, 12, MoveRejectReason.OutOfMap)]
    [InlineData(-1, 3, MoveRejectReason.OutOfMap)]
    [InlineData(20, 3, MoveRejectReason.Blocked)]
    [InlineData(35, 1, MoveRejectReason.Unreachable)]
    public void An_invalid_destination_is_refused_and_the_character_stays(int x, int y, MoveRejectReason expected)
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        outbox.Clear();
        var destination = new TilePosition(x, y);

        map.RequestMove(Ana, destination);
        RunTicks(map, 20);

        Assert.Equal([new MoveRejected(destination, expected)], outbox.To(Ana));
        Assert.True(map.TryGetPosition(Ana, out var position));
        Assert.Equal(new TilePosition(2, 5), position);
    }

    [Fact]
    [Trait("CA", "0000-CA-04")]
    public void A_destination_further_than_the_path_limit_is_unreachable()
    {
        var outbox = new RecordingOutbox();
        var map = new MapSimulation(GridMapText.Parse(Open), new MapRules(ViewRadius, TicksPerStep, maxPathLength: 5), outbox);
        map.Join(Ana, "Ana");
        outbox.Clear();

        map.RequestMove(Ana, new TilePosition(10, 5));

        Assert.Equal([new MoveRejected(new TilePosition(10, 5), MoveRejectReason.Unreachable)], outbox.To(Ana));
    }

    [Fact]
    [Trait("CA", "0000-CA-04")]
    public void A_refused_request_stops_the_path_in_progress()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.RequestMove(Ana, new TilePosition(10, 5));
        RunTicks(map, 1);

        map.RequestMove(Ana, new TilePosition(20, 3));
        RunTicks(map, 20);

        Assert.True(map.TryGetPosition(Ana, out var position));
        Assert.Equal(new TilePosition(3, 5), position);
    }

    [Fact]
    [Trait("CA", "0000-CA-04")]
    public void No_step_ever_lands_on_a_blocked_tile_or_cuts_a_wall_corner()
    {
        var outbox = new RecordingOutbox();
        var parsed = GridMapText.Parse(Open);
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        map.Join(Caio, "Caio");
        outbox.Clear();

        map.RequestMove(Ana, new TilePosition(22, 3));
        map.RequestMove(Bruno, new TilePosition(37, 0));
        map.RequestMove(Caio, new TilePosition(19, 0));
        RunTicks(map, 300);

        foreach (var id in new[] { Ana, Bruno, Caio })
        {
            var steps = outbox.To<EntityMoved>(id).Where(moved => moved.Id == id).Select(moved => moved.Position).ToList();
            Assert.NotEmpty(steps);
            Assert.All(steps, step => Assert.True(parsed.IsWalkable(step), $"{id} stepped on {step}"));
            Assert.All(steps.Zip(steps.Skip(1)), pair =>
            {
                var (from, to) = pair;
                Assert.True(parsed.IsWalkable(new TilePosition(to.X, from.Y)) && parsed.IsWalkable(new TilePosition(from.X, to.Y)),
                    $"{id} cut a corner from {from} to {to}");
            });
        }
    }

    [Fact]
    [Trait("CA", "0000-CA-05")]
    public void A_player_sees_the_steps_of_another_player_in_view()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        outbox.Clear();

        map.RequestMove(Ana, new TilePosition(2, 7));
        RunTicks(map, 10);

        Assert.Equal(
            [new EntityMoved(Ana, new TilePosition(2, 6)), new EntityMoved(Ana, new TilePosition(2, 7))],
            outbox.To<EntityMoved>(Bruno));
    }

    [Fact]
    [Trait("CA", "0000-CA-06")]
    public void A_far_player_who_walks_into_view_sees_the_others_and_loses_them_when_walking_away()
    {
        var outbox = new RecordingOutbox();
        var map = Create(outbox);
        map.Join(Ana, "Ana");
        map.Join(Bruno, "Bruno");
        map.Join(Caio, "Caio");
        outbox.Clear();

        map.RequestMove(Caio, new TilePosition(6, 10));
        RunUntilIdle(map, outbox, Caio, maxTicks: 500);

        Assert.Equal(
            [new EntityAppeared(Ana, "Ana", new TilePosition(2, 5)), new EntityAppeared(Bruno, "Bruno", new TilePosition(4, 5))],
            outbox.To<EntityAppeared>(Caio).OrderBy(appeared => appeared.Id));
        Assert.Single(outbox.To<EntityAppeared>(Ana), appeared => appeared.Id == Caio);
        Assert.Single(outbox.To<EntityAppeared>(Bruno), appeared => appeared.Id == Caio);
        outbox.Clear();

        map.RequestMove(Caio, new TilePosition(30, 5));
        RunUntilIdle(map, outbox, Caio, maxTicks: 500);

        Assert.Equal([Ana, Bruno], outbox.To<EntityDisappeared>(Caio).Select(gone => gone.Id).Order());
        Assert.Equal([new EntityDisappeared(Caio)], outbox.To<EntityDisappeared>(Ana));
        Assert.Equal([new EntityDisappeared(Caio)], outbox.To<EntityDisappeared>(Bruno));
        Assert.DoesNotContain(outbox.To<EntityMoved>(Ana), moved => moved.Id == Caio
            && moved.Position.ChebyshevDistance(new TilePosition(2, 5)) > ViewRadius);
    }

    private static void RunTicks(MapSimulation map, int count)
    {
        for (var i = 0; i < count; i++)
        {
            map.Tick();
        }
    }

    /// <summary>Runs ticks until the character has not stepped for a while, and returns each of its steps with the tick.</summary>
    private static List<(long Tick, TilePosition Position)> RunUntilIdle(MapSimulation map, RecordingOutbox outbox, EntityId id, int maxTicks)
    {
        var steps = new List<(long, TilePosition)>();
        var idleTicks = 0;
        for (var i = 0; i < maxTicks && idleTicks <= TicksPerStep; i++)
        {
            var before = outbox.To<EntityMoved>(id).Count(moved => moved.Id == id);
            map.Tick();
            var after = outbox.To<EntityMoved>(id).Where(moved => moved.Id == id).ToList();
            if (after.Count > before)
            {
                steps.Add((map.CurrentTick, after[^1].Position));
                idleTicks = 0;
            }
            else
            {
                idleTicks++;
            }
        }

        return steps;
    }
}
