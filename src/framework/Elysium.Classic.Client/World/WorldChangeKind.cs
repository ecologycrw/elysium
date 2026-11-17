namespace Elysium.Classic.Client.World;

/// <summary>What a message from the server changed in the <see cref="WorldState"/>.</summary>
public enum WorldChangeKind
{
    /// <summary>The player's own character entered the map.</summary>
    EnteredMap,

    /// <summary>Another entity came into view.</summary>
    EntityAppeared,

    /// <summary>An entity in view, or the player's own character, stepped to a new tile.</summary>
    EntityMoved,

    /// <summary>An entity left the view.</summary>
    EntityDisappeared,

    /// <summary>The server refused a destination the player asked for.</summary>
    MoveRefused,
}
