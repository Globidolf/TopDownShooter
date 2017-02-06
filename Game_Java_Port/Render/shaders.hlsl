
Texture2D ShaderTexture : register(t0);

SamplerState Sampler : register(s0);

cbuffer ConstantBuffer: register(b0)
{
    float4 DisplayArea;//XY = Offset -> Transpose | ZW = Size -> Scale
};

struct VertexObject {
	float4 Pos : SV_Position;
	float4 Color : COLOR; // filter
	float2 Tex : TEXCOORD0;
};


VertexObject VSMain2D(VertexObject input) {
	VertexObject output = input;
	output.Pos.xy = (output.Pos.xy + DisplayArea.xy / 2) / DisplayArea.zw * float2(2,-2);// transpose from display coordinates to normals
	output.Pos.z = 0;
	return output;
}

/* unused
VertexObject VSMain3D(VertexObject input) {
	VertexObject output = input;

	output.Pos.xy = (input.Pos.xy + DisplayArea.xy / 2) / DisplayArea.zw * 2; 
	output.Pos.z = input.Pos.z / 1000 + 0.5; // render everything in +- 500 range
	output.Pos.y = -output.Pos.y;

	return output;
}
*/


float4 PSMain(VertexObject input) : SV_Target
{
	return  ShaderTexture.Sample(Sampler, input.Tex) * input.Color; // filter the texture
}