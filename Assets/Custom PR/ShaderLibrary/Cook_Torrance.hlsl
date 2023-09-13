#ifndef CUSTOM_COOKTORRANCE_INCLUDED
#define CUSTOM_COOKTORRANCE_INCLUDED

#define PI 3.14159265358

float3 RGBtoH(float3 rgb)
{
    return rgb.r * 0.299 + rgb.g * 0.587 + rgb.b * 0.114;
}

float3 divideIntoLevels(int level,float3 color)
{
    float3 H = RGBtoH(color);
    return float(int(H.r * level)) / level;
}

float Max0Dot(float3 v1, float3 v2)
{
    return max(0, dot(v1, v2));
}

//法线分布函数D(h).
float GGX_Dh(float3 normal, float3 halfVector, float alpha)
{
    float alpha_2 = pow(alpha, 2);
    float NdotH_2 = pow(Max0Dot(normal, halfVector), 2);
    return alpha_2 / (PI * pow(NdotH_2 * (alpha_2 - 1) + 1, 2));
}

//几何遮挡函数G(v).
float3 Schlick_GGX(float3 NdotVe, float k)
{
    return NdotVe / (NdotVe * (1 - k) + k);
}

//菲涅尔项F(h,v,F0).
float3 Fresnel_Schlick(float3 F0, float3 halfVector, float3 viewDir)
{
    return F0 + (1 - F0) * pow(1 - Max0Dot(halfVector, viewDir), 5);
}

//Cook-Torrance PBR
float3 PBR(float3 albedo, float3 normal, float3 lightDir, float3 viewDir, float metallic, float roughness, float3 lightColor)
{
    roughness = max(roughness, 0.05);
    //漫反射部分
    float3 f_lambert = albedo / PI;
    //镜面反射部分
    float3 halfVector = normalize(lightDir + viewDir);
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    float k = pow(roughness + 1, 2) / 8;

    float NdotV = Max0Dot(normal, viewDir);
    float NdotL = Max0Dot(normal, lightDir);

    float3 G = Schlick_GGX(NdotV, k) * Schlick_GGX(NdotL, k);
    float3 Ks = Fresnel_Schlick(F0, halfVector, lightDir);
    float D = GGX_Dh(normal, halfVector, roughness);

    float3 f_specular = D * Ks * G / (4 * NdotV * NdotL + 0.0001);
    float3 Kd = (1 - Ks) * (1 - metallic);
    //float3 fr =  Kd* f_lambert + f_specular;
    //return  fr * lightColor *NdotL;
    return max((Kd * f_lambert + f_specular) * lightColor * NdotL, 0);
}

#endif
