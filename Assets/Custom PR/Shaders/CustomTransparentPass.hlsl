#ifndef CUSTOM_TRANSPARENT_PASS_INCLUDED
#define CUSTOM_TRANSPARENT_PASS_INCLUDED

#include"../ShaderLibrary/Cook_Torrance.hlsl"
#include"LightInfo.hlsl"

#include "UnityCG.cginc"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

//*****************************TEXTURES
	//CBUFFER_START(UnityPerMaterial)
		UNITY_DECLARE_TEX2D(_MainTex);			//漫反射
		UNITY_DECLARE_TEX2D(_BumpMap);			//法线
		UNITY_DECLARE_TEX2D(_MetallicGlossMap);	//金属度
		UNITY_DECLARE_TEX2D(_OcclusionMap);		//环境光遮蔽
		UNITY_DECLARE_TEX2D(_EmissionMap);		//自发光
	//CBUFFER_END
//**************************************

//***************************************OTHER_SETTING
	UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
		UNITY_DEFINE_INSTANCED_PROP(float, _Metallic_global)
		UNITY_DEFINE_INSTANCED_PROP(float, _Roughness_global)
		UNITY_DEFINE_INSTANCED_PROP(float, _Use_Normal_Map)
		UNITY_DEFINE_INSTANCED_PROP(float, _Use_Metal_Map)
		UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
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
	float4 positionWS : WS_POSITON;
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
	o.positionWS = mul(unity_ObjectToWorld, v.positionMS);
	//纹理缩放和平铺
	o.baseUV = v.baseUV * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST).xy + UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST).zw;// TRANSFORM_TEX(v.baseUV, _MainTex);
	o.normalWS = UnityObjectToWorldNormal(v.normalMS);
	o.tangentWS = UnityObjectToWorldDir(v.tangentMS.xyz);
	o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS) * v.tangentMS.w);
	//output.baseUV = v.baseUV;
	return o;
}

float4 Fragment(Varyings i) :SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	//采样获取纹理信息***********************************************
	float4 color = UNITY_SAMPLE_TEX2D(_MainTex,i.baseUV);
	//切线空间到世界空间的矩阵刚好对应法线的xyz
	float3x3 TBN = float3x3(normalize(i.tangentWS),normalize(i.bitangentWS),normalize(i.normalWS));

	float3 normal = i.normalWS;
	if (UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Use_Normal_Map))
	{
		normal = UnpackNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.baseUV));
		normal = normalize(mul(normal, TBN));
	}


	float metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic_global);
	float roughness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Roughness_global);
	if (UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Use_Metal_Map))
	{
		float4 metal = UNITY_SAMPLE_TEX2D(_MetallicGlossMap, i.baseUV);
		metallic = metal.r;
		roughness = 1.0 - metal.a;
	}

	float3 emission = UNITY_SAMPLE_TEX2D(_EmissionMap, i.baseUV).rgb;
	float ao = UNITY_SAMPLE_TEX2D(_OcclusionMap, i.baseUV).r;
	//************************************************************

	//屏幕空间坐标
	//float4 ndc = i.positionCS;
	//片元的世界坐标
	float4 worldPos = i.positionWS;
	//观察方向
	float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);

	//光照计算
	float3 rgb;
	for (int index = 0; index < DirectionalLightCount; index++)
	{
		rgb += PBR(color, normal, DirectionalLightDirections[index], viewDir, metallic, roughness, DirectionalLightColors[index]);
	}

	for (int index = 0; index < PointLightCount; index++)
	{
		float3 lightRay = PointLightPositions[index].xyz - worldPos.xyz;
		float3 lightDir = normalize(lightRay);
		float distanceSqr = max(Max0Dot(lightRay, lightRay), 0.00001);
		float attenuation = pow(max(0, 1 - pow(distanceSqr * PointLightPositions[index].w, 2)), 2) / distanceSqr;
		rgb += PBR(color, normal, lightDir, viewDir, metallic, roughness, PointLightColors[index]) * attenuation;
	}
	//return float4(divideIntoLevels(36,rgb), color.w);
	return float4(rgb, color.w);
	//return float4(i.positionCS.z, i.positionCS.z, i.positionCS.z, 1);

}

#endif