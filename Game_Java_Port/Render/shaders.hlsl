
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
	VertexObject output = (VertexObject)0;

	output.Pos.xy = (input.Pos.xy + DisplayArea.xy / 2) / DisplayArea.zw * 2;
	output.Pos.z = input.Pos.z / 1000 + 0.5;
	output.Pos.w = input.Pos.w;
	output.Pos.y = -output.Pos.y;
	output.Color = input.Color;
	output.Tex = input.Tex;

	return output;
}


float4 PSMain(VertexObject input) : SV_Target
{
	return  ShaderTexture.Sample(Sampler, input.Tex) * input.Color;
}