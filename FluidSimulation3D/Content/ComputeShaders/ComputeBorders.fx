//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

RWStructuredBuffer<float> _Write;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void CS(int3 id : SV_DispatchThreadID)
{

    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
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
    
    _Write[idx] = obstacle;
}


technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 CS();
    }
}