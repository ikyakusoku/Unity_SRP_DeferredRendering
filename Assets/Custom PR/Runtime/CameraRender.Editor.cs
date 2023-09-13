using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	//ʹ�����������Ϊ�����������
	partial void PrepareBuffer();
	//ͨ��ScriptableRenderContext.EmitWorldGeometryForSceneView����UI��ӵ�world geometry�У������ܱ����Ƴ���
	partial void PrepareForSceneWindow();
	partial void DrawGizmos();

	//���Ʋ�֧�ֵĲ��ʵļ�����
	partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
	//
	string SampleName { get; set; }

	//ԭ�ȵ�shaderTag
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	//�������
	static Material errorMaterial;

	
	partial void DrawUnsupportedShaders()
	{
		if (errorMaterial == null)
		{
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		){
			overrideMaterial = errorMaterial
		};
		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}


		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	partial void DrawGizmos()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	
	partial void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	partial void PrepareBuffer()
	{
		Profiler.BeginSample("Editor Only");//���ܲ����ӿڣ�������profiler�й۲⵽�����Ĵ�����ִ�п���
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}
#else
	const string SampleName = bufferName;
#endif
}
