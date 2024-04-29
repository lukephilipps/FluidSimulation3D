#define NUM_SAMPLES 64
			
float4 _SmokeColor = float4(.8, 1, 1, 1);
float _SmokeAbsorption = 50;
uniform float3 _Size;
			
StructuredBuffer<float> _Density;

float4x4 _World;
float4x4 _View;
float4x4 _Projection;
float3 _CamPos;

struct VertexIn
{
    float3 Position : POSITION0;
};
struct v2f
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
};

v2f VS(VertexIn input)
{
    v2f OUT;
    
    float4 worldPos = mul(float4(input.Position, 1), _World);
    float4 viewPos = mul(worldPos, _View);
    OUT.pos = mul(viewPos, _Projection);
    OUT.worldPos = worldPos.xyz;
    
    return OUT;
}
			
struct Ray
{
    float3 origin;
    float3 dir;
};
			
struct AABB
{
    float3 Min;
    float3 Max;
};
			
//find intersection points of a ray with a box
bool intersectBox(Ray r, AABB aabb, out float t0, out float t1)
{
    float3 invR = 1.0 / r.dir;
    float3 tbot = invR * (aabb.Min - r.origin);
    float3 ttop = invR * (aabb.Max - r.origin);
    float3 tmin = min(ttop, tbot);
    float3 tmax = max(ttop, tbot);
    float2 t = max(tmin.xx, tmin.yz);
    t0 = max(t.x, t.y);
    t = min(tmax.xx, tmax.yz);
    t1 = min(t.x, t.y);
    return t0 <= t1;
}
			
float SampleBilinear(StructuredBuffer<float> buffer, float3 uv, float3 size)
{
    uv = saturate(uv);
    uv = uv * (size - 1.0);
				
    int x = uv.x;
    int y = uv.y;
    int z = uv.z;
				
    int X = size.x;
    int XY = size.x * size.y;
				
    float fx = uv.x - x;
    float fy = uv.y - y;
    float fz = uv.z - z;
				
    int xp1 = min(_Size.x - 1, x + 1);
    int yp1 = min(_Size.y - 1, y + 1);
    int zp1 = min(_Size.z - 1, z + 1);
				
    float x0 = buffer[x + y * X + z * XY] * (1.0f - fx) + buffer[xp1 + y * X + z * XY] * fx;
    float x1 = buffer[x + y * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + y * X + zp1 * XY] * fx;
				
    float x2 = buffer[x + yp1 * X + z * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + z * XY] * fx;
    float x3 = buffer[x + yp1 * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + zp1 * XY] * fx;
				
    float z0 = x0 * (1.0f - fz) + x1 * fz;
    float z1 = x2 * (1.0f - fz) + x3 * fz;
				
    return z0 * (1.0f - fy) + z1 * fy;
				
}
			
float4 PS(v2f IN) : COLOR
{
    float3 pos = _CamPos;
	
    Ray r;
    r.origin = pos;
    r.dir = normalize(IN.worldPos - pos);
	
    AABB aabb;
    aabb.Min = float3(-0.5, -0.5, -0.5);
    aabb.Max = float3(0.5, 0.5, 0.5);

	//figure out where ray from eye hit front of cube
    float tnear, tfar;
    intersectBox(r, aabb, tnear, tfar);
	
	//if eye is in cube then start ray at eye
    if (tnear < 0.0)
        tnear = 0.0;

    float3 rayStart = r.origin + r.dir * tnear;
    float3 rayStop = r.origin + r.dir * tfar;
    
    //convert to texture space
    rayStart = rayStart + 0.5;
    rayStop = rayStop + 0.5;
   	
    float3 start = rayStart;
    float dist = distance(rayStop, rayStart);
    float stepSize = dist / float(NUM_SAMPLES);
    float3 ds = normalize(rayStop - rayStart) * stepSize;
    float alpha = 1.0;

    for (int i = 0; i < NUM_SAMPLES; i++, start += ds)
    {
   				 
        float D = SampleBilinear(_Density, start, _Size);
   				 	
        alpha *= 1.0 - saturate(D * stepSize * _SmokeAbsorption);
        			
        if (alpha <= 0.01)
            break;
    }
    
    return _SmokeColor * (1 - alpha);
}

technique Tech0
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VS();
        PixelShader = compile ps_4_0 PS();
    }
}