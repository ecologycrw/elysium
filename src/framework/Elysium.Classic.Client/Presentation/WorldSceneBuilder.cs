using System.Numerics;
using Elysium.Classic.Client.Camera;
using Elysium.Classic.Client.Movement;
using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;
using Elysium.Core.Client.Rendering;

namespace Elysium.Classic.Client.Presentation;

/// <summary>
/// Fills a <see cref="Scene"/> with the classic world: the ground of the map, blocked tiles as boxes, the
/// characters where they are shown, and the destination mark. Filling a scene that already held this world
/// does not allocate.
/// </summary>
public sealed class WorldSceneBuilder
{
    /// <summary>How thick the ground is under a walkable tile; its top is the ground plane (Y = 0).</summary>
    private const float GroundThickness = 0.1f;

    /// <summary>The space left between neighboring tiles, so the grid can be seen.</summary>
    private const float TileGap = 0.04f;

    private const float MarkWidth = 0.7f;
    private const float MarkThickness = 0.02f;

    private readonly GridMap _map;
    private readonly SceneStyle _style;

    public WorldSceneBuilder(GridMap map, SceneStyle style)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Replaces what <paramref name="scene"/> held with the world as it is shown this frame.</summary>
    public void Fill(Scene scene, Matrix4x4 viewProjection, WorldState world, WorldMotion motion, DestinationMark mark)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(motion);
        ArgumentNullException.ThrowIfNull(mark);
        scene.Clear();
        scene.ViewProjection = viewProjection;
        scene.Light = _style.Light;
        scene.Background = _style.Background;
        AddTerrain(scene);
        AddCharacters(scene, world, motion);
        AddMark(scene, mark);
    }

    private void AddTerrain(Scene scene)
    {
        const float width = 1 - TileGap;
        for (var row = 0; row < _map.Height; row++)
        {
            for (var column = 0; column < _map.Width; column++)
            {
                var tile = new TilePosition(column, row);
                var ground = TileSpace.ToWorld(tile);
                scene.Add(_map.IsWalkable(tile)
                    ? new SceneBlock(ground with { Y = -GroundThickness / 2 }, new Vector3(width, GroundThickness, width), _style.Ground)
                    : new SceneBlock(ground with { Y = _style.BlockedHeight / 2 }, new Vector3(width, _style.BlockedHeight, width), _style.Blocked));
            }
        }
    }

    private void AddCharacters(Scene scene, WorldState world, WorldMotion motion)
    {
        if (world.Self is { } self && motion.TryGetPosition(self, out var selfShown))
        {
            scene.Add(Character(selfShown, _style.Self));
        }

        foreach (var entity in world.VisibleEntities)
        {
            if (motion.TryGetPosition(entity.Id, out var shown))
            {
                scene.Add(Character(shown, _style.Others));
            }
        }
    }

    private SceneBlock Character(Vector2 shown, ColorRgb color) =>
        new(TileSpace.ToWorld(shown) with { Y = _style.CharacterSize.Y / 2 }, _style.CharacterSize, color);

    private void AddMark(Scene scene, DestinationMark mark)
    {
        if (mark.State == DestinationMarkState.Hidden)
        {
            return;
        }

        // The mark lies on whatever the tile shows: the ground, or the top of a blocked tile.
        var surface = _map.Contains(mark.Tile) && !_map.IsWalkable(mark.Tile) ? _style.BlockedHeight : 0;
        var color = mark.State == DestinationMarkState.Refused ? _style.RefusedDestination : _style.Destination;
        scene.Add(new SceneBlock(
            TileSpace.ToWorld(mark.Tile) with { Y = surface + (MarkThickness / 2) },
            new Vector3(MarkWidth, MarkThickness, MarkWidth),
            color));
    }
}
