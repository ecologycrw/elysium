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
/// </summary>
public static class GridMapText
{
    private const char Walkable = '.';
    private const char Blocked = '#';

    public static GridMap Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var spawns = new List<TilePosition>();
        var index = 0;

        for (; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line == "tiles")
            {
                index++;
                break;
            }

            spawns.Add(ParseSpawn(line, index + 1));
        }

        var blocked = new List<TilePosition>();
        var width = -1;
        var height = 0;
        for (; index < lines.Length; index++)
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

            for (var x = 0; x < row.Length; x++)
            {
                switch (row[x])
                {
                    case Walkable:
                        break;
                    case Blocked:
                        blocked.Add(new TilePosition(x, height));
                        break;
                    default:
                        throw new MapFormatException($"Line {index + 1}: unknown tile '{row[x]}' (use '{Walkable}' or '{Blocked}').");
                }
            }

            height++;
        }

        if (height == 0)
        {
            throw new MapFormatException("The map has no tiles (they come after a line with 'tiles').");
        }

        try
        {
            return new GridMap(width, height, blocked, spawns);
        }
        catch (ArgumentException exception)
        {
            throw new MapFormatException(exception.Message, exception);
        }
    }

    private static TilePosition ParseSpawn(string line, int lineNumber)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || parts[0] != "spawn"
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var x)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var y))
        {
            throw new MapFormatException($"Line {lineNumber}: expected 'spawn <column> <row>' or 'tiles', found '{line}'.");
        }

        return new TilePosition(x, y);
    }
}
