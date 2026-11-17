namespace Elysium.Core.Client.Rendering;

/// <summary>A color, each channel from 0 (none) to 1 (full), written to the screen as is.</summary>
public readonly record struct ColorRgb(float R, float G, float B);
