struct VertexInput
{
    float4 Position : POSITION;
    float4 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD;
};

struct VertexOutput
{
    float4 Position : POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TextureCoordinate : TEXCOORD2;
};

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float3 CameraPosition;
texture EnvironmentMap;

samplerCUBE SkyBoxSampler = sampler_state
{
    texture = <EnvironmentMap>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = Mirror;
    AddressV = Mirror;
};

VertexOutput VS(VertexInput input)
{
    VertexOutput output;
    
    float4 worldPos = mul(input.Position, World);
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);
    output.WorldPosition = worldPos.xyz;
    output.Normal = normalize(mul(input.Normal, WorldInverseTranspose).xyz);
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;
}

float4 ReflectionPS(VertexOutput input) : COLOR
{
    float3 I = normalize(input.WorldPosition - CameraPosition);
    float3 R = reflect(I, input.Normal);

    float4 reflectedColor = texCUBE(SkyBoxSampler, R);
    reflectedColor.a = .2;
    
    return reflectedColor;
}

technique Reflection
{
    pass pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 ReflectionPS();
    }
}