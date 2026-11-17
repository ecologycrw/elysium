using System.Globalization;

namespace Elysium.Core;

/// <summary>
/// Identifies an entity (a character, and later a monster or an item on the ground) while it exists on the server.
/// Systems refer to each other's entities by this ID, never by object reference.
/// </summary>
public readonly record struct EntityId(uint Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
