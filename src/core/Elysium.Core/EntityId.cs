using System.Globalization;

namespace Elysium.Core;

/// <summary>
/// Identifies an entity (a character, and later a monster or an item on the ground) while it exists on the server.
/// Systems refer to each other's entities by this ID, never by object reference.
/// Ordering by ID gives the simulation a stable, deterministic order (INV-04).
/// </summary>
public readonly record struct EntityId(uint Value) : IComparable<EntityId>
{
    public static bool operator <(EntityId left, EntityId right) => left.Value < right.Value;

    public static bool operator >(EntityId left, EntityId right) => left.Value > right.Value;

    public static bool operator <=(EntityId left, EntityId right) => left.Value <= right.Value;

    public static bool operator >=(EntityId left, EntityId right) => left.Value >= right.Value;

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
