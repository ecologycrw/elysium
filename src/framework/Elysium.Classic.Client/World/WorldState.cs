using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Client.World;

/// <summary>
/// What one client knows about the world, built only from what the server sent (INV-01): its own character
/// and the entities in view, each at the last tile the server confirmed. The bot and the graphical client use it.
/// Not thread-safe: one thread applies messages and reads it.
/// </summary>
public sealed class WorldState
{
    private readonly Dictionary<EntityId, VisibleEntity> _visible = [];

    /// <summary>The player's own character, once it entered the map.</summary>
    public EntityId? Self { get; private set; }

    /// <summary>The tile of the player's own character, once it entered the map.</summary>
    public TilePosition? SelfPosition { get; private set; }

    /// <summary>The other entities in view. Enumerating it does not allocate.</summary>
    public Dictionary<EntityId, VisibleEntity>.ValueCollection VisibleEntities => _visible.Values;

    /// <summary>How many destinations the server refused so far.</summary>
    public int MovesRefused { get; private set; }

    /// <summary>True for the message types this state understands; others (a game's own messages) go elsewhere.</summary>
    public static bool Handles(ushort messageType) => messageType is
        ClassicMessageTypes.EnteredMap or
        ClassicMessageTypes.EntityAppeared or
        ClassicMessageTypes.EntityMoved or
        ClassicMessageTypes.EntityDisappeared or
        ClassicMessageTypes.MoveRejected;

    public bool TryGetEntity(EntityId id, out VisibleEntity entity) => _visible.TryGetValue(id, out entity);

    /// <summary>
    /// Applies one message from the server and says what changed, or returns null when it changed nothing
    /// (for example, a step of an entity that is not in view). Throws <see cref="ProtocolException"/> for a
    /// malformed message or one this state does not <see cref="Handles"/>.
    /// </summary>
    public WorldChange? Apply(Frame frame) => frame.Type switch
    {
        ClassicMessageTypes.EnteredMap => ApplyEnteredMap(frame.Decode<EnteredMap>()),
        ClassicMessageTypes.EntityAppeared => ApplyAppeared(frame.Decode<EntityAppeared>()),
        ClassicMessageTypes.EntityMoved => ApplyMoved(frame.Decode<EntityMoved>()),
        ClassicMessageTypes.EntityDisappeared => ApplyDisappeared(frame.Decode<EntityDisappeared>()),
        ClassicMessageTypes.MoveRejected => ApplyMoveRejected(frame.Decode<MoveRejected>()),
        _ => throw new ProtocolException($"Message type {frame.Type} is not a world message."),
    };

    private WorldChange ApplyEnteredMap(EnteredMap entered)
    {
        Self = entered.Self;
        SelfPosition = entered.Position;
        return new WorldChange(WorldChangeKind.EnteredMap, entered.Self, string.Empty, entered.Position);
    }

    private WorldChange ApplyAppeared(EntityAppeared appeared)
    {
        _visible[appeared.Id] = new VisibleEntity(appeared.Id, appeared.Name, appeared.Position);
        return new WorldChange(WorldChangeKind.EntityAppeared, appeared.Id, appeared.Name, appeared.Position);
    }

    private WorldChange? ApplyMoved(EntityMoved moved)
    {
        if (moved.Id == Self)
        {
            SelfPosition = moved.Position;
            return new WorldChange(WorldChangeKind.EntityMoved, moved.Id, string.Empty, moved.Position);
        }

        if (!_visible.TryGetValue(moved.Id, out var known))
        {
            return null;
        }

        _visible[moved.Id] = known with { Position = moved.Position };
        return new WorldChange(WorldChangeKind.EntityMoved, moved.Id, known.Name, moved.Position);
    }

    private WorldChange? ApplyDisappeared(EntityDisappeared gone) =>
        _visible.Remove(gone.Id, out var lost)
            ? new WorldChange(WorldChangeKind.EntityDisappeared, gone.Id, lost.Name, lost.Position)
            : null;

    private WorldChange ApplyMoveRejected(MoveRejected rejected)
    {
        MovesRefused++;
        return new WorldChange(WorldChangeKind.MoveRefused, Self ?? default, string.Empty, rejected.Destination, rejected.Reason);
    }
}
