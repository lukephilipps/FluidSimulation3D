//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float3 _Size;

//RWStructuredBuffer<float3> _Write;
//StructuredBuffer<float3> _Velocity;

RWTexture3D<float4> _Write;
RWTexture3D<float4> _Velocity;

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Vorticity(int3 id : SV_DispatchThreadID)
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

    float3 L = _Velocity[idxL];
    float3 R = _Velocity[idxR];
    
    float3 B = _Velocity[idxB];
    float3 T = _Velocity[idxT];
    
    float3 D = _Velocity[idxD];
    float3 U = _Velocity[idxU];
    
    float4 vorticity = 0.5 * float4(((T.z - B.z) - (U.y - D.y)), ((U.x - D.x) - (R.z - L.z)), ((R.y - L.y) - (T.x - B.x)), 0);
			
    _Write[id] = vorticity;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Vorticity();
    }
}