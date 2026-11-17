// One fixed color lit by one directional light (slice 1, T02 prototype).

struct FragmentInput
{
    float4 Position : SV_Position;
    float3 Normal : TEXCOORD0;
};

float4 main(FragmentInput input) : SV_Target0
{
    const float3 towardsLight = normalize(float3(0.4, 1.0, 0.6));
    const float3 color = float3(0.85, 0.55, 0.25);
    float diffuse = saturate(dot(normalize(input.Normal), towardsLight));
    return float4(color * (0.3 + 0.7 * diffuse), 1.0);
}
