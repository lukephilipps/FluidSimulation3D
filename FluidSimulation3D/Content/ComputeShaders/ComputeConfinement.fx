﻿//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float _DeltaTime, _Epsilon;
float4 _Size;

RWStructuredBuffer<float3> _Write;
StructuredBuffer<float3> _Vorticity, _Read;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Confine(int3 id : SV_DispatchThreadID)
{
    int idxL = max(0, id.x - 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    int idxR = min(_Size.x - 1, id.x + 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxB = id.x + max(0, id.y - 1) * _Size.x + id.z * _Size.x * _Size.y;
    int idxT = id.x + min(_Size.y - 1, id.y + 1) * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxD = id.x + id.y * _Size.x + max(0, id.z - 1) * _Size.x * _Size.y;
    int idxU = id.x + id.y * _Size.x + min(_Size.z - 1, id.z + 1) * _Size.x * _Size.y;
    
    float omegaL = length(_Vorticity[idxL]);
    float omegaR = length(_Vorticity[idxR]);
    
    float omegaB = length(_Vorticity[idxB]);
    float omegaT = length(_Vorticity[idxT]);
    
    float omegaD = length(_Vorticity[idxD]);
    float omegaU = length(_Vorticity[idxU]);
    
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    float3 omega = _Vorticity[idx];
    
    float3 eta = 0.5 * float3(omegaR - omegaL, omegaT - omegaB, omegaU - omegaD);

    eta = normalize(eta + float3(0.001, 0.001, 0.001));
    
    float3 force = _DeltaTime * _Epsilon * float3(eta.y * omega.z - eta.z * omega.y, eta.z * omega.x - eta.x * omega.z, eta.x * omega.y - eta.y * omega.x);
	
    _Write[idx] = _Read[idx] + force;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Confine();
    }
}