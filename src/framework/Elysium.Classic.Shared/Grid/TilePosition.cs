using System.Globalization;

namespace Elysium.Classic.Shared.Grid;

/// <summary>
/// A tile on a grid map. X grows to the right (columns) and Y grows down (rows).
/// </summary>
public readonly record struct TilePosition(int X, int Y)
{
    /// <summary>
    /// Distance counting diagonal steps as one, which is how far a character walks in 8 directions.
    /// </summary>
    public int ChebyshevDistance(TilePosition other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y})");
}
