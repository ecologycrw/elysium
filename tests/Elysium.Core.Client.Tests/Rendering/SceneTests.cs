using System.Numerics;
using Elysium.Core.Client.Rendering;

namespace Elysium.Core.Client.Tests.Rendering;

public sealed class SceneTests
{
    private static readonly ColorRgb Red = new(1, 0, 0);
    private static readonly ColorRgb Blue = new(0, 0, 1);

    [Fact]
    public void Blocks_are_drawn_in_the_order_they_were_added()
    {
        var scene = new Scene();
        var first = new SceneBlock(new Vector3(1, 0, 1), Vector3.One, Red);
        var second = new SceneBlock(new Vector3(2, 0, 2), new Vector3(1, 2, 1), Blue);

        scene.Add(first);
        scene.Add(second);

        Assert.Equal([first, second], scene.Blocks.ToArray());
    }

    [Fact]
    public void A_cleared_scene_holds_only_what_the_next_frame_adds()
    {
        var scene = new Scene();
        scene.Add(new SceneBlock(Vector3.Zero, Vector3.One, Red));
        scene.Add(new SceneBlock(Vector3.UnitX, Vector3.One, Red));
        var next = new SceneBlock(Vector3.UnitZ, Vector3.One, Blue);

        scene.Clear();
        scene.Add(next);

        Assert.Equal([next], scene.Blocks.ToArray());
    }

    [Fact]
    [Trait("CA", "0001-CA-07")]
    public void Refilling_a_scene_of_the_same_size_allocates_nothing()
    {
        var scene = new Scene();
        Fill(scene, 500);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++)
        {
            Fill(scene, 500);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal(500, scene.Blocks.Length);
    }

    private static void Fill(Scene scene, int count)
    {
        scene.Clear();
        for (var i = 0; i < count; i++)
        {
            scene.Add(new SceneBlock(new Vector3(i, 0, 0), Vector3.One, Red));
        }
    }
}
