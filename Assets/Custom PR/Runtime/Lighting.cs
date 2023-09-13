using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
public class Lighting
{
    static int dirLight_MaxCount = 8;
    static int pointLight_MaxCount = 1023;

    //光源属性ID
    static int dl_CountID=Shader.PropertyToID("DirectionalLightCount");
    static int dl_ColorsID = Shader.PropertyToID("DirectionalLightColors");
    static int dl_DirsID = Shader.PropertyToID("DirectionalLightDirections");

    static int pl_CountID = Shader.PropertyToID("PointLightCount");
    static int pl_ColorsID = Shader.PropertyToID("PointLightColors");
    //static int pl_DirsID = Shader.PropertyToID("PointLightDirections");
    static int pl_PossID = Shader.PropertyToID("PointLightPositions");


    //光源信息数组
    static Vector4[] dl_Colors = new Vector4[dirLight_MaxCount];
    static Vector4[] dl_Dirs = new Vector4[dirLight_MaxCount];

    static Vector4[] pl_Colors = new Vector4[pointLight_MaxCount];
    //static Vector4[] pl_Dirs = new Vector4[pointLight_MaxCount];
    static Vector4[] pl_Poss = new Vector4[pointLight_MaxCount];

    //命令缓冲
    const string bufferName = "Lighting";
    CommandBuffer buffer=new CommandBuffer()
    {
        name = bufferName
    };

    public void Setup(ref ScriptableRenderContext context, CullingResults cullingResults)
    {
        buffer.BeginSample(bufferName);
        //传递光源数组
        SetupLights(cullingResults);
        buffer.EndSample(bufferName);
        //执行缓冲
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

    }

    void SetupLights(CullingResults cullingResults)
    { 
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int plCount = 0,dlCount=0;
        for(int i = 0; i < visibleLights.Length; i++)
        {
            //判断光源类型
            //无平移说明是平行光源
            VisibleLight curLight = visibleLights[i];
            if (curLight.lightType==LightType.Directional&&dlCount<=dirLight_MaxCount)
            {
                //向数组中传值
                SetupDirectionalLight(dlCount++, ref curLight);
                
            }
            else if(curLight.lightType==LightType.Point&&plCount<=pointLight_MaxCount)
            {
                SetupPointLight(plCount++, ref curLight);
            }
        }

        buffer.SetGlobalInt(pl_CountID, plCount);
        //buffer.SetGlobalVectorArray(pl_DirsID, pl_Dirs);
        buffer.SetGlobalVectorArray(pl_ColorsID, pl_Colors);
        buffer.SetGlobalVectorArray(pl_PossID, pl_Poss);

        buffer.SetGlobalInt(dl_CountID, dlCount);
        buffer.SetGlobalVectorArray(dl_DirsID,dl_Dirs);
        buffer.SetGlobalVectorArray(dl_ColorsID,dl_Colors);
        
    }

    void SetupDirectionalLight(int index,ref VisibleLight vl)
    {
        dl_Colors[index] = vl.finalColor;
        //方向
        dl_Dirs[index] = -vl.localToWorldMatrix.GetColumn(2);
    }

    void SetupPointLight(int index,ref VisibleLight vl)
    {
        pl_Colors[index] = vl.finalColor;
        //pl_Dirs[index] = -vl.localToWorldMatrix.GetColumn(2);
        //位置
        pl_Poss[index] = vl.localToWorldMatrix.GetColumn(3);
        pl_Poss[index].w = 1f/Mathf.Max(vl.range * vl.range,0.00001f);
    }


}
