//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

StructuredBuffer<unorm float> strucBuff; //WAS A FLOAT4 FOR 2 HOURS DIE DIE DIE
RWTexture3D<unorm float4> Texture;
float4 _Size;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void CS(int3 id : SV_DispatchThreadID)
{ 
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    //float4 col = strucBuff[idx];
    //col.w *= 0.75f;
    Texture[id] = strucBuff[idx];
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 CS();
    }
}