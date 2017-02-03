Texture2D ShaderTexture : register(t0);
SamplerState Sampler : register(s0);

cbuffer PerObject: register(b0)
{
    float4x4 WorldViewProj;
};

struct VertexShaderInput
{
    float4 Pos : SV_Position;
    float4 Color : COLOR;
    float2 Tex : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Pos : SV_Position;
    float4 Color : COLOR;
    float2 Tex : TEXCOORD0;
};


VertexShaderOutput VSMain2D(VertexShaderInput input) {
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Pos = input.Pos;
	output.Tex = input.Tex;

	return input;
}

VertexShaderOutput VSMain3D(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    output.Pos = mul(input.Pos, WorldViewProj);
    output.Tex = input.Tex;

    return output;
}

VertexShaderOutput VSSecondary(VertexShaderInput input)
{
	return input;
}


float4 PSMain(VertexShaderOutput input) : SV_Target
{
    return ShaderTexture.Sample(Sampler, input.Tex);
}

float4 PSAlpha(VertexShaderOutput input) : SV_Target
{
	float4 sampled = ShaderTexture.Sample(Sampler, input.Tex);
	return float4(sampled.a, 0, 0, 1);
}