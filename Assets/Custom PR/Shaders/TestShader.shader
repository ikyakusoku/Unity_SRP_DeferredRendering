Shader "Testlit/TestShader"
{
    Properties
    {
        //漫反射
        _MainTex("DiffuseTex", 2D) = "white" {}
        [Space(25)]

        //法线贴图
        [Toggle] _Use_Normal_Map("Use Normal Map", Float) = 1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        [Space(25)]
        
        _Metallic_global("Metallic", Range(0, 1)) = 0.5
        _Roughness_global("Roughness", Range(0, 1)) = 0.5

        //金属度贴图
        [Toggle] _Use_Metal_Map("Use Metal Map", Float) = 1
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        [Space(25)]

        //环境光遮蔽贴图
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        [Space(25)]

        //自发光贴图
        _EmissionMap("Emission Map", 2D) = "black" {}

    }
        SubShader
    {
        Tags { "LightMode" = "gbuffer" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 tangent: TEXCOORD1;
                float3 bitangent : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _MetallicGlossMap;
            sampler2D _OcclusionMap;
            sampler2D _EmissionMap;

            float4 _MainTex_ST;


            float _Metallic_global;
            float _Roughness_global;


            float _Use_Normal_Map;
            float _Use_Metal_Map;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangent = normalize(cross(o.normal, o.tangent) * v.tangent.w);
                
                return o;
            }

            void frag(
                v2f i,
                out float4 GT0 : SV_Target0,
                out float4 GT1 : SV_Target1,
                out float4 GT2 : SV_Target2,
                out float4 GT3 : SV_Target3
                )
            {
                //构建TBN矩阵
                float3x3 TBN=float3x3(normalize(i.tangent),normalize(i.bitangent),normalize(i.normal));

                float4 color = tex2D(_MainTex, i.uv);
                float3 normal = i.normal;

                if (_Use_Normal_Map)
                {
                    normal = UnpackNormal(tex2D(_BumpMap, i.uv));
                    normal = normalize(mul(normal,TBN));
                }
                

                float metallic = _Metallic_global;
                float roughness = _Roughness_global;
                if (_Use_Metal_Map)
                {
                    float4 metal = tex2D(_MetallicGlossMap,i.uv);
                    metallic = metal.r;
                    roughness = 1.0-metal.a;
                }
                
                float3 emission = tex2D(_EmissionMap, i.uv).rgb;
                float ao = tex2D(_OcclusionMap, i.uv).r;

                GT0 = color;
                GT1 = float4(normal*0.5+0.5, 1);
                GT2 = float4(0, 0, roughness, metallic);
                GT3 = float4(emission, ao);
                
            }

            //pass:计算最终结果;


            ENDCG
        }
    }
}
