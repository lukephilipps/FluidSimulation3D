//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float3 _Size;

//RWStructuredBuffer<float> _Write;
RWTexture3D<float> _Write;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Borders(int3 id : SV_DispatchThreadID)
{
    float obstacle = 0;
    
    if (id.x - 1 < 0)
        obstacle = 1;
    if (id.x + 1 > (int) _Size.x - 1)
        obstacle = 1;
    
    if (id.y - 1 < 0)
        obstacle = 1;
    if (id.y + 1 > (int) _Size.y - 1)
        obstacle = 1;
    
    if (id.z - 1 < 0)
        obstacle = 1;
    if (id.z + 1 > (int) _Size.z - 1)
        obstacle = 1;
    
    _Write[id] = obstacle;
}


technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Borders();
    }
}