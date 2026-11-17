using System.Numerics;
using System.Runtime.InteropServices;
using Elysium.Core.Client.Rendering;
using SDL;

namespace Elysium.Core.Client.Sdl;

/// <summary>
/// Draws a <see cref="Scene"/> with SDL3 GPU, which picks Direct3D 12 or Vulkan on Windows (and Metal on Apple
/// systems): every block in one draw call (instancing), lit by the scene's directional light. A frame of a scene
/// no bigger than the ones before does not allocate. Not thread-safe: use it from the thread that owns the window.
/// </summary>
public sealed unsafe class SdlGpuRenderer : IDisposable
{
    private const SDL_GPUTextureFormat DepthFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM;
    private const uint CubeVertexCount = 36;

    private readonly SdlWindow _window;
    private SDL_GPUDevice* _device;
    private SDL_GPUGraphicsPipeline* _pipeline;
    private GpuBlockBuffer? _blocks;
    private SDL_GPUTexture* _depth;
    private uint _depthWidth;
    private uint _depthHeight;

    private SdlGpuRenderer(SdlWindow window, SDL_GPUDevice* device)
    {
        _window = window;
        _device = device;
    }

    /// <summary>The graphics API SDL chose, for example "direct3d12" or "vulkan".</summary>
    public string DriverName => SDL3.SDL_GetGPUDeviceDriver(_device) ?? "unknown";

    /// <summary>Creates the GPU device for the window, with vertical sync.</summary>
    public static SdlGpuRenderer Create(SdlWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var device = SDL3.SDL_CreateGPUDevice(
            SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
            debug_mode: false,
            name: (byte*)null);
        if (device is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_CreateGPUDevice));
        }

        var renderer = new SdlGpuRenderer(window, device);
        try
        {
            if (!SDL3.SDL_ClaimWindowForGPUDevice(device, window.Handle))
            {
                throw SdlException.FromLastError(nameof(SDL3.SDL_ClaimWindowForGPUDevice));
            }

            renderer._pipeline = renderer.CreateBlockPipeline();
            renderer._blocks = new GpuBlockBuffer(device);
            return renderer;
        }
        catch
        {
            renderer.Dispose();
            throw;
        }
    }

    /// <summary>Draws one frame of <paramref name="scene"/>. Skips the frame while the window is minimized.</summary>
    public void Draw(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ObjectDisposedException.ThrowIf(_device is null || _blocks is null, this);
        var commands = SDL3.SDL_AcquireGPUCommandBuffer(_device);
        if (commands is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_AcquireGPUCommandBuffer));
        }

        SDL_GPUTexture* swapchain;
        uint width;
        uint height;
        if (!SDL3.SDL_WaitAndAcquireGPUSwapchainTexture(commands, _window.Handle, &swapchain, &width, &height))
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_WaitAndAcquireGPUSwapchainTexture));
        }

        if (swapchain is not null)
        {
            EnsureDepthTexture(width, height);
            _blocks.Upload(commands, scene.Blocks);
            PushUniforms(commands, scene);
            var pass = BeginPass(commands, swapchain, scene.Background);
            if (!scene.Blocks.IsEmpty)
            {
                SDL3.SDL_BindGPUGraphicsPipeline(pass, _pipeline);
                var buffer = _blocks.Buffer;
                SDL3.SDL_BindGPUVertexStorageBuffers(pass, 0, &buffer, 1);
                SDL3.SDL_DrawGPUPrimitives(pass, CubeVertexCount, (uint)scene.Blocks.Length, 0, 0);
            }

            SDL3.SDL_EndGPURenderPass(pass);
        }

        if (!SDL3.SDL_SubmitGPUCommandBuffer(commands))
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_SubmitGPUCommandBuffer));
        }
    }

    public void Dispose()
    {
        if (_device is null)
        {
            return;
        }

        _blocks?.Dispose();
        _blocks = null;
        if (_depth is not null)
        {
            SDL3.SDL_ReleaseGPUTexture(_device, _depth);
        }

        if (_pipeline is not null)
        {
            SDL3.SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
        }

        SDL3.SDL_ReleaseWindowFromGPUDevice(_device, _window.Handle);
        SDL3.SDL_DestroyGPUDevice(_device);
        _device = null;
    }

    private static void PushUniforms(SDL_GPUCommandBuffer* commands, Scene scene)
    {
        var camera = scene.ViewProjection;
        SDL3.SDL_PushGPUVertexUniformData(commands, 0, (IntPtr)(&camera), (uint)sizeof(Matrix4x4));
        var light = new LightUniforms(scene.Light);
        SDL3.SDL_PushGPUFragmentUniformData(commands, 0, (IntPtr)(&light), (uint)sizeof(LightUniforms));
    }

    private SDL_GPURenderPass* BeginPass(SDL_GPUCommandBuffer* commands, SDL_GPUTexture* swapchain, ColorRgb background)
    {
        var color = new SDL_GPUColorTargetInfo
        {
            texture = swapchain,
            clear_color = new SDL_FColor { r = background.R, g = background.G, b = background.B, a = 1f },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };
        var depth = new SDL_GPUDepthStencilTargetInfo
        {
            texture = _depth,
            clear_depth = 1f,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
            stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
            stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
        };
        return SDL3.SDL_BeginGPURenderPass(commands, &color, 1, &depth);
    }

    private void EnsureDepthTexture(uint width, uint height)
    {
        if (_depth is not null && width == _depthWidth && height == _depthHeight)
        {
            return;
        }

        if (_depth is not null)
        {
            SDL3.SDL_ReleaseGPUTexture(_device, _depth);
        }

        var info = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = DepthFormat,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
            width = width,
            height = height,
            layer_count_or_depth = 1,
            num_levels = 1,
        };
        _depth = SDL3.SDL_CreateGPUTexture(_device, &info);
        if (_depth is null)
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_CreateGPUTexture));
        }

        _depthWidth = width;
        _depthHeight = height;
    }

    private SDL_GPUGraphicsPipeline* CreateBlockPipeline()
    {
        var vertexShader = LoadShader("blocks.vert", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, uniformBuffers: 1, storageBuffers: 1);
        var fragmentShader = LoadShader("blocks.frag", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, uniformBuffers: 1, storageBuffers: 0);
        try
        {
            var colorTarget = new SDL_GPUColorTargetDescription
            {
                format = SDL3.SDL_GetGPUSwapchainTextureFormat(_device, _window.Handle),
            };
            var info = new SDL_GPUGraphicsPipelineCreateInfo
            {
                vertex_shader = vertexShader,
                fragment_shader = fragmentShader,
                primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                rasterizer_state = new SDL_GPURasterizerState
                {
                    fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                    cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                },
                depth_stencil_state = new SDL_GPUDepthStencilState
                {
                    compare_op = SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS,
                    enable_depth_test = true,
                    enable_depth_write = true,
                },
                target_info = new SDL_GPUGraphicsPipelineTargetInfo
                {
                    color_target_descriptions = &colorTarget,
                    num_color_targets = 1,
                    depth_stencil_format = DepthFormat,
                    has_depth_stencil_target = true,
                },
            };
            var pipeline = SDL3.SDL_CreateGPUGraphicsPipeline(_device, &info);
            return pipeline is not null ? pipeline : throw SdlException.FromLastError(nameof(SDL3.SDL_CreateGPUGraphicsPipeline));
        }
        finally
        {
            SDL3.SDL_ReleaseGPUShader(_device, vertexShader);
            SDL3.SDL_ReleaseGPUShader(_device, fragmentShader);
        }
    }

    /// <summary>Loads a precompiled shader in the format the chosen graphics API accepts (DXIL or SPIR-V).</summary>
    private SDL_GPUShader* LoadShader(string name, SDL_GPUShaderStage stage, uint uniformBuffers, uint storageBuffers)
    {
        var useDxil = (SDL3.SDL_GetGPUShaderFormats(_device) & SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL) != 0;
        var format = useDxil ? SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL : SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;
        var resource = $"Shaders.{name}.{(useDxil ? "dxil" : "spv")}";
        var code = ReadResource(resource);

        fixed (byte* codePointer = code)
        fixed (byte* entryPoint = "main\0"u8)
        {
            var info = new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)code.Length,
                code = codePointer,
                entrypoint = entryPoint,
                format = format,
                stage = stage,
                num_uniform_buffers = uniformBuffers,
                num_storage_buffers = storageBuffers,
            };
            var shader = SDL3.SDL_CreateGPUShader(_device, &info);
            return shader is not null ? shader : throw SdlException.FromLastError($"{nameof(SDL3.SDL_CreateGPUShader)} ({resource})");
        }
    }

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(SdlGpuRenderer).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The shader {name} is missing from the program.");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>The fragment uniform buffer of blocks.frag.hlsl, byte for byte.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct LightUniforms(DirectionalLight light)
    {
        public readonly Vector4 TowardsLight = new(TowardsLightOf(light.Direction), 0);
        public readonly Vector4 LightColor = new(light.Color.R, light.Color.G, light.Color.B, 1);
        public readonly Vector4 Ambient = new(light.Ambient.R, light.Ambient.G, light.Ambient.B, 1);

        /// <summary>The opposite of where the light travels, at unit length; no direct light for a zero direction.</summary>
        private static Vector3 TowardsLightOf(Vector3 direction) =>
            direction == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(-direction);
    }
}
