//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float _DeltaTime, _Dissipate, _Forward;

//StructuredBuffer<float3> _Velocity;
//StructuredBuffer<float> _Obstacles;

//RWStructuredBuffer<float> _Write1f;
//StructuredBuffer<float> _Read1f;

//RWStructuredBuffer<float3> _Write3f;
//StructuredBuffer<float3> _Read3f;

//StructuredBuffer<float> _Phi_n_1_hat, _Phi_n_hat;

RWTexture3D<float4> _Velocity;
RWTexture3D<float> _Obstacles;

RWTexture3D<float> _Write1f;
RWTexture3D<float> _Read1f;

RWTexture3D<float4> _Write3f;
RWTexture3D<float4> _Read3f;

//RWTexture3D<float> _Phi_n_1_hat, _Phi_n_hat;

float3 GetAdvectedPosTexCoords(float3 pos, int3 idx)
{
    pos -= _DeltaTime * _Forward * _Velocity[idx];

    return pos;
}

float SampleBilinear(StructuredBuffer<float> buffer, float3 uv, float3 size)
{
    int x = uv.x;
    int y = uv.y;
    int z = uv.z;
	
    int X = size.x;
    int XY = size.x * size.y;
	
    float fx = uv.x - x;
    float fy = uv.y - y;
    float fz = uv.z - z;
	
    int xp1 = min(size.x - 1, x + 1);
    int yp1 = min(size.y - 1, y + 1);
    int zp1 = min(size.z - 1, z + 1);
	
    float x0 = buffer[x + y * X + z * XY] * (1.0f - fx) + buffer[xp1 + y * X + z * XY] * fx;
    float x1 = buffer[x + y * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + y * X + zp1 * XY] * fx;
	
    float x2 = buffer[x + yp1 * X + z * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + z * XY] * fx;
    float x3 = buffer[x + yp1 * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + zp1 * XY] * fx;
	
    float z0 = x0 * (1.0f - fz) + x1 * fz;
    float z1 = x2 * (1.0f - fz) + x3 * fz;
	
    return z0 * (1.0f - fy) + z1 * fy;
}

float3 SampleBilinear(StructuredBuffer<float3> buffer, float3 uv, float3 size)
{
    int x = uv.x;
    int y = uv.y;
    int z = uv.z;
	
    int X = size.x;
    int XY = size.x * size.y;
	
    float fx = uv.x - x;
    float fy = uv.y - y;
    float fz = uv.z - z;
	
    int xp1 = min(size.x - 1, x + 1);
    int yp1 = min(size.y - 1, y + 1);
    int zp1 = min(size.z - 1, z + 1);
	
    float3 x0 = buffer[x + y * X + z * XY] * (1.0f - fx) + buffer[xp1 + y * X + z * XY] * fx;
    float3 x1 = buffer[x + y * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + y * X + zp1 * XY] * fx;
	
    float3 x2 = buffer[x + yp1 * X + z * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + z * XY] * fx;
    float3 x3 = buffer[x + yp1 * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + zp1 * XY] * fx;
	
    float3 z0 = x0 * (1.0f - fz) + x1 * fz;
    float3 z1 = x2 * (1.0f - fz) + x3 * fz;
	
    return z0 * (1.0f - fy) + z1 * fy;
}

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void AdvectVelocity(uint3 id : SV_DispatchThreadID)
{
    if (_Obstacles[id] > 0.1)
    {
        _Write3f[id] = float4(0, 0, 0, 0);
        return;
    }

    float3 uv = GetAdvectedPosTexCoords(id, id);
			
    _Write3f[id] = _Read3f[uv] * _Dissipate;
}

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Advect(uint3 id : SV_DispatchThreadID)
{
    if (_Obstacles[id] > 0.1)
    {
        _Write1f[id] = 0;
        return;
    }

    float3 uv = GetAdvectedPosTexCoords(id, id);
			
    _Write1f[id] = max(0, _Read1f[uv] * _Dissipate);
}

//[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
//void AdvectMacCormack(uint3 id : SV_DispatchThreadID)
//{
//    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
	
//    if (_Obstacles[idx] > 0.1)
//    {
//        _Write1f[idx] = 0;
//        return;
//    }
	
//    float3 uv = GetAdvectedPosTexCoords(id, idx);
	
//    float r;
//    float4 halfVolumeDim = _Size / 2;
//    float3 diff = abs(halfVolumeDim.xyz - id);

//    // Must use regular semi-Lagrangian advection instead of MacCormack at the volume boundaries
//    if ((diff.x > (halfVolumeDim.x - 4)) || (diff.y > (halfVolumeDim.y - 4)) || (diff.z > (halfVolumeDim.z - 4)))
//    {
//        r = SampleBilinear(_Read1f, uv, _Size.xyz);
//    }
//    else
//    {
//        int idx0 = (id.x - 1) + (id.y - 1) * _Size.x + (id.z - 1) * _Size.x * _Size.y;
//        int idx1 = (id.x - 1) + (id.y - 1) * _Size.x + (id.z + 1) * _Size.x * _Size.y;
	    
//        int idx2 = (id.x - 1) + (id.y + 1) * _Size.x + (id.z - 1) * _Size.x * _Size.y;
//        int idx3 = (id.x - 1) + (id.y + 1) * _Size.x + (id.z + 1) * _Size.x * _Size.y;
	    
//        int idx4 = (id.x + 1) + (id.y - 1) * _Size.x + (id.z - 1) * _Size.x * _Size.y;
//        int idx5 = (id.x + 1) + (id.y - 1) * _Size.x + (id.z + 1) * _Size.x * _Size.y;
	    
//        int idx6 = (id.x + 1) + (id.y + 1) * _Size.x + (id.z - 1) * _Size.x * _Size.y;
//        int idx7 = (id.x + 1) + (id.y + 1) * _Size.x + (id.z + 1) * _Size.x * _Size.y;
	           
//        float nodes[8];
//        nodes[0] = _Read1f[idx0];
//        nodes[1] = _Read1f[idx1];
	    
//        nodes[2] = _Read1f[idx2];
//        nodes[3] = _Read1f[idx3];
	    
//        nodes[4] = _Read1f[idx4];
//        nodes[5] = _Read1f[idx5];
	    
//        nodes[6] = _Read1f[idx6];
//        nodes[7] = _Read1f[idx7];
	    
//        float minPhi = min(min(min(min(min(min(min(nodes[0], nodes[1]), nodes[2]), nodes[3]), nodes[4]), nodes[5]), nodes[6]), nodes[7]);
	    
//        float maxPhi = max(max(max(max(max(max(max(nodes[0], nodes[1]), nodes[2]), nodes[3]), nodes[4]), nodes[5]), nodes[6]), nodes[7]);
   
//        r = SampleBilinear(_Phi_n_1_hat, uv, _Size.xyz) + 0.5f * (_Read1f[idx] - _Phi_n_hat[idx]);
        
//        r = max(min(r, maxPhi), minPhi);
//    }
    
//    _Write1f[idx] = max(0, r * _Dissipate);
//}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 AdvectVelocity();
    }
}
technique Tech1
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 Advect();
    }
}
//technique Tech2
//{
//    pass Pass0
//    {
//        ComputeShader = compile cs_5_0 AdvectMacCormack();
//    }
//}