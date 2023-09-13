#ifndef CUSTOM_OPAQUE_PASS_INCLUDED
#define CUSTOM_OPAQUE_PASS_INCLUDED

//常用函数
//#include "../ShaderLibrary/Common.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "UnityCG.cginc"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

//*****************************TEXTURES
	//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
		UNITY_DECLARE_TEX2D(_MainTex);			//漫反射
		UNITY_DECLARE_TEX2D(_BumpMap);			//法线
		UNITY_DECLARE_TEX2D(_MetallicGlossMap);	//金属度
		UNITY_DECLARE_TEX2D(_OcclusionMap);		//环境光遮蔽
		UNITY_DECLARE_TEX2D(_EmissionMap);		//自发光
	//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
//**************************************

//***************************************OTHER_SETTING//要使用SRP批处理所有属性的全局声明必须在cbuffer中，也就是GPU常量缓冲区
	UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
		UNITY_DEFINE_INSTANCED_PROP(float,_Metallic_global)
		UNITY_DEFINE_INSTANCED_PROP(float,_Roughness_global)
		UNITY_DEFINE_INSTANCED_PROP(float,_Use_Normal_Map)
		UNITY_DEFINE_INSTANCED_PROP(float,_Use_Metal_Map)
		UNITY_DEFINE_INSTANCED_PROP(float4,_MainTex_ST)
	UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
//****************************************



//************************************structs
	struct Attributes {
		float4 positionMS : POSITION;
		float3 normalMS:NORMAL;
		float4 tangentMS: TANGENT;
		float2 baseUV : TEXCOORD0;
		
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings {
		float4 positionCS : SV_POSITION;
		float3 normalWS : NORMAL;
		float2 baseUV : VAR_BASE_UV;
		float3 tangentWS : VAR_TANGENT;
		float3 bitangentWS : VAR_BITANGENT;

		UNITY_VERTEX_INPUT_INSTANCE_ID
	};
//********************************************

Varyings Vertex(Attributes v)
{
	Varyings o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	o.positionCS = UnityObjectToClipPos(v.positionMS);
	//纹理缩放和平铺
	o.baseUV = v.baseUV * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST).xy + UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST).zw;//TRANSFORM_TEX(v.baseUV, _MainTex);

	o.normalWS = UnityObjectToWorldNormal(v.normalMS);
	o.tangentWS = UnityObjectToWorldDir(v.tangentMS.xyz);
	o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS) * v.tangentMS.w);

	return o;
}

void Fragment(
	Varyings i,
	out float4 GT0 : SV_Target0,
	out float4 GT1 : SV_Target1,
	out float4 GT2 : SV_Target2,
	out float4 GT3 : SV_Target3
)
{
	//float4 test = mul(UNITY_MATRIX_MVP, float4(1, 1, 1, 1));
	UNITY_SETUP_INSTANCE_ID(i);
	float4 color = UNITY_SAMPLE_TEX2D(_MainTex,i.baseUV);
	//切线空间到世界空间的矩阵刚好对应法线的xyz
	float3x3 TBN = float3x3(normalize(i.tangentWS),normalize(i.bitangentWS),normalize(i.normalWS));

	float3 normal = i.normalWS;
	if (UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Use_Normal_Map))
	{
		normal = UnpackNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.baseUV));
		normal = normalize(mul(normal, TBN));
	}


	float metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic_global);
	float roughness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Roughness_global);
	if (UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Use_Metal_Map))
	{
		float4 metal = UNITY_SAMPLE_TEX2D(_MetallicGlossMap, i.baseUV);
		metallic = metal.r;
		roughness = 1.0 - metal.a;
	}

	float3 emission = UNITY_SAMPLE_TEX2D(_EmissionMap, i.baseUV).rgb;
	float ao = UNITY_SAMPLE_TEX2D(_OcclusionMap, i.baseUV).r;


	GT0 = color;
	GT1 = float4(normal*0.5+0.5, 1);
	GT2 = float4(0, 0, roughness, metallic);
	GT3 = float4(emission, ao);

	//return GT1;
}

#endif