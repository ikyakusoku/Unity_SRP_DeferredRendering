using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/TestRenderPipeline")]
public class TestRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    bool useGPUInstancing = true, useSRPBatcher = true;

    [SerializeField]
    bool useTileDeferredRender=true;

    [SerializeField]
    public ComputeShader computeShader=default;
    protected override RenderPipeline CreatePipeline()
    {
        return new TestRenderPipeline(useGPUInstancing, useSRPBatcher, shadows,useTileDeferredRender ,ref computeShader);
    }
}
