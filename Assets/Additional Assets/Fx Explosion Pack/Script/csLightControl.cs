using UnityEngine;
using System.Collections;

public class csLightControl : MonoBehaviour {
	
	public Light _lihgt;
	float _time = 0;
	public float Delay = 0.5f;
	public float Down = 1;

	void Update ()
	{
		_time += Time.deltaTime;

		if(_time > Delay)
		{
			if(_lihgt.intensity > 0)
				_lihgt.intensity -= Time.deltaTime*Down;

			if(_lihgt.intensity <= 0)
				_lihgt.intensity = 0;
		}
	}
}
