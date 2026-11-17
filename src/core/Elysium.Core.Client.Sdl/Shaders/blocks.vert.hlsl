// Draws every block of the scene with one draw call (instancing): one unit cube, built from the vertex index
// alone, placed, sized and colored by the block of its instance.
// Matrices arrive as System.Numerics writes them (row vectors); HLSL reads them column-major, which
// transposes them, so mul(matrix, vector) gives the same result as vector * matrix on the C# side.
// SDL3 GPU conventions: vertex storage buffers live in space0, vertex uniform buffers in space1.

// One block, as SdlGpuRenderer.GpuBlock writes it: float4 fields, so Direct3D and Vulkan read the same bytes.
struct Block
{
    float4 Center;
    float4 Size;
    float4 Color;
};

StructuredBuffer<Block> Blocks : register(t0, space0);

cbuffer Camera : register(b0, space1)
{
    float4x4 ViewProjection;
};

struct VertexOutput
{
    float4 Position : SV_Position;
    float3 Normal : TEXCOORD0;
    float3 Color : TEXCOORD1;
};

static const float3 Corners[8] =
{
    float3(-0.5, -0.5, -0.5), float3(0.5, -0.5, -0.5), float3(0.5, 0.5, -0.5), float3(-0.5, 0.5, -0.5),
    float3(-0.5, -0.5, 0.5), float3(0.5, -0.5, 0.5), float3(0.5, 0.5, 0.5), float3(-0.5, 0.5, 0.5),
};

// Six faces, two triangles each: -Z, +Z, -X, +X, -Y, +Y.
static const uint CornerOfVertex[36] =
{
    0, 1, 2, 0, 2, 3,
    4, 6, 5, 4, 7, 6,
    0, 3, 7, 0, 7, 4,
    1, 5, 6, 1, 6, 2,
    0, 4, 5, 0, 5, 1,
    3, 2, 6, 3, 6, 7,
};

static const float3 FaceNormals[6] =
{
    float3(0, 0, -1), float3(0, 0, 1), float3(-1, 0, 0), float3(1, 0, 0), float3(0, -1, 0), float3(0, 1, 0),
};

VertexOutput main(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    Block block = Blocks[instanceId];
    float3 world = block.Center.xyz + Corners[CornerOfVertex[vertexId]] * block.Size.xyz;

    VertexOutput output;
    output.Position = mul(ViewProjection, float4(world, 1.0));
    // Blocks are only moved and stretched along the axes, never turned, so a face keeps its normal.
    output.Normal = FaceNormals[vertexId / 6];
    output.Color = block.Color.rgb;
    return output;
}
