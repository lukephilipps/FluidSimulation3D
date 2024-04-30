//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;
float3 _Up;
float _DeltaTime, _Buoyancy, _Weight;

RWStructuredBuffer<float3> _Write;
StructuredBuffer<float3> _Velocity;
StructuredBuffer<float> _Density, _Temperature;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void BuoyantForce(int3 id : SV_DispatchThreadID)
{
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    float T = _Temperature[idx];
    float D = _Density[idx];
    float3 V = _Velocity[idx];
    
    V += (_DeltaTime * T * _Buoyancy - D * _Weight) * _Up;
    
    _Write[idx] = V;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 BuoyantForce();
    }
}