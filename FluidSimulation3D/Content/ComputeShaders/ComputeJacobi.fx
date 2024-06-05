//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;

//RWStructuredBuffer<float> _Write;
//StructuredBuffer<float> _Pressure, _Obstacles;
//StructuredBuffer<float3> _Divergence;

RWTexture3D<float> _Write;
RWTexture3D<float> _Pressure, _Obstacles;
RWTexture3D<float4> _Divergence;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Jacobi(int3 id : SV_DispatchThreadID)
{
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
    
    float divergence = _Divergence[id].r;
    
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
    
    _Write[id] = (L + R + B + T + U + D - divergence) / 6.0;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Jacobi();
    }
}