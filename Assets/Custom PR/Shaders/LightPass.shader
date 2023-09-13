Shader "Testlit/LightPassShader"
{
    Properties
    {
        //_MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Cull Off ZWrite On ZTest Always
        
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 3.0
            #include "UnityPBSLighting.cginc"
            #include"../ShaderLibrary/Cook_Torrance.hlsl"

            #include"LightInfo.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            //没有depth专属的采样器声明
            UNITY_DECLARE_TEX2D(_gdepth);
            UNITY_DECLARE_TEX2D(_GT0);
            UNITY_DECLARE_TEX2D(_GT1);
            UNITY_DECLARE_TEX2D(_GT2);
            UNITY_DECLARE_TEX2D(_GT3);


            CBUFFER_START(CustomMatrix)
                float4x4 _vpMatrixInv;
            CBUFFER_END

            fixed4 frag(v2f i, out float depthOut:SV_Depth) : SV_Target
            {
                float4 GT2 = UNITY_SAMPLE_TEX2D(_GT2,i.uv);
                float4 GT3 = UNITY_SAMPLE_TEX2D(_GT3, i.uv);

                float3 albedo = UNITY_SAMPLE_TEX2D(_GT0,i.uv).rgb;
                float3 normal = UNITY_SAMPLE_TEX2D(_GT1, i.uv).rgb*2-1;
                float2 motionVec = GT2.rg;
                float roughness = GT2.b;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;
                float depth = UNITY_SAMPLE_DEPTH(UNITY_SAMPLE_TEX2D(_gdepth, i.uv));
                float d_lin = Linear01Depth(depth);

                depthOut = depth;

                // 反投影重建世界坐标
                float4 ndcPos = float4(i.uv * 2 - 1, depth, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                float3 viewDir = normalize(float3(_WorldSpaceCameraPos.xyz - worldPos.xyz));
                float3 lightDir = normalize(float3(_WorldSpaceCameraPos.xyz - worldPos.xyz));

                //float3 specularTint;
                //float oneMinusReflectivity;
                //albedo = DiffuseAndSpecularFromMetallic(albedo, metallic, specularTint, oneMinusReflectivity);


                //UnityLight light;
                //light.color = _LightColor0.rgb;
                //light.dir = lightDir;
                //light.ndotl = DotClamped(normal, lightDir);

                //UnityIndirect indirectLight;
                //indirectLight.diffuse = 0;
                //indirectLight.specular = 0;
                ////Blinn-Phong
                //float3 halfVector = normalize(viewDir+lightDir);
                //float3 diffuse = albedo* _LightColor0.rgb*DotClamped(normal, lightDir);
                //float3 specular = specularTint * _LightColor0.rgb*pow(DotClamped(normal, halfVector),1-roughness);

                //return float4(_LightColor0.rgb, 1);
                float3 rgb;
                for (int dindex = 0; dindex < DirectionalLightCount; dindex++)
                {
                    rgb += PBR(albedo, normal, DirectionalLightDirections[dindex], viewDir, metallic, roughness, DirectionalLightColors[dindex]);
                }

                for (int pindex = 0; pindex < PointLightCount; pindex++)
                {
                    float3 lightRay = PointLightPositions[pindex].xyz - worldPos.xyz;
                    float3 lightDir = normalize(lightRay);
                    float distanceSqr = max(DotClamped(lightRay, lightRay),0.0001);
                    float attenuation = pow(max(0, 1 - pow(distanceSqr * PointLightPositions[pindex].w,2)),2)/ distanceSqr;
                    rgb += PBR(albedo, normal, lightDir, viewDir, metallic, roughness, PointLightColors[pindex])*attenuation;
                }

                float3 SHColor= ShadeSH9(float4(normal, 1));

                //return float4(SHColor, 1);
                //return float4(divideIntoLevels(36,rgb), 1) + float4(divideIntoLevels(36,emission), 1);
                //return float4(d_lin, d_lin, d_lin, 1);
                return float4(rgb, 1) + float4(emission, 1);
                //return UNITY_BRDF_PBS(albedo,specularTint, oneMinusReflectivity,1-roughness, normal, viewDir,light,indirectLight)+ float4(emission,1);
                //return float4(DotClamped(normal, halfVector), DotClamped(normal, halfVector), DotClamped(normal, halfVector), 1);
            }


            ENDCG
        }
    }
}