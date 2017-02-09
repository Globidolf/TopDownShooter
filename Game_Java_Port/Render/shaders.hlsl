Texture2D Font : register(t0);

Texture2DArray Textures : register(t1);

SamplerState Sampler : register(s0);

cbuffer ConstantBuffer: register(b0)
{
    float4 DisplayArea;//XY = Offset -> Transpose | ZW = Size -> Scale
};

struct VertexObject {
	float4 Pos : SV_Position; // position in texels, converted to normals in the vertex shader
	float4 Color : COLOR; // filter: rgba
	float4 Tex : TEXCOORD0; // xy = texture pos, zw = pair of resource IDs
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
/*
//Test Shader
float4 PSMain(VertexObject input) : SV_Target
{
	return
	(input.Tex.z >= 0 ? // base texture?
		Textures.Sample(Sampler, float3(input.Tex.xy, 3)) : // yes, sample
		Font.Sample(Sampler, input.Tex.xy)) * // no, use font
		(false ? // secondary texture?
			Textures.Sample(Sampler, input.Tex.xyw) * input.Color : // yes, sample & multiply plus apply color filter
			input.Color); // no, simply apply color filter
}
*/
//This shader samples two textures in one go to multiply their pixels.
float4 PSMain(VertexObject input) : SV_Target
{
	return
	(input.Tex.z >= 0 ? // base texture?
		Textures.Sample(Sampler, input.Tex.xyz) : // yes, sample
		Font.Sample(Sampler, input.Tex.xy)) * // no, use font
	(input.Tex.w >= 0 ? // secondary texture?
		Textures.Sample(Sampler, input.Tex.xyw) * input.Color : // yes, sample & multiply plus apply color filter
		input.Color); // no, simply apply color filter
}