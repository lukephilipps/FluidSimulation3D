//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float _DeltaTime, _Epsilon;
float3 _Size;

//RWStructuredBuffer<float3> _Write;
//StructuredBuffer<float3> _Vorticity, _Read;

RWTexture3D<float4> _Write;
RWTexture3D<float4> _Vorticity, _Read;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Confine(int3 id : SV_DispatchThreadID)
{
    int3 idxL, idxR, idxB, idxT, idxD, idxU;
    idxL = idxR = idxB = idxT = idxD = idxU = id;
    
    idxL.x = max(0, id.x - 1);
    idxR.x = min(id.x + 1, _Size.x - 1);
    idxB.y = max(0, id.y - 1);
    idxT.y = min(id.y + 1, _Size.y - 1);
    idxD.z = max(0, id.z - 1);
    idxU.z = min(id.z + 1, _Size.z - 1);
    
    //int idxL = max(0, id.x - 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    //int idxR = min(_Size.x - 1, id.x + 1) + id.y * _Size.x + id.z * _Size.x * _Size.y;
    //int idxB = id.x + max(0, id.y - 1) * _Size.x + id.z * _Size.x * _Size.y;
    //int idxT = id.x + min(_Size.y - 1, id.y + 1) * _Size.x + id.z * _Size.x * _Size.y;
    //int idxD = id.x + id.y * _Size.x + max(0, id.z - 1) * _Size.x * _Size.y;
    //int idxU = id.x + id.y * _Size.x + min(_Size.z - 1, id.z + 1) * _Size.x * _Size.y;
    
    float omegaL = length(_Vorticity[idxL]);
    float omegaR = length(_Vorticity[idxR]);
    
    float omegaB = length(_Vorticity[idxB]);
    float omegaT = length(_Vorticity[idxT]);
    
    float omegaD = length(_Vorticity[idxD]);
    float omegaU = length(_Vorticity[idxU]);
    
    float4 omega = _Vorticity[id];
    
    float3 eta = 0.5 * float3(omegaR - omegaL, omegaT - omegaB, omegaU - omegaD);

    eta = normalize(eta + float3(0.001, 0.001, 0.001));
    
    float4 force = _DeltaTime * _Epsilon * float4(eta.y * omega.z - eta.z * omega.y, eta.z * omega.x - eta.x * omega.z, eta.x * omega.y - eta.y * omega.x, 0);
	
    _Write[id] = _Read[id] + force;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Confine();
    }
}