using System.Numerics;
using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Client.Movement;

/// <summary>
/// The shown position of one entity, sliding from tile to tile behind the server. It only goes to tiles the
/// server confirmed, one step per <see cref="MotionRules.StepDuration"/>, and never past the last one (INV-01).
/// When it falls more than <see cref="MotionRules.MaxStepsBehind"/> steps behind, it jumps ahead instead of
/// piling up delay. Advancing it does not allocate.
/// </summary>
public sealed class TileMotion
{
    private readonly Queue<TilePosition> _pending = new();
    private TilePosition _from;
    private double _progress;

    public TileMotion(TilePosition start)
    {
        _from = start;
        LastConfirmed = start;
    }

    /// <summary>The last tile the server confirmed.</summary>
    public TilePosition LastConfirmed { get; private set; }

    /// <summary>True while there are confirmed steps still being shown.</summary>
    public bool IsMoving => _pending.Count > 0;

    /// <summary>Where to draw the entity, in tiles: X is the column and Y the row, with fractions between tiles.</summary>
    public Vector2 Position => _pending.TryPeek(out var next)
        ? Vector2.Lerp(ToVector(_from), ToVector(next), (float)_progress)
        : ToVector(_from);

    /// <summary>The server confirmed a step to <paramref name="tile"/>.</summary>
    public void Confirm(TilePosition tile)
    {
        _pending.Enqueue(tile);
        LastConfirmed = tile;
    }

    /// <summary>Moves the shown position forward by <paramref name="elapsed"/>, one step per step duration.</summary>
    public void Advance(TimeSpan elapsed, MotionRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        CatchUp(rules.MaxStepsBehind);
        if (_pending.Count == 0)
        {
            return;
        }

        _progress += elapsed / rules.StepDuration;
        while (_progress >= 1 && _pending.TryDequeue(out var reached))
        {
            _from = reached;
            _progress -= 1;
        }

        if (_pending.Count == 0)
        {
            _progress = 0;
        }
    }

    /// <summary>Drops the oldest steps still to be shown until at most <paramref name="maxStepsBehind"/> remain.</summary>
    private void CatchUp(int maxStepsBehind)
    {
        while (_pending.Count > maxStepsBehind)
        {
            _from = _pending.Dequeue();
            _progress = 0;
        }
    }

    private static Vector2 ToVector(TilePosition tile) => new(tile.X, tile.Y);
}
