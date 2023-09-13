Shader "Testlit/MyLitShader"
{
    Properties
    {
        //������
        _MainTex("DiffuseTex", 2D) = "white" {}
        [Space(25)]

        //������ͼ
        [Toggle] _Use_Normal_Map("Use Normal Map", Float) = 1
        [NoScaleOffset][Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        [Space(25)]

        _Metallic_global("Metallic", Range(0, 1)) = 0.5
        _Roughness_global("Roughness", Range(0, 1)) = 0.5

        //��������ͼ
        [Toggle] _Use_Metal_Map("Use Metal Map", Float) = 1
        [NoScaleOffset]_MetallicGlossMap("Metallic Map", 2D) = "white" {}
        [Space(25)]

        //�������ڱ���ͼ
        [NoScaleOffset]_OcclusionMap("Occlusion Map", 2D) = "white" {}
        [Space(25)]

        //�Է�����ͼ
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "black" {}

        //͸���Ȼ��
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0

        //���д��
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {

        Pass
        {
            Tags { "LightMode" = "SRP_OpaqueLit" }

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include"CustomOpaquePass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "SRP_TransparentLit" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include"CustomTransparentPass.hlsl"


            ENDHLSL
        }
    }
}
