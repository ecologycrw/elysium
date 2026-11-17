using System.Numerics;
using System.Runtime.InteropServices;
using Elysium.Core.Client.Rendering;
using SDL;

namespace Elysium.Core.Client.Sdl;

/// <summary>
/// The blocks of the scene on the GPU: a storage buffer the vertex shader reads one block per instance from,
/// filled each frame through an upload buffer. Both grow when a scene has more blocks than ever before, and are
/// reused otherwise, so a frame of the same size does not allocate. Not thread-safe: the renderer owns it.
/// </summary>
internal sealed unsafe class GpuBlockBuffer(SDL_GPUDevice* device) : IDisposable
{
    private SDL_GPUBuffer* _blocks;
    private SDL_GPUTransferBuffer* _upload;
    private int _capacity;

    /// <summary>The storage buffer with the blocks of the last upload, or null before the first one.</summary>
    public SDL_GPUBuffer* Buffer => _blocks;

    /// <summary>Copies <paramref name="blocks"/> to the GPU, in a copy pass recorded on <paramref name="commands"/>.</summary>
    public void Upload(SDL_GPUCommandBuffer* commands, ReadOnlySpan<SceneBlock> blocks)
    {
        if (blocks.IsEmpty)
        {
            return;
        }

        EnsureCapacity(blocks.Length);
        var size = (uint)(blocks.Length * sizeof(GpuBlock));
        var mapped = (GpuBlock*)SDL3.SDL_MapGPUTransferBuffer(device, _upload, cycle: true);
        if (mapped is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_MapGPUTransferBuffer));
        }

        for (var i = 0; i < blocks.Length; i++)
        {
            mapped[i] = new GpuBlock(blocks[i]);
        }

        SDL3.SDL_UnmapGPUTransferBuffer(device, _upload);
        var source = new SDL_GPUTransferBufferLocation { transfer_buffer = _upload, offset = 0 };
        var destination = new SDL_GPUBufferRegion { buffer = _blocks, offset = 0, size = size };
        var copyPass = SDL3.SDL_BeginGPUCopyPass(commands);
        SDL3.SDL_UploadToGPUBuffer(copyPass, &source, &destination, cycle: true);
        SDL3.SDL_EndGPUCopyPass(copyPass);
    }

    public void Dispose()
    {
        Release();
        _capacity = 0;
    }

    private void EnsureCapacity(int count)
    {
        if (count <= _capacity)
        {
            return;
        }

        Release();
        var capacity = Math.Max(count, _capacity * 2);
        var size = (uint)(capacity * sizeof(GpuBlock));
        var bufferInfo = new SDL_GPUBufferCreateInfo
        {
            usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ,
            size = size,
        };
        _blocks = SDL3.SDL_CreateGPUBuffer(device, &bufferInfo);
        if (_blocks is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_CreateGPUBuffer));
        }

        var uploadInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = size,
        };
        _upload = SDL3.SDL_CreateGPUTransferBuffer(device, &uploadInfo);
        if (_upload is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_CreateGPUTransferBuffer));
        }

        _capacity = capacity;
    }

    private void Release()
    {
        if (_blocks is not null)
        {
            SDL3.SDL_ReleaseGPUBuffer(device, _blocks);
            _blocks = null;
        }

        if (_upload is not null)
        {
            SDL3.SDL_ReleaseGPUTransferBuffer(device, _upload);
            _upload = null;
        }
    }

    /// <summary>
    /// One block as the vertex shader reads it (struct Block in blocks.vert.hlsl), byte for byte. Only float4
    /// fields, so Direct3D and Vulkan lay it out the same way.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuBlock(SceneBlock block)
    {
        public readonly Vector4 Center = new(block.Center, 0);
        public readonly Vector4 Size = new(block.Size, 0);
        public readonly Vector4 Color = new(block.Color.R, block.Color.G, block.Color.B, 1);
    }
}
