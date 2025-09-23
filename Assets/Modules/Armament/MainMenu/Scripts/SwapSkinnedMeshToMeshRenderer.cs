using System;
using NaughtyAttributes;
using UnityEngine;
namespace Armament.MainMenu
{
	public class SwapSkinnedMeshToMeshRenderer : MonoBehaviour
	{

		[Button]
		private void BakeToMesh()
		{
			SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer == null) return;

			Mesh mesh = new Mesh();
			skinnedMeshRenderer.BakeMesh(mesh);

			GameObject newObject = new GameObject($"{name}_Mesh");
			newObject.transform.SetParent(transform.parent);
			newObject.transform.localPosition = transform.localPosition;
			newObject.transform.localRotation = transform.localRotation;
			newObject.transform.localScale = transform.localScale;

			newObject.AddComponent<MeshFilter>().mesh = mesh;
			MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;

			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(newObject);
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
			#endif

		}

	}
}