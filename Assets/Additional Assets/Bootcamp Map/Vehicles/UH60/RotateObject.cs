using UnityEngine;
using System.Collections;

public class RotateObject : MonoBehaviour
{
	public float rotatespeedY = 10.0f;
	public float rotatespeedX = 10.0f;
	public float rotatespeedZ = 0.0f;

	
	void Start()
	{
		//transform.Rotate(new Vector3(0, 0, 0));
		
	}

	
	void Update()
	{
		transform.Rotate(new Vector3(rotatespeedX * Time.deltaTime, rotatespeedY * Time.deltaTime, rotatespeedZ * Time.deltaTime));
		//transform.Rotate(new Vector3(rotatespeedX * Time.deltaTime, 0, 0));
	}
}
