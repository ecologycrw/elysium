// Draws a unit cube from the vertex index alone, without vertex buffers (slice 1, T02 prototype).
// Matrices arrive as System.Numerics writes them (row vectors); HLSL reads them column-major, which
// transposes them, so mul(matrix, vector) gives the same result as vector * matrix on the C# side.
// SDL3 GPU convention: vertex uniform buffers live in space1.

cbuffer Transforms : register(b0, space1)
{
    float4x4 ViewProjection;
    float4x4 Model;
};

struct VertexOutput
{
    float4 Position : SV_Position;
    float3 Normal : TEXCOORD0;
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

VertexOutput main(uint vertexId : SV_VertexID)
{
    VertexOutput output;
    float4 world = mul(Model, float4(Corners[CornerOfVertex[vertexId]], 1.0));
    output.Position = mul(ViewProjection, world);
    output.Normal = mul((float3x3)Model, FaceNormals[vertexId / 6]);
    return output;
}
