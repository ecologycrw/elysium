using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;

namespace Elysium.Classic.Client.World;

/// <summary>
/// One change to the <see cref="WorldState"/>, described for whoever shows it (the window, the bot's text).
/// A value type, so applying a message adds nothing for the garbage collector to clean up.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Entity">Who it happened to: another entity, or the player's own character (<see cref="WorldState.Self"/>).</param>
/// <param name="Name">The other entity's name; empty for the player's own character.</param>
/// <param name="Position">The tile: where it entered, appeared or stepped to, or the refused destination.</param>
/// <param name="RefusalReason">Why the server refused the destination (only for <see cref="WorldChangeKind.MoveRefused"/>).</param>
public readonly record struct WorldChange(
    WorldChangeKind Kind,
    EntityId Entity,
    string Name,
    TilePosition Position,
    MoveRejectReason RefusalReason = default);
