using System.Numerics;
using System.Runtime.InteropServices;

namespace Elysium.Core.Client.Input;

/// <summary>
/// What the player did and how the window is, for one frame: the platform layer fills it, the game reads it,
/// and neither knows about the other. One input is reused frame after frame without allocating, once its
/// list of clicks has grown. Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class FrameInput
{
    private readonly List<PointerClick> _clicks = [];

    /// <summary>The size of the drawing area, in pixels; at least 1 by 1.</summary>
    public int ViewportWidth { get; private set; } = 1;

    /// <summary>The size of the drawing area, in pixels; at least 1 by 1.</summary>
    public int ViewportHeight { get; private set; } = 1;

    /// <summary>True once the player asked to close the window. It stays true.</summary>
    public bool CloseRequested { get; private set; }

    /// <summary>The clicks since the last frame, oldest first.</summary>
    public ReadOnlySpan<PointerClick> Clicks => CollectionsMarshal.AsSpan(_clicks);

    /// <summary>Starts a new frame: forgets the clicks of the last one.</summary>
    public void BeginFrame() => _clicks.Clear();

    /// <summary>The drawing area now has this size, in pixels. A minimized window counts as 1 by 1.</summary>
    public void SetViewport(int width, int height)
    {
        ViewportWidth = Math.Max(width, 1);
        ViewportHeight = Math.Max(height, 1);
    }

    public void AddClick(Vector2 position, PointerButton button) => _clicks.Add(new PointerClick(position, button));

    public void RequestClose() => CloseRequested = true;
}
