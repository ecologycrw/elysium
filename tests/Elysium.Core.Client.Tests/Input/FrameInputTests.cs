using System.Numerics;
using Elysium.Core.Client.Input;

namespace Elysium.Core.Client.Tests.Input;

public sealed class FrameInputTests
{
    [Fact]
    public void Clicks_of_a_frame_are_read_in_order_and_forgotten_by_the_next_frame()
    {
        var input = new FrameInput();
        input.BeginFrame();
        input.AddClick(new Vector2(10, 20), PointerButton.Primary);
        input.AddClick(new Vector2(30, 40), PointerButton.Secondary);

        Assert.Equal(
            [new PointerClick(new Vector2(10, 20), PointerButton.Primary), new PointerClick(new Vector2(30, 40), PointerButton.Secondary)],
            input.Clicks.ToArray());

        input.BeginFrame();

        Assert.True(input.Clicks.IsEmpty);
    }

    [Fact]
    public void A_minimized_window_still_has_a_drawing_area_of_at_least_one_pixel()
    {
        var input = new FrameInput();

        input.SetViewport(1280, 720);
        Assert.Equal((1280, 720), (input.ViewportWidth, input.ViewportHeight));

        input.SetViewport(0, 0);
        Assert.Equal((1, 1), (input.ViewportWidth, input.ViewportHeight));
    }

    [Fact]
    public void A_request_to_close_lasts_across_frames()
    {
        var input = new FrameInput();

        input.RequestClose();
        input.BeginFrame();

        Assert.True(input.CloseRequested);
    }

    [Fact]
    [Trait("CA", "0001-CA-07")]
    public void Reading_clicks_frame_after_frame_allocates_nothing()
    {
        var input = new FrameInput();
        RunFrame(input);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 200; frame++)
        {
            RunFrame(input);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void RunFrame(FrameInput input)
    {
        input.BeginFrame();
        input.SetViewport(1280, 720);
        input.AddClick(new Vector2(1, 2), PointerButton.Primary);
        input.AddClick(new Vector2(3, 4), PointerButton.Primary);
    }
}
