using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

public partial class CameraRenderer
{
//********************属性
	ScriptableRenderContext context;//渲染队列对象
	public Camera camera;					//相机

	const string bufferName = "Render Camera"; //命令缓冲对象

	CommandBuffer buffer = new CommandBuffer   //这里使用了对象初始化器
	{
		name = bufferName
	}; 

	//不透明物体的ShaderPassName
	static ShaderTagId opaque_shaderTagId = new ShaderTagId("SRP_OpaqueLit");
	//透明物体的ShaderPassName
	static ShaderTagId transparent_shaderTagId = new ShaderTagId("SRP_TransparentLit");

	CullingResults cullingResults;                             //裁剪函数cull返回的结果

	Lighting lighting=new Lighting();//光照处理

	//G-Buffer缓冲区
	RenderTexture[] gBuffers = new RenderTexture[4];
	RenderTexture gdepth;                                               // depth attachment
	RenderTargetIdentifier[] gbufferIDs = new RenderTargetIdentifier[4]; // tex ID 因为传入setRenderTarget的纹理都要用RenderTargetIdentifier的结构表示

	ComputeShader computeShader;
	TileDeferredRenderSetting tileSettings=new TileDeferredRenderSetting();

	//**************************************************


	public CameraRenderer()
    {
		gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
		gBuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		gBuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
		gBuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
		gBuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

		// 给纹理 ID 赋值
		for (int i = 0; i < 4; i++)
			gbufferIDs[i] = gBuffers[i];
	}

	//相机渲染入口
	public void Render(ScriptableRenderContext context, Camera camera, bool useGPUInstancing,bool useTileDeferredRender,ref ComputeShader computeShader)
	{
        this.context = context;
        this.camera = camera;
		this.computeShader = computeShader;

        PrepareBuffer();
        PrepareForSceneWindow();

		Setup();

		//裁剪对象初始化
		if (!Cull())
		{
			return;
		}

		//光照信息设置
		lighting.Setup(ref context, cullingResults);

		//DrawVisibleGeometry();
		Profiler.BeginSample("OpaqueDrawCall");
		DrawOpaqueGeometry(useGPUInstancing);
		Profiler.EndSample();

		PrepareTileCullingShader();

		Profiler.BeginSample("LightPassDrawCall");
		LightPass(context, camera);
		Profiler.EndSample();

		Profiler.BeginSample("SkyDrawCall");
		context.DrawSkybox(camera);
		Profiler.EndSample();

		DrawUnsupportedShaders();


		Profiler.BeginSample("TransparentDrawCall");
		DrawTransparentGeometry(useGPUInstancing);
		Profiler.EndSample();
		//Gizmos要在最后绘制
		DrawGizmos();

		Submit();
	}

	void PrepareTileCullingShader()
    {
		if(computeShader!=null)
        {
			if (!tileSettings.isConfig)
			{
				int kernelIndex = computeShader.FindKernel("CSMain");
				tileSettings.ConfigTileSettings(ref context, ref camera, ref computeShader, kernelIndex);
			}
		}
	}

	//延迟渲染光照计算通道
	void LightPass(ScriptableRenderContext context, Camera camera)
    {
        //使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Lightpass";

        Material mat = new Material(Shader.Find("Testlit/LightPassShader"));
        cmd.Blit(gbufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
		buffer.Clear();
    }

    //执行命令缓冲或者说提交命令缓冲到渲染队列，同时清空缓冲区
    void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	//向渲染命令队列传递相机属性，以及清空屏幕像素等其他初始设置
	//相机属性设置以后、清屏前再设置渲染对象
	void Setup()
	{
		SetMatrix_VP();
        //设置 gbuffer 为全局纹理
        buffer.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
			buffer.SetGlobalTexture("_GT" + i, gBuffers[i]);
		
		//放在ClearRenderTarget前，这样ClearRenderTarget可以调用更好的清理方式,向渲染命令队列中设置相机属性
		context.SetupCameraProperties(camera);
		buffer.SetRenderTarget(gbufferIDs,gdepth);
		//CameraClearFlags flags = camera.clearFlags;
		//ClearRenderTarget该接口包含了与缓冲区同名的采样buffername
		buffer.ClearRenderTarget(true, true, Color.clear);
		//buffer.ClearRenderTarget(flags<=CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ?
		//		camera.backgroundColor.linear : Color.clear);
		//采样缓冲区名称现实在调试器中
		buffer.BeginSample(SampleName);

		ExecuteBuffer();	
	}

	//传递/设置世界坐标转裁剪空间的矩阵以及逆矩阵
	void SetMatrix_VP()
    {
		// 设置相机矩阵
		Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
		Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
		Matrix4x4 vpMatrix = projMatrix * viewMatrix;
		Matrix4x4 vpMatrixInv = vpMatrix.inverse;
		buffer.SetGlobalMatrix("_vpMatrix", vpMatrix);
		buffer.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
		buffer.SetGlobalMatrix("_vMatrix", viewMatrix);
	}
	
	//绘制半透明物体
	void DrawTransparentGeometry(bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera) { 
			criteria=SortingCriteria.CommonTransparent
		};
		var drawSettings= new DrawingSettings(transparent_shaderTagId, sortingSettings) {
			enableInstancing = useGPUInstancing
		};
		var filterSettings = new FilteringSettings(RenderQueueRange.transparent);

		context.DrawRenderers(cullingResults,ref drawSettings,ref filterSettings);

    }

	//绘制不透明物体
	void DrawOpaqueGeometry(bool useGPUInstancing)
    {
		var sortSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};
		var drawSettings=new DrawingSettings(opaque_shaderTagId, sortSettings)
        {
			enableInstancing = useGPUInstancing
		};

		var filterSettings=new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(cullingResults,ref drawSettings,ref filterSettings);
    }

    //绘制相机视野内可见几何体
    void DrawVisibleGeometry()
	{
	//绘制指定shader的不透明几何体
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings();
		//过滤渲染队列
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

	//绘制天空盒
		context.DrawSkybox(camera);

	//绘制指定shader的半透明几何体
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}


	//裁剪
	bool Cull()
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void End()
    {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
	}
	//提交渲染队列
	void Submit()
	{
		End();
		context.Submit();
	}



}
