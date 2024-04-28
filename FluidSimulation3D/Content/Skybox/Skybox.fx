struct VertexInput
{
    float4 Position : POSITION;
};

struct VertexOutput
{
    float4 Position : POSITION;
    float3 TextureCoordinate : TEXCOORD;
};

float4x4 World;
float4x4 View;
float4x4 Projection;
float3 CameraPosition;
texture SkyBoxTexture;

samplerCUBE SkyBoxSampler = sampler_state
{
    texture = <SkyBoxTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = Mirror;
    AddressV = Mirror;
};

VertexOutput VS(VertexInput input)
{
    VertexOutput output;
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    
    float4 VertexPosition = mul(input.Position, World);
    output.TextureCoordinate = VertexPosition.xyz - CameraPosition;
    return output;

}

float4 PS(VertexOutput input) : COLOR
{
    return texCUBE(SkyBoxSampler, normalize(input.TextureCoordinate));
}

technique Skybox
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}