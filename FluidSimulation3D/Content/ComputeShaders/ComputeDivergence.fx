﻿//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

RWStructuredBuffer<float3> _Write;
StructuredBuffer<float3> _Velocity;
StructuredBuffer<float> _Obstacles;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Divergence(int3 id : SV_DispatchThreadID)
{
    int idxL = max(0, id.x - 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    int idxR = min(_Size.x - 1, id.x + 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxB = id.x + max(0, id.y - 1) * _Size.x + id.z * _Size.x * _Size.y;
    int idxT = id.x + min(_Size.y - 1, id.y + 1) * _Size.x + id.z * _Size.x * _Size.y;
    
    int idxD = id.x + id.y * _Size.x + max(0, id.z - 1) * _Size.x * _Size.y;
    int idxU = id.x + id.y * _Size.x + min(_Size.z - 1, id.z + 1) * _Size.x * _Size.y;
    
    float3 L = _Velocity[idxL];
    float3 R = _Velocity[idxR];
    
    float3 B = _Velocity[idxB];
    float3 T = _Velocity[idxT];
    
    float3 D = _Velocity[idxD];
    float3 U = _Velocity[idxU];
    
    float3 obstacleVelocity = float3(0, 0, 0);
    
    // Possibly remove checks and directly set these values to borders as they are 0 or 1
    if (_Obstacles[idxL] > 0.1)
        L = obstacleVelocity;
    if (_Obstacles[idxR] > 0.1)
        R = obstacleVelocity;
    
    if (_Obstacles[idxB] > 0.1)
        B = obstacleVelocity;
    if (_Obstacles[idxT] > 0.1)
        T = obstacleVelocity;
    
    if (_Obstacles[idxD] > 0.1)
        D = obstacleVelocity;
    if (_Obstacles[idxU] > 0.1)
        U = obstacleVelocity;
    
    float divergence = 0.5 * ((R.x - L.x) + (T.y - B.y) + (U.z - D.z));
    
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
    
    _Write[idx] = float3(divergence, 0, 0);
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Divergence();
    }
}