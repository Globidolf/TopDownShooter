Texture2D ShaderTexture : register(t0);
SamplerState Sampler : register(s0);

cbuffer ConstantBuffer: register(b0)
{
    float4 DisplayArea;
};

struct VertexObject {
	float4 Pos : SV_Position;
	float4 Color : COLOR;
	float2 Tex : TEXCOORD0;
};


VertexObject VSMain2D(VertexObject input) {
	VertexObject output = (VertexObject)0;

	output.Pos = input.Pos;
	output.Tex = input.Tex;

	return input;
}

VertexObject VSMain3D(VertexObject input)
{
	VertexObject output = (VertexObject)0;

    output.Pos = mul(input.Pos, WorldViewProj);
    output.Tex = input.Tex;

    return output;
}

VertexObject VSSecondary(VertexObject input)
{
	return input;
}


float4 PSMain(VertexObject input) : SV_Target
{
    return ShaderTexture.Sample(Sampler, input.Tex);
}

float4 PSAlpha(VertexObject input) : SV_Target
{
	float4 sampled = ShaderTexture.Sample(Sampler, input.Tex);
	return float4(sampled.r, input.Tex.y % 1, input.Tex.x % 1, max(sampled.a, 0.5));
}