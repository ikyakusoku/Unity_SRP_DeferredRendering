using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class MeshBall : MonoBehaviour
{

	[SerializeField]
	Mesh mesh = default;


	[SerializeField]
	public Material[] material;

	[SerializeField]
	public uint number=0;

	Matrix4x4[] matrices = new Matrix4x4[2048];
	//Vector4[] baseColors = new Vector4[1023];

	//MaterialPropertyBlock block;

	//public bool turnOnInstance = true;
	void Awake()
	{
		for (int i = 0; i < number; i++)
		{
			matrices[i] = Matrix4x4.TRS(
				Random.insideUnitSphere * 20f, Quaternion.identity, Vector3.one
			);
		}

	}

	void Update()
	{
		//if (block == null)
		//{
		//	block = new MaterialPropertyBlock();
		//}
		Profiler.BeginSample("AutoDrawMesh");
		for(int i = 0; i < number; i++)
        {
			Graphics.DrawMesh(mesh, matrices[i], material[1], 0);
		}
		Profiler.EndSample();


	}
}
