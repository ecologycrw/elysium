using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// Who sees whom on one map. When an entity enters, steps or leaves, it works out who came into view and who
/// went out of view, keeps every <see cref="MapEntity.Visible"/> set symmetric, and tells both sides.
/// Single-threaded, owned by the map simulation; every loop goes in entity ID order (INV-04).
/// </summary>
internal sealed class AreaOfInterest
{
    private readonly int _viewRadius;
    private readonly IReadOnlyDictionary<EntityId, MapEntity> _entities;
    private readonly IClientOutbox _outbox;
    private readonly AreaOfInterestGrid _grid;
    private readonly SortedSet<EntityId> _inViewNow = [];
    private readonly List<EntityId> _outOfViewNow = [];

    public AreaOfInterest(int viewRadius, IReadOnlyDictionary<EntityId, MapEntity> entities, IClientOutbox outbox)
    {
        _viewRadius = viewRadius;
        _entities = entities;
        _outbox = outbox;
        _grid = new AreaOfInterestGrid(cellSize: viewRadius);
    }

    /// <summary>The entity was just added to the map: it and everyone in view see each other appear.</summary>
    public void Enter(MapEntity entity)
    {
        _grid.Add(entity.Id, entity.Position);
        Refresh(entity, stepped: false);
    }

    /// <summary>The entity stepped from <paramref name="from"/>: views are updated and onlookers see the step.</summary>
    public void Step(MapEntity entity, TilePosition from)
    {
        _grid.Move(entity.Id, from, entity.Position);
        Refresh(entity, stepped: true);
    }

    /// <summary>The entity was just removed from the map: everyone who saw it sees it disappear.</summary>
    public void Leave(MapEntity entity)
    {
        _grid.Remove(entity.Id, entity.Position);
        foreach (var otherId in entity.Visible)
        {
            _entities[otherId].Visible.Remove(entity.Id);
            _outbox.Send(otherId, new EntityDisappeared(entity.Id));
        }
    }

    private void Refresh(MapEntity entity, bool stepped)
    {
        FindInView(entity);
        LoseSightOfThoseOutOfView(entity);
        MeetOrShowStep(entity, stepped);
    }

    /// <summary>Fills <see cref="_inViewNow"/> with every other entity within the view radius.</summary>
    private void FindInView(MapEntity entity)
    {
        _inViewNow.Clear();
        _grid.Candidates(entity.Position, _inViewNow);
        _inViewNow.Remove(entity.Id);
        _inViewNow.RemoveWhere(otherId => entity.Position.ChebyshevDistance(_entities[otherId].Position) > _viewRadius);
    }

    private void LoseSightOfThoseOutOfView(MapEntity entity)
    {
        _outOfViewNow.Clear();
        foreach (var otherId in entity.Visible)
        {
            if (!_inViewNow.Contains(otherId))
            {
                _outOfViewNow.Add(otherId);
            }
        }

        foreach (var otherId in _outOfViewNow)
        {
            entity.Visible.Remove(otherId);
            _entities[otherId].Visible.Remove(entity.Id);
            _outbox.Send(entity.Id, new EntityDisappeared(otherId));
            _outbox.Send(otherId, new EntityDisappeared(entity.Id));
        }
    }

    private void MeetOrShowStep(MapEntity entity, bool stepped)
    {
        foreach (var otherId in _inViewNow)
        {
            var other = _entities[otherId];
            if (entity.Visible.Add(otherId))
            {
                other.Visible.Add(entity.Id);
                _outbox.Send(entity.Id, new EntityAppeared(otherId, other.Name, other.Position));
                _outbox.Send(otherId, new EntityAppeared(entity.Id, entity.Name, entity.Position));
            }
            else if (stepped)
            {
                _outbox.Send(otherId, new EntityMoved(entity.Id, entity.Position));
            }
        }
    }
}
