using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Shared.Tests.Grid;

public sealed class GridMapTextTests
{
    private const string ValidMap = """
        # a small test map
        spawn 0 0
        spawn 3 1

        tiles
        ..#.
        ....
        """;

    [Fact]
    public void A_valid_map_is_read_with_size_blocked_tiles_and_spawns()
    {
        var map = GridMapText.Parse(ValidMap);

        Assert.Equal(4, map.Width);
        Assert.Equal(2, map.Height);
        Assert.False(map.IsWalkable(new TilePosition(2, 0)));
        Assert.True(map.IsWalkable(new TilePosition(3, 0)));
        Assert.Equal([new TilePosition(0, 0), new TilePosition(3, 1)], map.SpawnPoints);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 2)]
    public void Tiles_outside_the_map_are_not_walkable(int x, int y)
    {
        var map = GridMapText.Parse(ValidMap);

        Assert.False(map.Contains(new TilePosition(x, y)));
        Assert.False(map.IsWalkable(new TilePosition(x, y)));
    }

    [Theory]
    [InlineData("spawn 0 0\ntiles\n..\n...\n", "Line 4")]
    [InlineData("spawn 0 0\ntiles\n.x\n", "unknown tile")]
    [InlineData("spawn 0\ntiles\n..\n", "Line 1")]
    [InlineData("tiles\n..\n", "spawn point")]
    [InlineData("spawn 1 0\ntiles\n.#\n", "blocked")]
    [InlineData("spawn 5 5\ntiles\n..\n", "outside")]
    [InlineData("spawn 0 0\n", "no tiles")]
    public void An_invalid_map_is_refused_with_a_clear_message(string text, string expectedInMessage)
    {
        var exception = Assert.Throws<MapFormatException>(() => GridMapText.Parse(text));

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Windows_line_endings_are_accepted()
    {
        var map = GridMapText.Parse("spawn 0 0\r\ntiles\r\n...\r\n");

        Assert.Equal(3, map.Width);
        Assert.Equal(1, map.Height);
    }

    [Fact]
    public void Chebyshev_distance_counts_diagonal_steps_as_one()
    {
        Assert.Equal(3, new TilePosition(0, 0).ChebyshevDistance(new TilePosition(3, 2)));
        Assert.Equal(0, new TilePosition(4, 4).ChebyshevDistance(new TilePosition(4, 4)));
    }
}
