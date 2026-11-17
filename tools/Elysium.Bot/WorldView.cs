using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Bot;

/// <summary>
/// What one bot knows about the world: its own position and the entities in view, built only from what the
/// server sent. Provisional home: this moves to Elysium.Classic.Client in slice 1 (docs/TECH_DEBT.md).
/// </summary>
public sealed class WorldView
{
    private readonly Dictionary<EntityId, (string Name, TilePosition Position)> _visible = [];

    public EntityId? Self { get; private set; }

    public TilePosition? Position { get; private set; }

    public IReadOnlyCollection<string> VisibleNames => [.. _visible.Values.Select(entity => entity.Name).Order(StringComparer.Ordinal)];

    public bool Sees(string name) => _visible.Values.Any(entity => entity.Name == name);

    /// <summary>
    /// Applies one message from the server. Returns a line describing what changed, or null when nothing worth telling happened.
    /// </summary>
    public string? Apply(Frame frame)
    {
        switch (frame.Type)
        {
            case ClassicMessageTypes.EnteredMap:
                var entered = frame.Decode<EnteredMap>();
                Self = entered.Self;
                Position = entered.Position;
                return $"entered the map at {entered.Position}";
            case ClassicMessageTypes.EntityAppeared:
                var appeared = frame.Decode<EntityAppeared>();
                _visible[appeared.Id] = (appeared.Name, appeared.Position);
                return $"sees {appeared.Name} at {appeared.Position}";
            case ClassicMessageTypes.EntityMoved:
                var moved = frame.Decode<EntityMoved>();
                if (moved.Id == Self)
                {
                    Position = moved.Position;
                }
                else if (_visible.TryGetValue(moved.Id, out var known))
                {
                    _visible[moved.Id] = (known.Name, moved.Position);
                }

                return null;
            case ClassicMessageTypes.EntityDisappeared:
                var gone = frame.Decode<EntityDisappeared>();
                return _visible.Remove(gone.Id, out var lost) ? $"lost sight of {lost.Name}" : null;
            case ClassicMessageTypes.MoveRejected:
                var rejected = frame.Decode<MoveRejected>();
                return $"could not walk to {rejected.Destination}: {rejected.Reason}";
            default:
                return $"received unknown message type {frame.Type}";
        }
    }
}
