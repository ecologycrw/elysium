using System.Numerics;
using System.Runtime.InteropServices;

namespace Elysium.Core.Client.Rendering;

/// <summary>
/// What to draw in one frame, as plain data: the camera, the light, the background and the blocks. The game
/// fills it every frame and the platform layer draws it, without either knowing about the other.
/// One scene is reused frame after frame: once its list of blocks has grown to the size of the world shown,
/// filling it again does not allocate. Not thread-safe: the frame loop owns it.
/// </summary>
public sealed class Scene
{
    private readonly List<SceneBlock> _blocks = [];

    /// <summary>World to clip space, as System.Numerics writes it (row vectors).</summary>
    public Matrix4x4 ViewProjection { get; set; } = Matrix4x4.Identity;

    public DirectionalLight Light { get; set; }

    /// <summary>The color where nothing is drawn.</summary>
    public ColorRgb Background { get; set; }

    /// <summary>The blocks added since the last <see cref="Clear"/>, in the order they were added.</summary>
    public ReadOnlySpan<SceneBlock> Blocks => CollectionsMarshal.AsSpan(_blocks);

    /// <summary>Removes every block, keeping the room they used for the next frame.</summary>
    public void Clear() => _blocks.Clear();

    public void Add(SceneBlock block) => _blocks.Add(block);
}
