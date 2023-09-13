using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PointlightSpawn : MonoBehaviour
{

	//[SerializeField]
	public int numOfLights=0;

	void Awake()
	{
		for (int i = 0; i < numOfLights; i++)
		{
			// Make a game object
			GameObject lightGameObject = new GameObject();

			// Add the light component
			Light lightComp = lightGameObject.AddComponent<Light>();

			// Set color and position
			lightComp.color = new Color(Random.value, Random.value, Random.value);

			lightComp.intensity = Random.Range(50.0f, 200.0f);

			lightComp.type = LightType.Point;

			lightComp.range = Random.Range(5, 10);

			lightGameObject.transform.position = Random.insideUnitSphere*22f;
		}
	}

	//void Update()
	//{	

	//}
}
