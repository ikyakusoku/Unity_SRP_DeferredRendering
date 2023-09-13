#ifndef LIGHTINFO_INCLUDED
#define LIGHTINFO_INCLUDED

#include "UnityCG.cginc"

//******************************************LightInfo
#define MAX_DIRECTIONAL_LIGHT_COUNT 8
#define MAX_POINT_LIGHT_COUNT 1023

CBUFFER_START(CustomDirectionalLight)
	int DirectionalLightCount;
	float4 DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

CBUFFER_START(CustomPointLight)
	int PointLightCount;
	float4 PointLightColors[MAX_POINT_LIGHT_COUNT];
	float4 PointLightPositions[MAX_POINT_LIGHT_COUNT];
CBUFFER_END
//***************************************************

#endif