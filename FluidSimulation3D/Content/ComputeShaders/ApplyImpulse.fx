//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float _Radius, _Amount, _DeltaTime;
float3 _Pos;
float4 _Size;

RWStructuredBuffer<float> _Write;
StructuredBuffer<float> _Read, _Reaction;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void GaussImpulse(uint3 id : SV_DispatchThreadID)
{
    float3 pos = id / (_Size.xyz - 1.0f) - _Pos;
    float mag = pos.x * pos.x + pos.y * pos.y + pos.z * pos.z;
    float rad2 = _Radius * _Radius;
	
    float amount = exp(-mag / rad2) * _Amount * _DeltaTime;
				
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
	
    _Write[idx] = _Read[idx] + amount;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 GaussImpulse();
    }
}