#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix WorldViewProjection;
texture cubeTexture;

float4x4 WorldInverseTranspose;
float4x4 World;
float4 AmbientColor = float4(1, 1, 1, 1);
float AmbientIntensity = 0.1;
 
float3 DiffuseLightDirection = float3(1, 0, 0);
float4 DiffuseColor = float4(1, 1, 1, 1);
float DiffuseIntensity = 1.0;
 
float Shininess = 200;
float4 SpecularColor = float4(1, 1, 1, 1);
float SpecularIntensity = 1;
float3 ViewVector = float3(1, 0, 0);

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
	float2 TexCoord : TEXCOORD0;
	float4 Normal	: NORMAL0;
};

struct InstancingVSoutput
{
	float4 Position : POSITION0;
	float3 Normal 	: TEXCOORD0;
	float2 TexCoord : TEXCOORD1;
	float4 Color	: COLOR0;
};

InstancingVSoutput InstancingVS(InstancingVSinput input, float4 instanceTransform : POSITION1,
								float2 atlasCoord : TEXCOORD1)
{
	InstancingVSoutput output;

	float4 pos = input.Position + instanceTransform;
	pos = mul(pos, WorldViewProjection);

	output.Position = pos;
	float4 normal = mul(input.Normal, WorldInverseTranspose);
    float lightIntensity = dot(normal.xyz, DiffuseLightDirection);
    output.Color = saturate(DiffuseColor * DiffuseIntensity * lightIntensity);
	output.Normal = normal.xyz;
	output.TexCoord = float2((input.TexCoord.x) + (atlasCoord.x),
							 (input.TexCoord.y) + (atlasCoord.y));
	return output;
}

float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
	float3 light = normalize(DiffuseLightDirection);
    float3 normal = normalize(input.Normal);
    float3 r = normalize(2 * dot(light, normal) * normal - light);
    float3 v = normalize(mul(float4(normalize(ViewVector),0), World)).xyz;
    float dotProduct = dot(r, v);
 
    float4 specular = SpecularIntensity * SpecularColor * max(pow(abs(dotProduct), Shininess), 0) * length(input.Color);
 
    float4 textureColor = tex2D(TextureSampler, input.TexCoord);
    textureColor.a = 1;
 
    return saturate(textureColor * (input.Color) + AmbientColor * AmbientIntensity + specular);
}

technique Instancing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL InstancingVS();
		PixelShader = compile PS_SHADERMODEL InstancingPS();
	}
};