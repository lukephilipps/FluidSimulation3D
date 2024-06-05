//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

//RWStructuredBuffer<float3> _Write;
//StructuredBuffer<float3> _Velocity;
//StructuredBuffer<float> _Obstacles;

RWTexture3D<float4> _Write;
RWTexture3D<float4> _Velocity;
RWTexture3D<float> _Obstacles;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Divergence(int3 id : SV_DispatchThreadID)
{
    int3 idxL, idxR, idxB, idxT, idxD, idxU;
    idxL = idxR = idxB = idxT = idxD = idxU = id;
    
    idxL.x = max(0, id.x - 1);
    idxR.x = min(id.x + 1, _Size.x - 1);
    idxB.y = max(0, id.y - 1);
    idxT.y = min(id.y + 1, _Size.y - 1);
    idxD.z = max(0, id.z - 1);
    idxU.z = min(id.z + 1, _Size.z - 1);
    
    float4 L = _Velocity[idxL];
    float4 R = _Velocity[idxR];
    
    float4 B = _Velocity[idxB];
    float4 T = _Velocity[idxT];
    
    float4 D = _Velocity[idxD];
    float4 U = _Velocity[idxU];
    
    float4 obstacleVelocity = float4(0, 0, 0, 0);
    
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
    
    _Write[id] = float4(divergence, 0, 0, 0);
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Divergence();
    }
}