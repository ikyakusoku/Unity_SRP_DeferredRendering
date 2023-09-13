using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class TestRenderPipeline : RenderPipeline
{
    //ScriptableRenderContext定义管线需要的绘制命令和渲染状态信息
    //ScriptableRenderContext context;

    CameraRenderer renderer = new CameraRenderer();

    bool useGPUInstancing;

    ShadowSettings shadowSettings;
    bool tileDeferredRender;

    ComputeShader computeShader;
    public TestRenderPipeline(bool useGPUInstancing, bool useSRPBatcher,ShadowSettings shadowSettings,bool useTileDeferredRender,ref ComputeShader computeShader)
    {
        this.computeShader=computeShader;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        this.tileDeferredRender = useTileDeferredRender;
        GraphicsSettings.lightsUseLinearIntensity = true;
        
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //处理相机
        foreach(Camera cam in cameras)
        {
            renderer.Render(context, cam, useGPUInstancing,tileDeferredRender,ref computeShader);
        }
    }
}

