struct VertexPositionTexture
{
    float4 Position : POSITION;
    float2 TextureCoordinate : TEXCOORD;
};

float4x4 World;
float4x4 View;
float4x4 Projection;

texture MyTexture;
sampler mySampler = sampler_state
{
    Texture = <MyTexture>;
};

VertexPositionTexture VS(VertexPositionTexture input)
{
    VertexPositionTexture output;
    
    output.Position = mul(mul(mul(input.Position, World), View), Projection);
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;

}

float4 PS(VertexPositionTexture input) : COLOR
{
    return tex2D(mySampler, input.TextureCoordinate);
}

technique MyTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}