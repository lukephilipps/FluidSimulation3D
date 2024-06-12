//==============================================================================
// Compute Shader
//==============================================================================
#define GroupSizeXYZ 8

float4 _Size;
float _DeltaTime, _Dissipate, _Forward;

StructuredBuffer<float3> _Velocity;
StructuredBuffer<float> _Obstacles;

RWStructuredBuffer<float> _Write1f;
StructuredBuffer<float> _Read1f;

RWStructuredBuffer<float3> _Write3f;
StructuredBuffer<float3> _Read3f;

StructuredBuffer<float> _Phi_n_1_hat, _Phi_n_hat;

float3 GetAdvectedPosTexCoords(float3 pos, int idx)
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
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
	
    if (_Obstacles[idx] > 0.1)
    {
        _Write3f[idx] = float3(0, 0, 0);
        return;
    }

    float3 uv = GetAdvectedPosTexCoords(id, idx);
			
    _Write3f[idx] = SampleBilinear(_Read3f, uv, _Size.xyz) * _Dissipate;
}

[numthreads(GroupSizeXYZ, GroupSizeXYZ, GroupSizeXYZ)]
void Advect(uint3 id : SV_DispatchThreadID)
{
    int idx = id.x + id.y * _Size.x + id.z * _Size.x * _Size.y;
	
    if (_Obstacles[idx] > 0.1)
    {
        _Write1f[idx] = 0;
        return;
    }

    float3 uv = GetAdvectedPosTexCoords(id, idx);
			
    _Write1f[idx] = max(0, SampleBilinear(_Read1f, uv, _Size.xyz) * _Dissipate);
}

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