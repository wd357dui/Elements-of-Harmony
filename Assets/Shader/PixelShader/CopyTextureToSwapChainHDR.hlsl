// this shader will replace the unnamed shader
// that copies the rendered texture to the swap chain back buffer

// notice that the order of the input in this one is different from other shaders
// (the order matters, must match exactly with the output from the vertex shader)
// in this one, TEXCOORD comes before SV_Position instead of after

SamplerState s0 : register(s0);
Texture2D<float4> t0 : register(t0);

cbuffer ConstantBuffer : register(b0)
{
	float DynamicRangeFactor;	// I added this to allow for control of target range
								// set with ID3D11DeviceContext::PSSetConstantBuffers
	float Unused1, Unused2, Unused3;
}

float4 main(float2 UV : TEXCOORD, float4 Position : SV_Position) : SV_Target
{
	float4 Result = t0.Sample(s0, UV);
	return float4(Result.rgb * 12.5f * DynamicRangeFactor, Result.a);
}