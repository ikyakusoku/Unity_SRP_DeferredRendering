using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	//使用相机名称作为命令缓冲区名称
	partial void PrepareBuffer();
	//通过ScriptableRenderContext.EmitWorldGeometryForSceneView，把UI添加到world geometry中，让他能被绘制出来
	partial void PrepareForSceneWindow();
	partial void DrawGizmos();

	//绘制不支持的材质的几何体
	partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
	//
	string SampleName { get; set; }

	//原先的shaderTag
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	//错误材质
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
		Profiler.BeginSample("Editor Only");//性能采样接口，可以在profiler中观测到采样的代码块的执行开销
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}
#else
	const string SampleName = bufferName;
#endif
}
