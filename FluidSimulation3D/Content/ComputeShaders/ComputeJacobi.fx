﻿//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

RWStructuredBuffer<float> _Write;
StructuredBuffer<float> _Pressure, _Obstacles;
StructuredBuffer<float3> _Divergence;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Jacobi(int3 id : SV_DispatchThreadID)
{
    int idxL = max(0, id.x - 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    int idxR = min(_Size.x - 1, id.x + 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxB = id.x + max(0, id.y - 1) * _Size.x + id.z * _Size.x * _Size.y;
    int idxT = id.x + min(_Size.y - 1, id.y + 1) * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxD = id.x + id.y * _Size.x + max(0, id.z - 1) * _Size.x * _Size.y;
    int idxU = id.x + id.y * _Size.x + min(_Size.z - 1, id.z + 1) * _Size.x * _Size.y;
    
    float L = _Pressure[idxL];
    float R = _Pressure[idxR];
    
    float B = _Pressure[idxB];
    float T = _Pressure[idxT];
    
    float D = _Pressure[idxD];
    float U = _Pressure[idxU];
    
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    float C = _Pressure[idx];
    
    float divergence = _Divergence[idx].r;
    
    if (_Obstacles[idxL] > 0.1)
        L = C;
    if (_Obstacles[idxR] > 0.1)
        R = C;
    
    if (_Obstacles[idxB] > 0.1)
        B = C;
    if (_Obstacles[idxT] > 0.1)
        T = C;
    
    if (_Obstacles[idxD] > 0.1)
        D = C;
    if (_Obstacles[idxU] > 0.1)
        U = C;
    
    _Write[idx] = (L + R + B + T + U + D - divergence) / 6.0;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Jacobi();
    }
}