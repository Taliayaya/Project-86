using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts
{
	class Slicer
	{
		/// <summary>
		/// Slice the object by the plane 
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="objectToCut"></param>
		/// <returns></returns>
		public static GameObject[] Slice(Plane plane, GameObject objectToCut)
		{
			//Get the current mesh and its verts and tris
			Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
			var a = mesh.GetSubMesh(0);
			Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

			if (sliceable == null)
			{
				throw new NotSupportedException("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
			}

			//Create left and right slice of hollow object
			SlicesMetadata slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

			GameObject positiveObject = CreateMeshGameObject(objectToCut, out _);
			positiveObject.name = string.Format("{0}_positive", objectToCut.name);

			GameObject negativeObject = CreateMeshGameObject(objectToCut, out _);
			negativeObject.name = string.Format("{0}_negative", objectToCut.name);

			var positiveSideMeshData = slicesMeta.PositiveSideMesh;
			var negativeSideMeshData = slicesMeta.NegativeSideMesh;

			positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
			negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

			SetupCollidersAndRigidBodys(positiveObject, positiveSideMeshData, sliceable.UseGravity);
			SetupCollidersAndRigidBodys(negativeObject, negativeSideMeshData, sliceable.UseGravity);

			return new GameObject[] { positiveObject, negativeObject };
		}

		public static GameObject SliceNewPart(Plane plane, Sliceable objectToCut)
		{
			Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
			Sliceable sliceable = objectToCut;

			SlicesMetadata slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

			float maxPositive = objectToCut.transform.TransformPoint(slicesMeta.PositiveSideMesh.bounds.max).y;
			float maxNegative = objectToCut.transform.TransformPoint(slicesMeta.NegativeSideMesh.bounds.max).y;
			
			MeshSide upperMesh = maxPositive > maxNegative ? MeshSide.Positive : MeshSide.Negative;

			GameObject newPart = CreateMeshGameObject(objectToCut.gameObject, out Sliceable newSliceable);
			newPart.name = $"{objectToCut.name}_part";
			
			Mesh oldPartMesh;
			Mesh newPartMesh;
			if (upperMesh == MeshSide.Positive)
			{
				oldPartMesh = slicesMeta.PositiveSideMesh;
				newPartMesh = slicesMeta.NegativeSideMesh;
			}
			else
			{
				oldPartMesh = slicesMeta.NegativeSideMesh;
				newPartMesh = slicesMeta.PositiveSideMesh;
			}


			objectToCut.GetComponent<MeshFilter>().mesh = oldPartMesh;
			newPart.GetComponent<MeshFilter>().mesh = newPartMesh;
			oldPartMesh.RecalculateTangents();
			oldPartMesh.RecalculateNormals();
			oldPartMesh.RecalculateBounds();
			
			newPartMesh.RecalculateTangents();
			newPartMesh.RecalculateNormals();
			newPartMesh.RecalculateBounds();
			

			SetupCollidersAndRigidBodys(objectToCut.gameObject, oldPartMesh, false);
			SetupCollidersAndRigidBodys(newPart, newPartMesh, sliceable.UseGravity);

			// Disable Collider for object to cut. Let's make it cut only once for now

			// objectToCut.GetComponent<MeshCollider>().enabled = false;

			// newSliceable.CanSlice = false;

			return newPart;
		}

		/// <summary>
		/// Creates the default mesh game object.
		/// </summary>
		/// <param name="originalObject">The original object.</param>
		/// <returns></returns>
		private static GameObject CreateMeshGameObject(GameObject originalObject, out Sliceable newSliceable)
		{
			var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

			GameObject meshGameObject = new GameObject();
			Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

			meshGameObject.AddComponent<MeshFilter>();
			meshGameObject.AddComponent<MeshRenderer>();

			newSliceable = meshGameObject.AddComponent<Sliceable>();

			newSliceable.IsSolid = originalSliceable.IsSolid;
			newSliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
			newSliceable.UseGravity = originalSliceable.UseGravity;

			meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

			meshGameObject.transform.localScale = originalObject.transform.localScale;
			meshGameObject.transform.rotation = originalObject.transform.rotation;
			meshGameObject.transform.position = originalObject.transform.position;

			meshGameObject.tag = originalObject.tag;

			return meshGameObject;
		}

		/// <summary>
		/// Add mesh collider and rigid body to game object
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="mesh"></param>
		private static void SetupCollidersAndRigidBodys(GameObject gameObject, Mesh mesh, bool useGravity)
		{
			MeshCollider meshCollider;
			if (!gameObject.TryGetComponent(out meshCollider))
				meshCollider = gameObject.AddComponent<MeshCollider>();

			meshCollider.sharedMesh = mesh;
			meshCollider.convex = true;

			if (useGravity)
			{
				var rb = gameObject.AddComponent<Rigidbody>();
				rb.useGravity = useGravity;
			}
		}
	}
}