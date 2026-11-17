using System.Globalization;

namespace Elysium.Classic.Shared.Grid;

/// <summary>
/// Reads a grid map from text. The format:
/// <code>
/// # comments and blank lines are ignored before the tiles
/// spawn 2 3        (one line per spawn point: column and row)
/// tiles
/// ....#....        (one line per row: '.' is walkable, '#' is blocked; all rows have the same width)
/// </code>
/// Errors are reported as <see cref="MapFormatException"/> with the line number.
/// </summary>
public static class GridMapText
{
    private const string TilesKeyword = "tiles";
    private const string SpawnKeyword = "spawn";
    private const char Walkable = '.';
    private const char Blocked = '#';

    public static GridMap Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var tilesLine = Array.FindIndex(lines, line => line.Trim() == TilesKeyword);
        if (tilesLine < 0)
        {
            throw new MapFormatException($"The map has no tiles (they come after a line with '{TilesKeyword}').");
        }

        var spawns = ParseSpawns(lines.AsSpan(0, tilesLine));
        var tiles = ParseTiles(lines, firstRow: tilesLine + 1);
        try
        {
            return new GridMap(tiles.Width, tiles.Height, tiles.Blocked, spawns);
        }
        catch (ArgumentException exception)
        {
            throw new MapFormatException(exception.Message, exception);
        }
    }

    /// <summary>Reads the lines before 'tiles': spawn points, comments and blank lines.</summary>
    private static List<TilePosition> ParseSpawns(ReadOnlySpan<string> header)
    {
        var spawns = new List<TilePosition>();
        for (var index = 0; index < header.Length; index++)
        {
            var line = header[index].Trim();
            if (line.Length > 0 && !line.StartsWith('#'))
            {
                spawns.Add(ParseSpawn(line, lineNumber: index + 1));
            }
        }

        return spawns;
    }

    private static TilePosition ParseSpawn(string line, int lineNumber)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || parts[0] != SpawnKeyword
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var x)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var y))
        {
            throw new MapFormatException($"Line {lineNumber}: expected '{SpawnKeyword} <column> <row>' or '{TilesKeyword}', found '{line}'.");
        }

        return new TilePosition(x, y);
    }

    /// <summary>Reads the tile rows that start at <paramref name="firstRow"/>, skipping blank lines.</summary>
    private static TileRows ParseTiles(string[] lines, int firstRow)
    {
        var blocked = new List<TilePosition>();
        var width = -1;
        var height = 0;
        for (var index = firstRow; index < lines.Length; index++)
        {
            var row = lines[index].TrimEnd();
            if (row.Length == 0)
            {
                continue;
            }

            if (width == -1)
            {
                width = row.Length;
            }
            else if (row.Length != width)
            {
                throw new MapFormatException($"Line {index + 1}: every row must have {width} tiles, this one has {row.Length}.");
            }

            AddBlockedTiles(row, rowIndex: height, lineNumber: index + 1, blocked);
            height++;
        }

        if (height == 0)
        {
            throw new MapFormatException($"The map has no tiles (they come after a line with '{TilesKeyword}').");
        }

        return new TileRows(width, height, blocked);
    }

    private static void AddBlockedTiles(string row, int rowIndex, int lineNumber, List<TilePosition> blocked)
    {
        for (var x = 0; x < row.Length; x++)
        {
            switch (row[x])
            {
                case Walkable:
                    break;
                case Blocked:
                    blocked.Add(new TilePosition(x, rowIndex));
                    break;
                default:
                    throw new MapFormatException($"Line {lineNumber}: unknown tile '{row[x]}' (use '{Walkable}' or '{Blocked}').");
            }
        }
    }

    private sealed record TileRows(int Width, int Height, List<TilePosition> Blocked);
}
