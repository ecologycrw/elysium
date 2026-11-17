using System.Numerics;
using Elysium.Classic.Client.World;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Presentation;

/// <summary>
/// The mark on the tile the player clicked to walk to. It shows the request until the character gets there,
/// or the server's refusal for <see cref="SceneStyle.RefusedDestinationTime"/>. Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class DestinationMark(TimeSpan refusedTime)
{
    private TimeSpan _refusedLeft;

    public DestinationMarkState State { get; private set; }

    /// <summary>The marked tile; meaningless while <see cref="State"/> is <see cref="DestinationMarkState.Hidden"/>.</summary>
    public TilePosition Tile { get; private set; }

    /// <summary>The player asked to walk to <paramref name="tile"/>: the mark moves there, replacing any earlier one.</summary>
    public void Show(TilePosition tile)
    {
        Tile = tile;
        State = DestinationMarkState.Requested;
    }

    /// <summary>Follows one change of the world: a refusal of the marked tile turns the mark into a refusal.</summary>
    public void Apply(WorldChange change)
    {
        if (change.Kind == WorldChangeKind.MoveRefused && State == DestinationMarkState.Requested && change.Position == Tile)
        {
            State = DestinationMarkState.Refused;
            _refusedLeft = refusedTime;
        }
    }

    /// <summary>
    /// Moves time forward: a requested mark hides once the player's own character is shown on it, and a refused
    /// mark hides when its time is over.
    /// </summary>
    /// <param name="elapsed">The time since the last frame.</param>
    /// <param name="selfShown">Where the player's own character is drawn, in tiles; null before it entered the map.</param>
    public void Advance(TimeSpan elapsed, Vector2? selfShown)
    {
        switch (State)
        {
            case DestinationMarkState.Requested when selfShown == new Vector2(Tile.X, Tile.Y):
                State = DestinationMarkState.Hidden;
                break;
            case DestinationMarkState.Refused:
                _refusedLeft -= elapsed;
                if (_refusedLeft <= TimeSpan.Zero)
                {
                    State = DestinationMarkState.Hidden;
                }

                break;
            default:
                break;
        }
    }
}
