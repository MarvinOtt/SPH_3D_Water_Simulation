#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif
float4x4 WVP;
float4x4 World;
float4x4 View;
float4x4 Projection;

sampler TextureSampler = sampler_state
{
	texture = <cubeTexture>;
	mipfilter = LINEAR;
	minfilter = LINEAR;
	magfilter = LINEAR;
};

struct InstancingVSinput
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL0;
};

struct InstancingVSoutput
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL0;
	float4 Color : COLOR0;
};

InstancingVSoutput InstancingVS(InstancingVSinput input, float4 instanceTransform : POSITION1)
{
	InstancingVSoutput output;
	float4 pos = float4(input.Position.xyz * 4, 0.00001f) + instanceTransform;
	/*float4 worldPosition = mul(pos, World);
	float4 viewPosition = mul(worldPosition, View);*/
	output.Position = mul(pos, WVP);

	//output.Position = pos;
	output.Normal = input.Normal;
	output.Color = float4(0, 0, 0.75f, 1.0f);
	return output;
}

float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
	float diffuse = clamp(dot(input.Normal.xyz, normalize(float3(1, 1, 0))), 0, 1);
	return input.Color + float4(1, 1, 1, 1) * diffuse * 0.6f;
}

technique Instancing
{
	pass Pass0
	{
		VertexShader = compile vs_5_0 InstancingVS();
		PixelShader = compile ps_5_0 InstancingPS();
	}
}