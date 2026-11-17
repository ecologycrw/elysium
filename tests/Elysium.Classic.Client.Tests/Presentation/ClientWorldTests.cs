using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Presentation;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core.Client.Rendering;
using Elysium.Core.Protocol;
using static Elysium.Classic.Client.Tests.Presentation.TestWorld;

namespace Elysium.Classic.Client.Tests.Presentation;

public sealed class ClientWorldTests
{
    private static readonly TilePosition AnaStart = new(5, 5);
    private static readonly TimeSpan Frame60 = TimeSpan.FromSeconds(1.0 / 60);

    [Fact]
    public void The_scene_has_every_tile_with_blocked_tiles_standing_taller_in_their_own_color()
    {
        var world = EnteredAsAna();

        var scene = Filled(world);

        var terrain = scene.Blocks.ToArray().Where(block => block.Color == Style.Ground || block.Color == Style.Blocked).ToList();
        Assert.Equal(Width * Height, terrain.Count);
        var wall = Assert.Single(terrain, block => block.Color == Style.Blocked);
        Assert.Equal(new Vector2(Wall.X, Wall.Y), new Vector2(wall.Center.X, wall.Center.Z));
        Assert.Equal(Style.BlockedHeight, Top(wall), precision: 5);
        Assert.Equal(0, Bottom(wall), precision: 5);
        Assert.All(terrain.Where(block => block.Color == Style.Ground), ground => Assert.Equal(0, Top(ground), precision: 5));
    }

    [Fact]
    public void The_scene_carries_the_camera_light_and_background()
    {
        var world = EnteredAsAna();

        var scene = Filled(world);

        Assert.Equal(world.Camera.ViewProjection, scene.ViewProjection);
        Assert.Equal(Style.Light, scene.Light);
        Assert.Equal(Style.Background, scene.Background);
    }

    [Fact]
    public void The_own_character_and_the_others_stand_on_the_ground_where_they_are_shown_each_in_its_color()
    {
        var world = EnteredAsAna();
        world.Apply(Message(new EntityAppeared(Bruno, "Bruno", new TilePosition(8, 6))));
        world.Apply(Message(new EntityMoved(Bruno, new TilePosition(9, 6))));

        world.Advance(Step / 2, ViewportWidth, ViewportHeight);
        var scene = Filled(world);

        var ana = Assert.Single(scene.Blocks.ToArray(), block => block.Color == Style.Self);
        var bruno = Assert.Single(scene.Blocks.ToArray(), block => block.Color == Style.Others);
        Assert.Equal(new Vector3(AnaStart.X, Style.CharacterSize.Y / 2, AnaStart.Y), ana.Center);
        Assert.Equal(new Vector3(8.5f, Style.CharacterSize.Y / 2, 6), bruno.Center);
        Assert.Equal(Style.CharacterSize, bruno.Size);
    }

    [Fact]
    public void A_character_that_left_the_view_is_no_longer_in_the_scene()
    {
        var world = EnteredAsAna();
        world.Apply(Message(new EntityAppeared(Bruno, "Bruno", new TilePosition(8, 6))));

        world.Apply(Message(new EntityDisappeared(Bruno)));
        var scene = Filled(world);

        Assert.DoesNotContain(scene.Blocks.ToArray(), block => block.Color == Style.Others);
    }

    [Fact]
    public void The_camera_follows_the_own_character_as_it_slides()
    {
        var world = EnteredAsAna();
        world.Apply(Message(new EntityMoved(Ana, new TilePosition(6, 5))));

        world.Advance(Step / 2, ViewportWidth, ViewportHeight);

        Assert.Equal(TileSpace.ToWorld(new Vector2(5.5f, 5)), world.Camera.Target);
    }

    [Fact]
    [Trait("CA", "0001-CA-04")]
    public void A_click_on_a_blocked_tile_marks_it_and_the_refusal_shows_on_top_of_it_while_the_character_stays()
    {
        var world = EnteredAsAna();
        Assert.True(world.Camera.TryProject(TileSpace.ToWorld(Wall) with { Y = Style.BlockedHeight }, out var pixel));

        Assert.True(world.TryClickGround(pixel, out var destination));
        Assert.Equal(Wall, destination);
        Assert.Equal(Style.Destination, Mark(Filled(world)).Color);

        world.Apply(Message(new MoveRejected(Wall, MoveRejectReason.Blocked)));
        world.Advance(Frame60, ViewportWidth, ViewportHeight);
        var scene = Filled(world);

        var mark = Mark(scene);
        Assert.Equal(Style.RefusedDestination, mark.Color);
        Assert.Equal(new Vector2(Wall.X, Wall.Y), new Vector2(mark.Center.X, mark.Center.Z));
        Assert.Equal(Style.BlockedHeight, Bottom(mark), precision: 5);
        var ana = Assert.Single(scene.Blocks.ToArray(), block => block.Color == Style.Self);
        Assert.Equal(new Vector2(AnaStart.X, AnaStart.Y), new Vector2(ana.Center.X, ana.Center.Z));
    }

    [Fact]
    public void The_mark_of_an_accepted_destination_goes_away_when_the_character_gets_there()
    {
        var world = EnteredAsAna();
        var target = new TilePosition(6, 5);
        Assert.True(world.Camera.TryProject(TileSpace.ToWorld(target), out var pixel));
        Assert.True(world.TryClickGround(pixel, out _));

        world.Apply(Message(new EntityMoved(Ana, target)));
        world.Advance(Step, ViewportWidth, ViewportHeight);

        Assert.Equal(DestinationMarkState.Hidden, world.Destination.State);
        Assert.DoesNotContain(Filled(world).Blocks.ToArray(), block => block.Color == Style.Destination);
    }

    [Fact]
    public void Clicks_before_entering_the_map_are_ignored()
    {
        var world = Create();
        world.Advance(Frame60, ViewportWidth, ViewportHeight);

        Assert.False(world.TryClickGround(new Vector2(ViewportWidth / 2f, ViewportHeight / 2f), out _));
        Assert.Equal(DestinationMarkState.Hidden, world.Destination.State);
    }

    private static ClientWorld EnteredAsAna()
    {
        var world = Create();
        world.Apply(Message(new EnteredMap(Ana, AnaStart)));
        world.Advance(TimeSpan.Zero, ViewportWidth, ViewportHeight);
        return world;
    }

    private static Frame Message(IMessage message) => Frame.FromMessage(message);

    private static Scene Filled(ClientWorld world)
    {
        var scene = new Scene();
        world.Fill(scene);
        return scene;
    }

    private static SceneBlock Mark(Scene scene) =>
        Assert.Single(scene.Blocks.ToArray(), block => block.Color == Style.Destination || block.Color == Style.RefusedDestination);

    private static float Top(SceneBlock block) => block.Center.Y + (block.Size.Y / 2);

    private static float Bottom(SceneBlock block) => block.Center.Y - (block.Size.Y / 2);
}
