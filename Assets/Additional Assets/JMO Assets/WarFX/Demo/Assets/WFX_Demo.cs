using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
 *	Demo Scene Script for WAR FX
 *	
 *	(c) 2015, Jean Moreno
**/

public class WFX_Demo : MonoBehaviour
{
	public float cameraSpeed = 10f;
	
	public bool orderedSpawns = true;
	public float step = 1.0f;
	public float range = 5.0f;
	private float order = -5.0f;
	
	public GameObject walls;
	public GameObject bulletholes;
	
	public GameObject[] ParticleExamples;
	
	private int exampleIndex;
	private string randomSpawnsDelay = "0.5";
	private bool randomSpawns;
	
	private bool slowMo;
	private bool rotateCam = true;
	
	public Material wood,concrete,metal,checker;
	public Material woodWall,concreteWall,metalWall,checkerWall;
	private string groundTextureStr = "Checker";
	private List<string> groundTextures = new List<string>(new string[]{"Concrete","Wood","Metal","Checker"});
		
	void OnMouseDown()
	{
		RaycastHit hit = new RaycastHit();
		if(this.GetComponent<Collider>().Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 9999f))
		{
			GameObject particle = spawnParticle();
			if(!particle.name.StartsWith("WFX_MF"))
				particle.transform.position = hit.point + particle.transform.position;
		}
	}
	
	public GameObject spawnParticle()
	{
		GameObject particles = (GameObject)Instantiate(ParticleExamples[exampleIndex]);
		
		if(particles.name.StartsWith("WFX_MF"))
		{
			particles.transform.parent = ParticleExamples[exampleIndex].transform.parent;
			particles.transform.localPosition = ParticleExamples[exampleIndex].transform.localPosition;
			particles.transform.localRotation = ParticleExamples[exampleIndex].transform.localRotation;
		}
		else if(particles.name.Contains("Hole"))
		{
			particles.transform.parent = bulletholes.transform;
		}
		
		SetActiveCrossVersions(particles, true);
		
		return particles;
	}
	
	void SetActiveCrossVersions(GameObject obj, bool active)
	{
		#if UNITY_3_5
				obj.SetActiveRecursively(active);
		#else
				obj.SetActive(active);
				for(int i = 0; i < obj.transform.childCount; i++)
					obj.transform.GetChild(i).gameObject.SetActive(active);
		#endif
	}
	
	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(5,20,Screen.width-10,60));
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Effect: " + ParticleExamples[exampleIndex].name, GUILayout.Width(280));
		if(GUILayout.Button("<", GUILayout.Width(30)))
		{
			prevParticle();
		}
		if(GUILayout.Button(">", GUILayout.Width(30)))
		{
			nextParticle();
		}
		
		GUILayout.FlexibleSpace();
		GUILayout.Label("Click on the ground to spawn the selected effect");
		GUILayout.FlexibleSpace();
		
		if(GUILayout.Button(rotateCam ? "Pause Camera" : "Rotate Camera", GUILayout.Width(110)))
		{
			rotateCam = !rotateCam;
		}
		
		/*
		if(GUILayout.Button(randomSpawns ? "Stop Random Spawns" : "Start Random Spawns", GUILayout.Width(140)))
		{
			randomSpawns = !randomSpawns;
			if(randomSpawns)	StartCoroutine("RandomSpawnsCoroutine");
			else 				StopCoroutine("RandomSpawnsCoroutine");
		}
		
		randomSpawnsDelay = GUILayout.TextField(randomSpawnsDelay, 10, GUILayout.Width(42));
		randomSpawnsDelay = Regex.Replace(randomSpawnsDelay, @"[^0-9.]", "");
		*/
		
		if(GUILayout.Button(this.GetComponent<Renderer>().enabled ? "Hide Ground" : "Show Ground", GUILayout.Width(90)))
		{
			this.GetComponent<Renderer>().enabled = !this.GetComponent<Renderer>().enabled;
		}
		
		if(GUILayout.Button(slowMo ? "Normal Speed" : "Slow Motion", GUILayout.Width(100)))
		{
			slowMo = !slowMo;
			if(slowMo)	Time.timeScale = 0.33f;
			else  		Time.timeScale = 1.0f;
		}
		
		GUILayout.EndHorizontal();
		
		//--------------------
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Ground texture: " + groundTextureStr, GUILayout.Width(160));
		if(GUILayout.Button("<", GUILayout.Width(30)))
		{
			prevTexture();
		}
		if(GUILayout.Button(">", GUILayout.Width(30)))
		{
			nextTexture();
		}
		
		GUILayout.EndHorizontal();
		
		GUILayout.EndArea();
		
		//--------------------
		
		if(m4.GetComponent<Renderer>().enabled)
		{
			GUILayout.BeginArea(new Rect(5, Screen.height - 100, Screen.width - 10, 90));
			rotate_m4 = GUILayout.Toggle(rotate_m4, "AutoRotate Weapon", GUILayout.Width(250));
			GUI.enabled = !rotate_m4;
			float rx = m4.transform.localEulerAngles.x;
			rx = rx > 90 ? rx-180 : rx;
			float ry = m4.transform.localEulerAngles.y;
			float rz = m4.transform.localEulerAngles.z;
			rx = GUILayout.HorizontalSlider(rx, 0, 179, GUILayout.Width(256));
			ry = GUILayout.HorizontalSlider(ry, 0, 359, GUILayout.Width(256));
			rz = GUILayout.HorizontalSlider(rz, 0, 359, GUILayout.Width(256));
			if(GUI.changed)
			{
				if(rx > 90)
					rx += 180;
				
				m4.transform.localEulerAngles = new Vector3(rx,ry,rz);
				Debug.Log(rx);
			}
			GUILayout.EndArea();
		}
	}
	
	public GameObject m4, m4fps;
	private bool rotate_m4 = true;
	
	IEnumerator RandomSpawnsCoroutine()
	{
		
	LOOP:
		GameObject particles = spawnParticle();
		
		if(orderedSpawns)
		{
			particles.transform.position = this.transform.position + new Vector3(order,particles.transform.position.y,0);
			order -= step;
			if(order < -range) order = range;
		}
		else 				particles.transform.position = this.transform.position + new Vector3(Random.Range(-range,range),0,Random.Range(-range,range)) + new Vector3(0,particles.transform.position.y,0);
		
		yield return new WaitForSeconds(float.Parse(randomSpawnsDelay));
		
		goto LOOP;
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			prevParticle();
		}
		else if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			nextParticle();
		}
		
		if(rotateCam)
		{
			Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, cameraSpeed*Time.deltaTime);
		}
		if(rotate_m4)
		{
			m4.transform.Rotate(new Vector3(0,40f,0) * Time.deltaTime, Space.World);
		}
	}
	
	private void prevTexture()
	{
		int index = groundTextures.IndexOf(groundTextureStr);
		index--;
		if(index < 0)
			index = groundTextures.Count-1;
		
		groundTextureStr = groundTextures[index];
		
		selectMaterial();
		
	}
	private void nextTexture()
	{
		int index = groundTextures.IndexOf(groundTextureStr);
		index++;
		if(index >= groundTextures.Count)
			index = 0;
		
		groundTextureStr = groundTextures[index];
		
		selectMaterial();
	}
	
	private void selectMaterial()
	{
		switch(groundTextureStr)
		{
		case "Concrete":
			this.GetComponent<Renderer>().material = concrete;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = concreteWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = concreteWall;
			break;
			
		case "Wood":
			this.GetComponent<Renderer>().material = wood;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = woodWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = woodWall;
			break;

		case "Metal":
			this.GetComponent<Renderer>().material = metal;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = metalWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = metalWall;
			break;
			
		case "Checker":
			this.GetComponent<Renderer>().material = checker;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = checkerWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = checkerWall;
			break;
		}
	}
	
	private void prevParticle()
	{
		exampleIndex--;
		if(exampleIndex < 0) exampleIndex = ParticleExamples.Length - 1;
		
		showHideStuff();
	}
	private void nextParticle()
	{
		exampleIndex++;
		if(exampleIndex >= ParticleExamples.Length) exampleIndex = 0;
		
		showHideStuff();
	}
	
	private void showHideStuff()
	{
		//Show m4
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_MF Spr"))
			m4.GetComponent<Renderer>().enabled = true;
		else
			m4.GetComponent<Renderer>().enabled = false;
		
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_MF FPS"))
			m4fps.GetComponent<Renderer>().enabled = true;
		else
			m4fps.GetComponent<Renderer>().enabled = false;
		
		//Show walls
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_BImpact"))
		{
			SetActiveCrossVersions(walls, true);
			
			Renderer[] rs = bulletholes.GetComponentsInChildren<Renderer>();
			foreach(Renderer r in rs)
				r.enabled = true;
		}
		else
		{
			SetActiveCrossVersions(walls, false);
			
			Renderer[] rs = bulletholes.GetComponentsInChildren<Renderer>();
			foreach(Renderer r in rs)
				r.enabled = false;
		}
		
		//Change ground texture
		if(ParticleExamples[exampleIndex].name.Contains("Wood"))
		{
			groundTextureStr = "Wood";
			selectMaterial();
		}
		else if(ParticleExamples[exampleIndex].name.Contains("Concrete"))
		{
			groundTextureStr = "Concrete";
			selectMaterial();
		}
		else if(ParticleExamples[exampleIndex].name.Contains("Metal"))
		{
			groundTextureStr = "Metal";
			selectMaterial();
		}
		else if(ParticleExamples[exampleIndex].name.Contains("Dirt")
			|| ParticleExamples[exampleIndex].name.Contains("Sand")
			|| ParticleExamples[exampleIndex].name.Contains("SoftBody"))
		{
			groundTextureStr = "Checker";
			selectMaterial();
		}
		else if(ParticleExamples[exampleIndex].name == "WFX_Explosion")
		{
			groundTextureStr = "Checker";
			selectMaterial();
		}
	}
}
