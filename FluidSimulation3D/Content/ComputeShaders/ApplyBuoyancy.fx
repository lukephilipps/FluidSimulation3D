//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Up;
float _DeltaTime, _Buoyancy, _Weight;

//RWStructuredBuffer<float3> _Write;
//StructuredBuffer<float3> _Velocity;
//StructuredBuffer<float> _Density, _Temperature;

RWTexture3D<float4> _Write;
RWTexture3D<float4> _Velocity;
RWTexture3D<float> _Density, _Temperature;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void BuoyantForce(int3 id : SV_DispatchThreadID)
{
    float T = _Temperature[id];
    float D = _Density[id];
    float4 V = _Velocity[id];
    
    V += (_DeltaTime * T * _Buoyancy - D * _Weight) * _Up;
    
    _Write[id] = V;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 BuoyantForce();
    }
}