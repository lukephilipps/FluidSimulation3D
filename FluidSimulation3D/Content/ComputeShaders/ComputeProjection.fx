//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float3 _Size;

//RWStructuredBuffer<float3> _Write;
//StructuredBuffer<float> _Pressure, _Obstacles;
//StructuredBuffer<float3> _Velocity;

RWTexture3D<float4> _Write;
RWTexture3D<float> _Pressure, _Obstacles;
RWTexture3D<float4> _Velocity;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Project(int3 id : SV_DispatchThreadID)
{
    if (_Obstacles[id] > 0.1)
    {
        _Write[id] = float4(0, 0, 0, 0);
        return;
    }

    int3 idxL, idxR, idxB, idxT, idxD, idxU;
    idxL = idxR = idxB = idxT = idxD = idxU = id;
    
    idxL.x = max(0, id.x - 1);
    idxR.x = min(id.x + 1, _Size.x - 1);
    idxB.y = max(0, id.y - 1);
    idxT.y = min(id.y + 1, _Size.y - 1);
    idxD.z = max(0, id.z - 1);
    idxU.z = min(id.z + 1, _Size.z - 1);
    
    float L = _Pressure[idxL];
    float R = _Pressure[idxR];
    
    float B = _Pressure[idxB];
    float T = _Pressure[idxT];
    
    float D = _Pressure[idxD];
    float U = _Pressure[idxU];
    
    float C = _Pressure[id];
    
    float4 mask = float4(1, 1, 1, 0);
    
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
    
    float4 v = _Velocity[id] - float4(R - L, T - B, U - D, 0) * 0.5;
    
    _Write[id] = v * mask;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Project();
    }
}