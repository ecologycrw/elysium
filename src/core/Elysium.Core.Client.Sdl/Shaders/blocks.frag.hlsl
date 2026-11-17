// The block color lit by one directional light, plus the light that reaches every face.
// SDL3 GPU convention: fragment uniform buffers live in space3.

cbuffer Light : register(b0, space3)
{
    float4 TowardsLight; // unit length, from the surface towards the light
    float4 LightColor;
    float4 Ambient;
};

struct FragmentInput
{
    float4 Position : SV_Position;
    float3 Normal : TEXCOORD0;
    float3 Color : TEXCOORD1;
};

float4 main(FragmentInput input) : SV_Target0
{
    float diffuse = saturate(dot(normalize(input.Normal), TowardsLight.xyz));
    float3 light = Ambient.rgb + LightColor.rgb * diffuse;
    return float4(saturate(input.Color * light), 1.0);
}
