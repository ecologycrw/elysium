using Elysium.Classic.Shared.Grid;
using Elysium.Core;

namespace Elysium.Classic.Client.World;

/// <summary>Another entity in view, at the last tile the server confirmed.</summary>
public readonly record struct VisibleEntity(EntityId Id, string Name, TilePosition Position);
