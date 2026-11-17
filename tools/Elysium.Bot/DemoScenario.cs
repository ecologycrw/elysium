namespace Elysium.Bot;

/// <summary>
/// The slice 0 scenario: Ana and Bruno enter near each other and see each other; Caio enters far away and sees
/// nobody; Caio walks to Ana and they all see each other; Caio walks back and they lose sight of each other.
/// It expects an empty server, so the three bots get spawn points 0, 1 and 2 of the map.
/// </summary>
public static class DemoScenario
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(500);

    /// <summary>Runs the scenario and writes what happens. Returns true when every check passed.</summary>
    public static async Task<bool> RunAsync(string host, int port, TextWriter output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        await using var ana = await HeadlessBot.ConnectAsync(host, port, "Ana", output, cancellationToken).ConfigureAwait(false);
        await using var bruno = await HeadlessBot.ConnectAsync(host, port, "Bruno", output, cancellationToken).ConfigureAwait(false);
        await using var caio = await HeadlessBot.ConnectAsync(host, port, "Caio", output, cancellationToken).ConfigureAwait(false);

        bool Fail(string reason)
        {
            output.WriteLine($"FAILED: {reason}");
            return false;
        }

        async Task<bool> Wait(Func<bool> condition) =>
            await HeadlessBot.WaitUntilAsync(condition, StepTimeout, cancellationToken).ConfigureAwait(false);

        if (!await Wait(() => ana.Position is not null && bruno.Position is not null && caio.Position is not null).ConfigureAwait(false))
        {
            return Fail("the bots did not enter the map.");
        }

        if (!await Wait(() => ana.Sees("Bruno") && bruno.Sees("Ana")).ConfigureAwait(false))
        {
            return Fail("Ana and Bruno did not see each other. Is the server empty (no other bots connected)?");
        }

        await Task.Delay(SettleTime, cancellationToken).ConfigureAwait(false);
        if (caio.VisibleNames.Count != 0 || ana.Sees("Caio") || bruno.Sees("Caio"))
        {
            return Fail("Caio should start out of view of everyone.");
        }

        caio.Say("sees nobody");
        var caioStart = caio.Position!.Value;

        await caio.WalkToAsync(ana.Position!.Value, cancellationToken).ConfigureAwait(false);
        if (!await Wait(() => caio.Sees("Ana") && caio.Sees("Bruno") && ana.Sees("Caio") && bruno.Sees("Caio")).ConfigureAwait(false))
        {
            return Fail("Caio walked to Ana, but they did not all see each other.");
        }

        await caio.WalkToAsync(caioStart, cancellationToken).ConfigureAwait(false);
        if (!await Wait(() => caio.VisibleNames.Count == 0 && !ana.Sees("Caio") && !bruno.Sees("Caio")).ConfigureAwait(false))
        {
            return Fail("Caio walked away, but the others did not disappear.");
        }

        caio.Say("sees nobody again");
        output.WriteLine("PASSED: near bots see each other, a far bot sees nobody until it walks into view.");
        return true;
    }
}
