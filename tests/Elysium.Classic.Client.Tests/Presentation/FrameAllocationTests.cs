using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Client.Rendering;
using Elysium.Core.Protocol;
using static Elysium.Classic.Client.Tests.Presentation.TestWorld;

namespace Elysium.Classic.Client.Tests.Presentation;

public sealed class FrameAllocationTests
{
    private const int FramesPerStep = 15;
    private const int WarmUpFrames = 20 * FramesPerStep;
    private const int MeasuredFrames = 40 * FramesPerStep;

    private static readonly TimeSpan FrameTime = Step / FramesPerStep;

    [Fact]
    [Trait("CA", "0001-CA-07")]
    public void After_warming_up_a_frame_allocates_nothing()
    {
        var world = Create();
        var scene = new Scene();
        world.Apply(Frame.FromMessage(new EnteredMap(Ana, new TilePosition(3, 3))));
        world.Apply(Frame.FromMessage(new EntityAppeared(Bruno, "Bruno", new TilePosition(3, 6))));
        var traffic = new ServerTraffic();

        RunFrames(world, scene, traffic, WarmUpFrames);
        var before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(world, scene, traffic, MeasuredFrames);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(world.State.MovesRefused > WarmUpFrames / FramesPerStep, "the measured frames must include refused destinations");
    }

    /// <summary>
    /// One frame as the client runs it: take what the server sent, click now and then, move time forward and
    /// fill the scene. Both characters walk back and forth, one step per step time, and every click is on the
    /// wall, which the server refuses.
    /// </summary>
    private static void RunFrames(ClientWorld world, Scene scene, ServerTraffic traffic, int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            if (traffic.FrameNumber % FramesPerStep == 0)
            {
                foreach (var message in traffic.NextStep())
                {
                    world.Apply(message);
                }

                Assert.True(world.Camera.TryProject(TileSpace.ToWorld(Wall) with { Y = Style.BlockedHeight }, out var wallPixel));
                Assert.True(world.TryClickGround(wallPixel, out _));
            }

            world.Advance(FrameTime, ViewportWidth, ViewportHeight);
            world.Fill(scene);
            traffic.FrameNumber++;
        }
    }

    /// <summary>The messages the server sends at each step, encoded once before the test measures anything.</summary>
    private sealed class ServerTraffic
    {
        private readonly Frame[][] _steps;
        private int _next;

        public ServerTraffic()
        {
            // Ana walks along row 3 and Bruno along row 6, four tiles forth and four back.
            int[] columns = [4, 5, 6, 7, 6, 5, 4, 3];
            _steps = [.. columns.Select(column => new[]
            {
                Frame.FromMessage(new EntityMoved(Ana, new TilePosition(column, 3))),
                Frame.FromMessage(new EntityMoved(Bruno, new TilePosition(column, 6))),
                Frame.FromMessage(new MoveRejected(Wall, MoveRejectReason.Blocked)),
            })];
        }

        public int FrameNumber { get; set; }

        public Frame[] NextStep()
        {
            var step = _steps[_next];
            _next = (_next + 1) % _steps.Length;
            return step;
        }
    }
}
