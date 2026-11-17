using Elysium.Classic.Shared.Grid;
using Elysium.Core.Protocol;

namespace Elysium.Classic.Shared.Messages;

/// <summary>
/// Writes and reads a tile position as two 16-bit numbers (column, row).
/// </summary>
public static class TileCodec
{
    public static void WriteTile(this MessageWriter writer, TilePosition tile)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (tile.X is < 0 or > GridMap.MaxSize || tile.Y is < 0 or > GridMap.MaxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(tile), tile, "Tile positions travel as numbers from 0 to 65535.");
        }

        writer.WriteUInt16((ushort)tile.X);
        writer.WriteUInt16((ushort)tile.Y);
    }

    public static TilePosition ReadTile(this ref MessageReader reader)
    {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        return new TilePosition(x, y);
    }
}
