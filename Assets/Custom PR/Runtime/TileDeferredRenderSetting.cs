using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TileDeferredRenderSetting 
{
    //tile��С
    public uint tileSizeX = 0;
    public uint tileSizeY = 0;
    
    public int perTileLightsCount=32;
    public int lightsIndicesBufferSize=0;

    //һ��tileռ��Ļ���صĳ���ı���
    public float tileX = 0;
    public float tileY = 0;

    //��Զƽ��Ĵ�С(����ռ�/�۲�����)
    public float nearPlaneWidth = 0;
    public float nearPlaneHeight = 0;

    public float farPlaneWidth = 0;
    public float farPlaneHeight = 0;

    private ComputeBuffer _tileLightsIndicesBuffer;
    private ComputeBuffer _tileLightsArgsBuffer;
    //ÿ��tile��ƽ��Ĺ۲�ռ��С


    public bool isConfig=false;

    private Vector4 BuildZBufferParams(float near, float far)
    {
        var result = new Vector4();
        result.x = 1 - far / near;
        result.y = 1 - result.x;
        result.z = result.x / far;
        result.w = result.y / far;
        return result;
    }

    public void ConfigTileSettings(ref ScriptableRenderContext context, ref Camera camera, ref ComputeShader computeShader,int kernelIndex)
    {
        isConfig=true;
        int kernelId=computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernelId, out tileSizeX, out tileSizeY, out uint groupSizeZ);
        var tileCountX = Mathf.CeilToInt(camera.pixelWidth *1f/tileSizeX);
        var tileCountY = Mathf.CeilToInt(camera.pixelHeight*1f/tileSizeY);
        lightsIndicesBufferSize = tileCountX * tileCountY * perTileLightsCount;
        var argsBufferSize = tileCountX * tileCountY;

        _tileLightsArgsBuffer = new ComputeBuffer(argsBufferSize, sizeof(int));
        Shader.SetGlobalBuffer(ShaderConstants.TileLightsArgsBuffer, _tileLightsArgsBuffer);
        computeShader.SetBuffer(0, ShaderConstants.RWTileLightsArgsBuffer, _tileLightsArgsBuffer);

        _tileLightsIndicesBuffer = new ComputeBuffer(lightsIndicesBufferSize, sizeof(int));
        Shader.SetGlobalBuffer(ShaderConstants.TileLightsIndicesBuffer, _tileLightsIndicesBuffer);
        computeShader.SetBuffer(0, ShaderConstants.RWTileLightsIndicesBuffer, _tileLightsIndicesBuffer);


        tileX = tileSizeX * 1f / camera.pixelWidth;
        tileY = tileSizeY * 1f / camera.pixelHeight;

        //fov����������˵Ӧ���ǹ۲�ռ�����ƽ��ļнǶ�������ƽ��
        nearPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f) * 2 * camera.nearClipPlane;
        //aspect�ǿ�ȸ�
        nearPlaneWidth = camera.aspect * nearPlaneHeight;

        var farDivNear= camera.nearClipPlane/camera.farClipPlane;

        farPlaneHeight = nearPlaneHeight * farDivNear;
        farPlaneWidth = nearPlaneWidth * farDivNear;

        var cmd = new CommandBuffer() { name="TileParams" };
        cmd.SetGlobalVector(ShaderConstants.DeferredTileParams, new Vector4(tileSizeX, tileSizeY, tileCountX, tileCountY));

        var zbufferParams = BuildZBufferParams(camera.nearClipPlane, camera.farClipPlane);
        cmd.SetComputeVectorParam(computeShader, ShaderConstants.ZBufferParams, zbufferParams);
        //tile��Ӧ����ƽ���ϵĵ�λ��С
        var basisH = new Vector2(tileSizeX * nearPlaneWidth / camera.pixelWidth, 0);
        var basisV = new Vector2(0, tileSizeY * nearPlaneHeight / camera.pixelHeight);
        cmd.SetComputeVectorParam(computeShader, ShaderConstants.CameraNearPlaneLB, new Vector4(-nearPlaneWidth / 2, -nearPlaneHeight / 2, camera.nearClipPlane, 0));
        cmd.SetComputeVectorParam(computeShader, ShaderConstants.CameraNearBasisH, basisH);
        cmd.SetComputeVectorParam(computeShader, ShaderConstants.CameraNearBasisV, basisV);
        //����
        cmd.DispatchCompute(computeShader, 0, tileCountX, tileCountY, 1);
        context.ExecuteCommandBuffer(cmd);

    }

    public static class ShaderConstants
    {
        public static readonly int TileCount = Shader.PropertyToID("_TileCount");
        public static readonly int CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int OutTexture = Shader.PropertyToID("_OutTexture");
        public static readonly int ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int CameraNearPlaneLB = Shader.PropertyToID("_CameraNearPlaneLB");
        public static readonly int CameraNearBasisH = Shader.PropertyToID("_CameraNearBasisH");
        public static readonly int CameraNearBasisV = Shader.PropertyToID("_CameraNearBasisV");
        public static readonly int RWTileLightsArgsBuffer = Shader.PropertyToID("_RWTileLightsArgsBuffer");
        public static readonly int RWTileLightsIndicesBuffer = Shader.PropertyToID("_RWTileLightsIndicesBuffer");


        //*************����Ϊȫ������*****************//

        public static readonly int TileLightsArgsBuffer = Shader.PropertyToID("_TileLightsArgsBuffer");
        public static readonly int TileLightsIndicesBuffer = Shader.PropertyToID("_TileLightsIndicesBuffer");
        public static readonly int DeferredTileParams = Shader.PropertyToID("_DeferredTileParams");

    }
}
