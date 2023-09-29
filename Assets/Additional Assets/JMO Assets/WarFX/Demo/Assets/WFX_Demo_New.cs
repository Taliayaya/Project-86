using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Cartoon FX - (c) 2015 - Jean Moreno
//
// Script handling the Demo scene of the Cartoon FX Packs

public class WFX_Demo_New : MonoBehaviour
{
	public Renderer groundRenderer;
	public Collider groundCollider;
	[Space]
	[Space]
	public Image slowMoBtn;
	public Text slowMoLabel;
	public Image camRotBtn;
	public Text camRotLabel;
	public Image groundBtn;
	public Text groundLabel;
	[Space]
	public Text EffectLabel;
	public Text EffectIndexLabel;

	//WFX
	public GameObject[] AdditionalEffects;
	public GameObject ground;
	public GameObject walls;
	public GameObject bulletholes;
	public GameObject m4, m4fps;
	public Material wood,concrete,metal,checker;
	public Material woodWall,concreteWall,metalWall,checkerWall;
	private string groundTextureStr = "Checker";
	private List<string> groundTextures = new List<string>(new string[]{"Concrete","Wood","Metal","Checker"});
	
	//-------------------------------------------------------------

	private GameObject[] ParticleExamples;
	private int exampleIndex;
	private bool slowMo;
	private Vector3 defaultCamPosition;
	private Quaternion defaultCamRotation;
	
	private List<GameObject> onScreenParticles = new List<GameObject>();
	
	//-------------------------------------------------------------
	
	void Awake()
	{
		List<GameObject> particleExampleList = new List<GameObject>();
		int nbChild = this.transform.childCount;
		for(int i = 0; i < nbChild; i++)
		{
			GameObject child = this.transform.GetChild(i).gameObject;
			particleExampleList.Add(child);
		}
		particleExampleList.AddRange(AdditionalEffects);
		ParticleExamples = particleExampleList.ToArray();
		
		defaultCamPosition = Camera.main.transform.position;
		defaultCamRotation = Camera.main.transform.rotation;
		
		StartCoroutine("CheckForDeletedParticles");
		
		UpdateUI();
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
		else if(Input.GetKeyDown(KeyCode.Delete))
		{
			destroyParticles();
		}
		
		if(Input.GetMouseButtonDown(0))
		{
			RaycastHit hit = new RaycastHit();
			if(groundCollider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 9999f))
			{
				GameObject particle = spawnParticle();
				if(!particle.name.StartsWith("WFX_MF"))
				particle.transform.position = hit.point + particle.transform.position;
			}
		}
		
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if(scroll != 0f)
		{
			Camera.main.transform.Translate(Vector3.forward * (scroll < 0f ? -1f : 1f), Space.Self);
		}
		
		if(Input.GetMouseButtonDown(2))
		{
			Camera.main.transform.position = defaultCamPosition;
			Camera.main.transform.rotation = defaultCamRotation;
		}
	}

	//-------------------------------------------------------------
	// MESSAGES

	public void OnToggleGround()
	{
		var c = Color.white;
		groundRenderer.enabled = !groundRenderer.enabled;
		c.a = groundRenderer.enabled ? 1f : 0.33f;
		groundBtn.color = c;
		groundLabel.color = c;
	}

	public void OnToggleCamera()
	{
		var c = Color.white;
		CFX_Demo_RotateCamera.rotating = !CFX_Demo_RotateCamera.rotating;
		c.a = CFX_Demo_RotateCamera.rotating ? 1f : 0.33f;
		camRotBtn.color = c;
		camRotLabel.color = c;
	}
	
	public void OnToggleSlowMo()
	{
		var c = Color.white;

		slowMo = !slowMo;
		if(slowMo)
		{
			Time.timeScale = 0.33f;
			c.a = 1f;
		}
		else
		{
			Time.timeScale = 1.0f;
			c.a = 0.33f;
		}

		slowMoBtn.color = c;
		slowMoLabel.color = c;
	}

	public void OnPreviousEffect()
	{
		prevParticle();
	}

	public void OnNextEffect()
	{
		nextParticle();
	}
	
	//-------------------------------------------------------------
	// UI
	
	private void UpdateUI()
	{
		EffectLabel.text = ParticleExamples[exampleIndex].name;
		EffectIndexLabel.text = string.Format("{0}/{1}", (exampleIndex+1).ToString("00"), ParticleExamples.Length.ToString("00"));
	}
	
	//-------------------------------------------------------------
	// SYSTEM
	
	public GameObject spawnParticle()
	{
		GameObject particles = (GameObject)Instantiate(ParticleExamples[exampleIndex]);
		particles.transform.position = new Vector3(0,particles.transform.position.y,0);
		#if UNITY_3_5
			particles.SetActiveRecursively(true);
		#else
			particles.SetActive(true);
//			for(int i = 0; i < particles.transform.childCount; i++)
//				particles.transform.GetChild(i).gameObject.SetActive(true);
		#endif
		
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

		ParticleSystem ps = particles.GetComponent<ParticleSystem>();
#if UNITY_5_5_OR_NEWER
		if (ps != null)
		{
			var main = ps.main;
			if (main.loop)
			{
				ps.gameObject.AddComponent<CFX_AutoStopLoopedEffect>();
				ps.gameObject.AddComponent<CFX_AutoDestructShuriken>();
			}
		}
#else
		if(ps != null && ps.loop)
		{
			ps.gameObject.AddComponent<CFX_AutoStopLoopedEffect>();
			ps.gameObject.AddComponent<CFX_AutoDestructShuriken>();
		}
#endif

		onScreenParticles.Add(particles);
		
		return particles;
	}
	
	IEnumerator CheckForDeletedParticles()
	{
		while(true)
		{
			yield return new WaitForSeconds(5.0f);
			for(int i = onScreenParticles.Count - 1; i >= 0; i--)
			{
				if(onScreenParticles[i] == null)
				{
					onScreenParticles.RemoveAt(i);
				}
			}
		}
	}
	
	private void prevParticle()
	{
		exampleIndex--;
		if(exampleIndex < 0) exampleIndex = ParticleExamples.Length - 1;
		
		UpdateUI();
		showHideStuff();
	}
	private void nextParticle()
	{
		exampleIndex++;
		if(exampleIndex >= ParticleExamples.Length) exampleIndex = 0;
		
		UpdateUI();
		showHideStuff();
	}
	
	private void destroyParticles()
	{
		for(int i = onScreenParticles.Count - 1; i >= 0; i--)
		{
			if(onScreenParticles[i] != null)
			{
				GameObject.Destroy(onScreenParticles[i]);
			}
			
			onScreenParticles.RemoveAt(i);
		}
	}
	
	// Change Textures
	
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
			ground.GetComponent<Renderer>().material = concrete;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = concreteWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = concreteWall;
			break;
			
		case "Wood":
			ground.GetComponent<Renderer>().material = wood;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = woodWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = woodWall;
			break;

		case "Metal":
			ground.GetComponent<Renderer>().material = metal;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = metalWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = metalWall;
			break;
			
		case "Checker":
			ground.GetComponent<Renderer>().material = checker;
			walls.transform.GetChild(0).GetComponent<Renderer>().material = checkerWall;
			walls.transform.GetChild(1).GetComponent<Renderer>().material = checkerWall;
			break;
		}
	}
	
	private void showHideStuff()
	{
		//Show m4
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_MF Spr"))
		{
			m4.GetComponent<Renderer>().enabled = true;
			Camera.main.transform.position = new Vector3(-2.482457f, 3.263842f, -0.004924395f);
			Camera.main.transform.eulerAngles = new Vector3(20f, 90f, 0f);
		}
		else
			m4.GetComponent<Renderer>().enabled = false;
		
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_MF FPS"))
			m4fps.GetComponent<Renderer>().enabled = true;
		else
			m4fps.GetComponent<Renderer>().enabled = false;
		
		//Show walls
		if(ParticleExamples[exampleIndex].name.StartsWith("WFX_BImpact"))
		{
			walls.SetActive(true);
			
			Renderer[] rs = bulletholes.GetComponentsInChildren<Renderer>();
			foreach(Renderer r in rs)
				r.enabled = true;
		}
		else
		{
			walls.SetActive(false);
			
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
