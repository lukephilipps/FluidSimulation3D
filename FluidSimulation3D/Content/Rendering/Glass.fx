float4x4 _View;
float4x4 _Projection;

struct VertexIn
{
    float3 Position : POSITION0;
};
struct v2f
{
    float4 Position : SV_POSITION;
    float3 TexCoord : TEXCOORD0;
};

v2f vert(VertexIn input)
{
    v2f output;
    
    output.Position = mul(mul(float4(input.Position, 1), _View), _Projection);
    output.TexCoord = input.Position.xyz + 0.5;
	
    return output;
}

float4 frag(v2f IN) : COLOR
{
    float3 pixelPos = IN.TexCoord * 2 - 1;
	
    return pow(dot(pixelPos, pixelPos), 5) * 0.002;
}

technique Tech0
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 vert();
        PixelShader = compile ps_4_0 frag();
    }
}