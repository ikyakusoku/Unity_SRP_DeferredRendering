#ifndef FIRST_PASS_INCLUDED
#define FIRST_PASS_INCLUDED

//常用函数
#include "../ShaderLibrary/Common.hlsl"


//TEXTURE2D(_BaseMap);
//SAMPLER(sampler_BaseMap);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)



//struct Attributes {
//	float3 positionOS : POSITION;
//	float2 baseUV : TEXCOORD0;
//	UNITY_VERTEX_INPUT_INSTANCE_ID
//};
//
//struct Varyings {
//	float4 positionCS : SV_POSITION;
//	float2 baseUV : VAR_BASE_UV;
//	UNITY_VERTEX_INPUT_INSTANCE_ID
//};
//
//Varyings Vertex(Attributes input) { //: SV_POSITION {
//	Varyings output;
//	UNITY_SETUP_INSTANCE_ID(input);
//	UNITY_TRANSFER_INSTANCE_ID(input, output);
//	float3 positionWS = TransformObjectToWorld(input.positionOS);
//	output.positionCS = TransformWorldToHClip(positionWS);
//
//	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
//	output.baseUV = input.baseUV * baseST.xy + baseST.zw;
//	return output;
//}
//
//float4 Fragment(Varyings input) : SV_TARGET{
//	UNITY_SETUP_INSTANCE_ID(input);
//	//SAMPLE_TEXTURE2D(纹理，采样器，uv)
//	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
//	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
//	return baseMap * baseColor;
//}

#endif