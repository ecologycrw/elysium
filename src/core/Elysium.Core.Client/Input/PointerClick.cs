using System.Numerics;

namespace Elysium.Core.Client.Input;

/// <summary>A click (or touch) on the window.</summary>
/// <param name="Position">Where, in pixels from the top-left corner of the window, as the scene is drawn.</param>
/// <param name="Button">Which button.</param>
public readonly record struct PointerClick(Vector2 Position, PointerButton Button);
