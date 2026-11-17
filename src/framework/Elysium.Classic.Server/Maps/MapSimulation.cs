using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// The rules of one grid map: who is on it, where, and who sees whom.
/// Single-threaded and deterministic (INV-03, INV-04): time is the number of ticks run so far,
/// and every loop goes in entity ID order. It knows nothing about the network; it only queues
/// messages in the <see cref="IClientOutbox"/>.
/// </summary>
public sealed class MapSimulation
{
    private readonly GridMap _map;
    private readonly MapRules _rules;
    private readonly IClientOutbox _outbox;
    private readonly SortedDictionary<EntityId, MapEntity> _entities = [];
    private readonly AreaOfInterestGrid _grid;
    private readonly SortedSet<EntityId> _scratch = [];
    private readonly List<TilePosition> _pathScratch = [];

    public MapSimulation(GridMap map, MapRules rules, IClientOutbox outbox)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(outbox);
        _map = map;
        _rules = rules;
        _outbox = outbox;
        _grid = new AreaOfInterestGrid(rules.ViewRadius);
    }

    /// <summary>How many ticks have run. This is the simulation's only notion of time.</summary>
    public long CurrentTick { get; private set; }

    public int PlayerCount => _entities.Count;

    /// <summary>
    /// Puts a character on the map. Players enter at spawn point (players on the map) modulo (number of spawn points).
    /// </summary>
    public void Join(EntityId id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_entities.ContainsKey(id))
        {
            throw new InvalidOperationException($"Entity {id} is already on the map.");
        }

        var spawn = _map.SpawnPoints[_entities.Count % _map.SpawnPoints.Count];
        var entity = new MapEntity(id, name, spawn);
        _entities.Add(id, entity);
        _grid.Add(id, spawn);

        _outbox.Send(id, new EnteredMap(id, spawn));
        UpdateVisibility(entity, moved: false);
    }

    /// <summary>Takes a character off the map. Everyone who saw it is told it disappeared.</summary>
    public void Leave(EntityId id)
    {
        if (!_entities.Remove(id, out var entity))
        {
            return;
        }

        _grid.Remove(id, entity.Position);
        foreach (var otherId in entity.Visible)
        {
            _entities[otherId].Visible.Remove(id);
            _outbox.Send(otherId, new EntityDisappeared(id));
        }
    }

    /// <summary>Where the character is now, if it is on the map.</summary>
    public bool TryGetPosition(EntityId id, out TilePosition position)
    {
        if (_entities.TryGetValue(id, out var entity))
        {
            position = entity.Position;
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>
    /// The player asks to walk to <paramref name="destination"/>. The server checks it, finds the path and
    /// replaces any path in progress; the character then walks one tile per step interval, in <see cref="Tick"/>.
    /// A refused request is answered with <see cref="MoveRejected"/>, and the character stops where it is.
    /// </summary>
    public void RequestMove(EntityId id, TilePosition destination)
    {
        if (!_entities.TryGetValue(id, out var entity))
        {
            return;
        }

        MoveRejectReason? reason = null;
        if (!_map.Contains(destination))
        {
            reason = MoveRejectReason.OutOfMap;
        }
        else if (!_map.IsWalkable(destination))
        {
            reason = MoveRejectReason.Blocked;
        }
        else if (!GridPathfinder.TryFindPath(_map, entity.Position, destination, _rules.MaxPathLength, _pathScratch))
        {
            reason = MoveRejectReason.Unreachable;
        }

        entity.Path.Clear();
        if (reason is { } refused)
        {
            _outbox.Send(id, new MoveRejected(destination, refused));
            return;
        }

        foreach (var step in _pathScratch)
        {
            entity.Path.Enqueue(step);
        }
    }

    /// <summary>
    /// Runs one tick: every character with a path whose step interval has passed takes exactly one step.
    /// </summary>
    public void Tick()
    {
        CurrentTick++;
        foreach (var entity in _entities.Values)
        {
            if (entity.Path.Count == 0 || CurrentTick < entity.NextStepTick)
            {
                continue;
            }

            var from = entity.Position;
            var to = entity.Path.Dequeue();
            entity.Position = to;
            entity.NextStepTick = CurrentTick + _rules.TicksPerStep;
            _grid.Move(entity.Id, from, to);

            _outbox.Send(entity.Id, new EntityMoved(entity.Id, to));
            UpdateVisibility(entity, moved: true);
        }
    }

    /// <summary>
    /// Recomputes what <paramref name="entity"/> sees after it entered or stepped, and tells both sides about
    /// every change. When it <paramref name="moved"/>, everyone who still sees it is told its new position.
    /// </summary>
    private void UpdateVisibility(MapEntity entity, bool moved)
    {
        _scratch.Clear();
        _grid.Candidates(entity.Position, _scratch);
        _scratch.Remove(entity.Id);
        _scratch.RemoveWhere(otherId => entity.Position.ChebyshevDistance(_entities[otherId].Position) > _rules.ViewRadius);

        foreach (var otherId in entity.Visible.Where(otherId => !_scratch.Contains(otherId)).ToList())
        {
            entity.Visible.Remove(otherId);
            _entities[otherId].Visible.Remove(entity.Id);
            _outbox.Send(entity.Id, new EntityDisappeared(otherId));
            _outbox.Send(otherId, new EntityDisappeared(entity.Id));
        }

        foreach (var otherId in _scratch)
        {
            var other = _entities[otherId];
            if (entity.Visible.Add(otherId))
            {
                other.Visible.Add(entity.Id);
                _outbox.Send(entity.Id, new EntityAppeared(otherId, other.Name, other.Position));
                _outbox.Send(otherId, new EntityAppeared(entity.Id, entity.Name, entity.Position));
            }
            else if (moved)
            {
                _outbox.Send(otherId, new EntityMoved(entity.Id, entity.Position));
            }
        }
    }
}
