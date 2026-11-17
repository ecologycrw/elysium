namespace Elysium.Classic.Shared.Grid;

/// <summary>
/// Thrown when the text of a map file is invalid. The message says which line is wrong.
/// </summary>
public sealed class MapFormatException : Exception
{
    public MapFormatException()
    {
    }

    public MapFormatException(string message)
        : base(message)
    {
    }

    public MapFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
