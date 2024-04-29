struct VertexPositionTexture
{
    float4 Position : POSITION;
    float2 TextureCoordinate : TEXCOORD;
};

float4x4 _World;
float4x4 _View;
float4x4 _Projection;

texture _Texture;
sampler _TexSampler = sampler_state
{
    Texture = <_Texture>;
};

VertexPositionTexture VS(VertexPositionTexture input)
{
    VertexPositionTexture output;
    
    output.Position = mul(mul(mul(input.Position, _World), _View), _Projection);
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;
}

float4 PS(VertexPositionTexture input) : COLOR
{
    return tex2D(_TexSampler, input.TextureCoordinate);
}

technique MyTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}