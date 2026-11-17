namespace Elysium.Core.Client.Input;

/// <summary>Which button made a click. A touch on a phone screen is a <see cref="Primary"/> click.</summary>
public enum PointerButton
{
    /// <summary>The main button: the left mouse button, or a touch.</summary>
    Primary,

    /// <summary>The other button: the right mouse button.</summary>
    Secondary,
}
