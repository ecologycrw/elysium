using System.Numerics;

namespace Elysium.Core.Client.Rendering;

/// <summary>
/// Light that comes from far away in one direction, like the sun, plus the light that reaches every face.
/// </summary>
/// <param name="Direction">Where the light travels to, in the world (for a sun high in the sky, mostly -Y). Need not be unit length.</param>
/// <param name="Color">The light on faces that look at it.</param>
/// <param name="Ambient">The light on every face, so faces in shadow are not black.</param>
public readonly record struct DirectionalLight(Vector3 Direction, ColorRgb Color, ColorRgb Ambient);
