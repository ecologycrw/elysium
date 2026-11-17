using System.Numerics;
using Elysium.Classic.Client.World;
using Elysium.Core;

namespace Elysium.Classic.Client.Movement;

/// <summary>
/// The shown positions of the player's own character and of every entity in view, kept in step with the
/// <see cref="WorldState"/> through its <see cref="WorldChange"/>s. Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class WorldMotion(MotionRules rules)
{
    private readonly MotionRules _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    private readonly Dictionary<EntityId, TileMotion> _motions = [];

    /// <summary>Follows one change of the world: a new entity starts at its tile, a step is queued, a gone entity is dropped.</summary>
    public void Apply(WorldChange change)
    {
        switch (change.Kind)
        {
            case WorldChangeKind.EnteredMap:
            case WorldChangeKind.EntityAppeared:
                _motions[change.Entity] = new TileMotion(change.Position);
                break;
            case WorldChangeKind.EntityMoved when _motions.TryGetValue(change.Entity, out var motion):
                motion.Confirm(change.Position);
                break;
            case WorldChangeKind.EntityDisappeared:
                _motions.Remove(change.Entity);
                break;
            default:
                break;
        }
    }

    /// <summary>Moves every shown position forward by the time since the last frame.</summary>
    public void Advance(TimeSpan elapsed)
    {
        foreach (var motion in _motions.Values)
        {
            motion.Advance(elapsed, _rules);
        }
    }

    /// <summary>Where to draw <paramref name="entity"/>, in tiles, if it is known.</summary>
    public bool TryGetPosition(EntityId entity, out Vector2 position)
    {
        if (_motions.TryGetValue(entity, out var motion))
        {
            position = motion.Position;
            return true;
        }

        position = default;
        return false;
    }
}
