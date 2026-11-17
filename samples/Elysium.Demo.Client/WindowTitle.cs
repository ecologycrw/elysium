using System.Globalization;

namespace Elysium.Demo.Client;

/// <summary>The window title: who is playing, how many frames per second, and whether the connection fell.</summary>
public static class WindowTitle
{
    public static string Format(string name, double framesPerSecond, string? lostReason) =>
        lostReason is null
            ? string.Create(CultureInfo.InvariantCulture, $"Elysium Demo - {name} - {framesPerSecond:0} FPS")
            : string.Create(CultureInfo.InvariantCulture, $"Elysium Demo - {name} - {framesPerSecond:0} FPS - CONNECTION LOST: {lostReason}");
}
