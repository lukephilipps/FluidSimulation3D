﻿//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

RWStructuredBuffer<float3> _Write;
StructuredBuffer<float> _Pressure, _Obstacles;
StructuredBuffer<float3> _Velocity;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Project(int3 id : SV_DispatchThreadID)
{
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;

    if (_Obstacles[idx] > 0.1)
    {
        _Write[idx] = float3(0, 0, 0);
        return;
    }

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
    
    float C = _Pressure[idx];
    
    float3 mask = float3(1, 1, 1);
    
    if (_Obstacles[idxL] > 0.1)
    {
        L = C;
        mask.x = 0;
    }
    if (_Obstacles[idxR] > 0.1)
    {
        R = C;
        mask.x = 0;
    }
    
    if (_Obstacles[idxB] > 0.1)
    {
        B = C;
        mask.y = 0;
    }
    if (_Obstacles[idxT] > 0.1)
    {
        T = C;
        mask.y = 0;
    }
    
    if (_Obstacles[idxD] > 0.1)
    {
        D = C;
        mask.z = 0;
    }
    if (_Obstacles[idxU] > 0.1)
    {
        U = C;
        mask.z = 0;
    }
    
    float3 v = _Velocity[idx] - float3(R - L, T - B, U - D) * 0.5;
    
    _Write[idx] = v * mask;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Project();
    }
}