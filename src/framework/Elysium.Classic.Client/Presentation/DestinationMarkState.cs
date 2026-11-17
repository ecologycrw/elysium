namespace Elysium.Classic.Client.Presentation;

/// <summary>What the mark on the clicked destination shows.</summary>
public enum DestinationMarkState
{
    /// <summary>No mark.</summary>
    Hidden,

    /// <summary>The player asked to walk there, and the server has not refused.</summary>
    Requested,

    /// <summary>The server refused the destination; the mark shows it for a while, then hides.</summary>
    Refused,
}
