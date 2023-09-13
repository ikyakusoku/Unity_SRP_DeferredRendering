Shader "Testlit/TestShader_2"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1

		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

	Subshader
	{
		Tags { "LightMode" = "SPRlit" }

		Pass
		{
			//Blend[_SrcBlend][_DstBlend]
			//ZWrite[_ZWrite]
			//
			//HLSLPROGRAM
			//#pragma multi_compile_instancing
			//#pragma vertex Vertex
			//#pragma fragment Fragment

			////#include "FirstPass.hlsl"

			//ENDHLSL
		}
	}
}