using Elysium.Classic.Shared.Grid;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// Finds a shortest path on a grid map in 8 directions.
/// A breadth-first search from the goal measures how many steps each tile is from it; the path then walks
/// from the start, always to a neighbor one step closer, choosing among them the one nearest to the goal
/// in a straight line. That keeps paths straight instead of zigzagging.
/// A diagonal step is allowed only when both tiles beside it are walkable, so paths never cut wall corners.
/// The search never looks further than <c>maxLength</c> tiles from the start, which bounds its cost.
/// </summary>
internal static class GridPathfinder
{
    private static readonly (int X, int Y)[] Directions =
    [
        (0, -1), (1, 0), (0, 1), (-1, 0), (1, -1), (1, 1), (-1, 1), (-1, -1),
    ];

    /// <summary>
    /// Fills <paramref name="path"/> with the steps from <paramref name="start"/> (not included) to
    /// <paramref name="goal"/> (included). Returns false when there is no path of at most <paramref name="maxLength"/> steps.
    /// </summary>
    public static bool TryFindPath(GridMap map, TilePosition start, TilePosition goal, int maxLength, List<TilePosition> path)
    {
        path.Clear();
        if (start == goal)
        {
            return true;
        }

        if (!map.IsWalkable(start) || !map.IsWalkable(goal) || start.ChebyshevDistance(goal) > maxLength)
        {
            return false;
        }

        var stepsToGoal = MeasureFromGoal(map, start, goal, maxLength);
        if (!stepsToGoal.TryGetValue(start, out var remaining) || remaining > maxLength)
        {
            return false;
        }

        var current = start;
        while (current != goal)
        {
            TilePosition? best = null;
            var bestDistance = long.MaxValue;
            foreach (var (dx, dy) in Directions)
            {
                var next = new TilePosition(current.X + dx, current.Y + dy);
                if (!stepsToGoal.TryGetValue(next, out var steps) || steps != remaining - 1 || !CanStep(map, current, dx, dy))
                {
                    continue;
                }

                var distance = SquaredDistance(next, goal);
                if (distance < bestDistance)
                {
                    best = next;
                    bestDistance = distance;
                }
            }

            current = best!.Value;
            remaining--;
            path.Add(current);
        }

        return true;
    }

    private static Dictionary<TilePosition, int> MeasureFromGoal(GridMap map, TilePosition start, TilePosition goal, int maxLength)
    {
        var stepsToGoal = new Dictionary<TilePosition, int> { [goal] = 0 };
        var frontier = new Queue<TilePosition>();
        frontier.Enqueue(goal);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == start)
            {
                break;
            }

            var steps = stepsToGoal[current] + 1;
            foreach (var (dx, dy) in Directions)
            {
                var next = new TilePosition(current.X + dx, current.Y + dy);
                if (stepsToGoal.ContainsKey(next) || start.ChebyshevDistance(next) > maxLength || !CanStep(map, current, dx, dy))
                {
                    continue;
                }

                stepsToGoal.Add(next, steps);
                frontier.Enqueue(next);
            }
        }

        return stepsToGoal;
    }

    private static bool CanStep(GridMap map, TilePosition from, int dx, int dy)
    {
        if (!map.IsWalkable(new TilePosition(from.X + dx, from.Y + dy)))
        {
            return false;
        }

        var isDiagonal = dx != 0 && dy != 0;
        return !isDiagonal
            || (map.IsWalkable(new TilePosition(from.X + dx, from.Y)) && map.IsWalkable(new TilePosition(from.X, from.Y + dy)));
    }

    private static long SquaredDistance(TilePosition a, TilePosition b)
    {
        long dx = a.X - b.X;
        long dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }
}
