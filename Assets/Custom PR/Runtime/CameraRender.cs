using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

public partial class CameraRenderer
{
//********************����
	ScriptableRenderContext context;//��Ⱦ���ж���
	public Camera camera;					//���

	const string bufferName = "Render Camera"; //��������

	CommandBuffer buffer = new CommandBuffer   //����ʹ���˶����ʼ����
	{
		name = bufferName
	}; 

	//��͸�������ShaderPassName
	static ShaderTagId opaque_shaderTagId = new ShaderTagId("SRP_OpaqueLit");
	//͸�������ShaderPassName
	static ShaderTagId transparent_shaderTagId = new ShaderTagId("SRP_TransparentLit");

	CullingResults cullingResults;                             //�ü�����cull���صĽ��

	Lighting lighting=new Lighting();//���մ���

	//G-Buffer������
	RenderTexture[] gBuffers = new RenderTexture[4];
	RenderTexture gdepth;                                               // depth attachment
	RenderTargetIdentifier[] gbufferIDs = new RenderTargetIdentifier[4]; // tex ID ��Ϊ����setRenderTarget������Ҫ��RenderTargetIdentifier�Ľṹ��ʾ

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

		// ������ ID ��ֵ
		for (int i = 0; i < 4; i++)
			gbufferIDs[i] = gBuffers[i];
	}

	//�����Ⱦ���
	public void Render(ScriptableRenderContext context, Camera camera, bool useGPUInstancing,bool useTileDeferredRender,ref ComputeShader computeShader)
	{
        this.context = context;
        this.camera = camera;
		this.computeShader = computeShader;

        PrepareBuffer();
        PrepareForSceneWindow();

		Setup();

		//�ü������ʼ��
		if (!Cull())
		{
			return;
		}

		//������Ϣ����
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
		//GizmosҪ��������
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

	//�ӳ���Ⱦ���ռ���ͨ��
	void LightPass(ScriptableRenderContext context, Camera camera)
    {
        //ʹ�� Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Lightpass";

        Material mat = new Material(Shader.Find("Testlit/LightPassShader"));
        cmd.Blit(gbufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
		buffer.Clear();
    }

    //ִ����������˵�ύ����嵽��Ⱦ���У�ͬʱ��ջ�����
    void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	//����Ⱦ������д���������ԣ��Լ������Ļ���ص�������ʼ����
	//������������Ժ�����ǰ��������Ⱦ����
	void Setup()
	{
		SetMatrix_VP();
        //���� gbuffer Ϊȫ������
        buffer.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
			buffer.SetGlobalTexture("_GT" + i, gBuffers[i]);
		
		//����ClearRenderTargetǰ������ClearRenderTarget���Ե��ø��õ�����ʽ,����Ⱦ��������������������
		context.SetupCameraProperties(camera);
		buffer.SetRenderTarget(gbufferIDs,gdepth);
		//CameraClearFlags flags = camera.clearFlags;
		//ClearRenderTarget�ýӿڰ������뻺����ͬ���Ĳ���buffername
		buffer.ClearRenderTarget(true, true, Color.clear);
		//buffer.ClearRenderTarget(flags<=CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ?
		//		camera.backgroundColor.linear : Color.clear);
		//����������������ʵ�ڵ�������
		buffer.BeginSample(SampleName);

		ExecuteBuffer();	
	}

	//����/������������ת�ü��ռ�ľ����Լ������
	void SetMatrix_VP()
    {
		// �����������
		Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
		Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
		Matrix4x4 vpMatrix = projMatrix * viewMatrix;
		Matrix4x4 vpMatrixInv = vpMatrix.inverse;
		buffer.SetGlobalMatrix("_vpMatrix", vpMatrix);
		buffer.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
		buffer.SetGlobalMatrix("_vMatrix", viewMatrix);
	}
	
	//���ư�͸������
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

	//���Ʋ�͸������
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

    //���������Ұ�ڿɼ�������
    void DrawVisibleGeometry()
	{
	//����ָ��shader�Ĳ�͸��������
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings();
		//������Ⱦ����
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

	//������պ�
		context.DrawSkybox(camera);

	//����ָ��shader�İ�͸��������
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}


	//�ü�
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
	//�ύ��Ⱦ����
	void Submit()
	{
		End();
		context.Submit();
	}



}
