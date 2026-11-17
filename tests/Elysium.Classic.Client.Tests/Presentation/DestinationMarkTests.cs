using System.Numerics;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;

namespace Elysium.Classic.Client.Tests.Presentation;

public sealed class DestinationMarkTests
{
    private static readonly TimeSpan RefusedTime = TimeSpan.FromSeconds(1);
    private static readonly TilePosition Clicked = new(8, 3);

    [Fact]
    public void A_clicked_destination_stays_marked_until_the_character_is_shown_on_it()
    {
        var mark = new DestinationMark(RefusedTime);

        mark.Show(Clicked);
        mark.Advance(TimeSpan.FromSeconds(10), selfShown: new Vector2(7.5f, 3));

        Assert.Equal(DestinationMarkState.Requested, mark.State);
        Assert.Equal(Clicked, mark.Tile);

        mark.Advance(TimeSpan.FromMilliseconds(16), selfShown: new Vector2(8, 3));

        Assert.Equal(DestinationMarkState.Hidden, mark.State);
    }

    [Fact]
    [Trait("CA", "0001-CA-04")]
    public void A_refused_destination_shows_the_refusal_for_a_while_then_hides()
    {
        var mark = new DestinationMark(RefusedTime);
        mark.Show(Clicked);

        mark.Apply(Refusal(Clicked));
        mark.Advance(RefusedTime - TimeSpan.FromMilliseconds(1), selfShown: new Vector2(5, 5));

        Assert.Equal(DestinationMarkState.Refused, mark.State);
        Assert.Equal(Clicked, mark.Tile);

        mark.Advance(TimeSpan.FromMilliseconds(1), selfShown: new Vector2(5, 5));

        Assert.Equal(DestinationMarkState.Hidden, mark.State);
    }

    [Fact]
    public void A_refusal_of_an_earlier_click_does_not_touch_the_newer_mark()
    {
        var mark = new DestinationMark(RefusedTime);
        mark.Show(new TilePosition(1, 1));
        mark.Show(Clicked);

        mark.Apply(Refusal(new TilePosition(1, 1)));

        Assert.Equal(DestinationMarkState.Requested, mark.State);
        Assert.Equal(Clicked, mark.Tile);
    }

    [Fact]
    public void A_new_click_replaces_a_refused_mark()
    {
        var mark = new DestinationMark(RefusedTime);
        mark.Show(new TilePosition(1, 1));
        mark.Apply(Refusal(new TilePosition(1, 1)));

        mark.Show(Clicked);

        Assert.Equal(DestinationMarkState.Requested, mark.State);
        Assert.Equal(Clicked, mark.Tile);
    }

    [Fact]
    public void Nothing_is_marked_before_the_first_click()
    {
        var mark = new DestinationMark(RefusedTime);

        mark.Apply(Refusal(Clicked));
        mark.Advance(TimeSpan.FromSeconds(1), selfShown: null);

        Assert.Equal(DestinationMarkState.Hidden, mark.State);
    }

    private static WorldChange Refusal(TilePosition tile) =>
        new(WorldChangeKind.MoveRefused, TestWorld.Ana, string.Empty, tile, MoveRejectReason.Blocked);
}
