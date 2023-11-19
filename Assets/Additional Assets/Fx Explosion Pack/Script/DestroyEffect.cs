using System;
using UnityEngine;
using System.Collections;

public class DestroyEffect : MonoBehaviour {
	[SerializeField] private float destroyTime = 2;
	private void Awake()
	{
		StartCoroutine(DestroyEffectAfterTime());
	}

	private IEnumerator DestroyEffectAfterTime()
	{
		yield return new WaitForSeconds(destroyTime);
		Destroy(gameObject);
	}
}
