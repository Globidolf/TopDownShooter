
Texture2D ShaderTexture : register(t0);

SamplerState Sampler : register(s0);

cbuffer ConstantBuffer: register(b0)
{
    float4 DisplayArea;//XY = XY = Transpose | ZW = Width & Height = Scale
};

struct VertexObject {
	float4 Pos : SV_Position;
	float4 Color : COLOR;
	float2 Tex : TEXCOORD0;
};


VertexObject VSMain2D(VertexObject input) {
	VertexObject output = input;

	output.Pos.xy = (input.Pos.xy + DisplayArea.xy) / DisplayArea.zw;

	return output;
}

/* unused
VertexObject VSMain3D(VertexObject input)
{
	VertexObject output = input;

    output.Pos = mul(input.Pos, WorldViewProj);

    return output;
}
*/


float4 PSMain(VertexObject input) : SV_Target
{
    return ShaderTexture.Sample(Sampler, input.Tex);
}

float4 PSAlpha(VertexObject input) : SV_Target
{
	float4 sampled = ShaderTexture.Sample(Sampler, input.Tex);
	return float4(sampled.r, input.Tex.y % 1, input.Tex.x % 1, max(sampled.a, 0.5));
}