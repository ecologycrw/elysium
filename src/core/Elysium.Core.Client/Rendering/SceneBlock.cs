using System.Numerics;

namespace Elysium.Core.Client.Rendering;

/// <summary>A box aligned with the world axes, in one color: the only thing a <see cref="Scene"/> draws for now.</summary>
/// <param name="Center">The middle of the box, in the world.</param>
/// <param name="Size">The width (X), height (Y) and depth (Z) of the box.</param>
/// <param name="Color">The color of every face, before light.</param>
public readonly record struct SceneBlock(Vector3 Center, Vector3 Size, ColorRgb Color);
