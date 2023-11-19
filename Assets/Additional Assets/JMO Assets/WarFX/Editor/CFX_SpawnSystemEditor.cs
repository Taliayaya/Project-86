using UnityEngine;
using UnityEditor;
using System.Collections;

// Cartoon FX  - (c) 2012-2016 Jean Moreno

// CFX Spawn System Editor interface

[CustomEditor(typeof(CFX_SpawnSystem))]
public class CFX_SpawnSystemEditor : UnityEditor.Editor
{
	private SerializedProperty _hideObjectsInHierarchy;
	private SerializedProperty p_hideObjectsInHierarchy
	{
		get
		{
			if(_hideObjectsInHierarchy == null)
				_hideObjectsInHierarchy = this.serializedObject.FindProperty("hideObjectsInHierarchy");
			return _hideObjectsInHierarchy;
		}
	}

	private SerializedProperty _onlyGetInactiveObjects;
	private SerializedProperty p_onlyGetInactiveObjects
	{
		get
		{
			if(_onlyGetInactiveObjects == null)
				_onlyGetInactiveObjects = this.serializedObject.FindProperty("onlyGetInactiveObjects");
			return _onlyGetInactiveObjects;
		}
	}

	private SerializedProperty _instantiateIfNeeded;
	private SerializedProperty p_instantiateIfNeeded
	{
		get
		{
			if(_instantiateIfNeeded == null)
				_instantiateIfNeeded = this.serializedObject.FindProperty("instantiateIfNeeded");
			return _instantiateIfNeeded;
		}
	}

	private SerializedProperty _spawnAsChildren;
	private SerializedProperty p_spawnAsChildren
	{
		get
		{
			if(_spawnAsChildren == null)
				_spawnAsChildren = this.serializedObject.FindProperty("spawnAsChildren");
			return _spawnAsChildren;
		}
	}

	private GUIContent guiContent = new GUIContent();
	private GUIContent getGuiContent(string label, string tooltip = null)
	{
		guiContent.text = label;
		guiContent.tooltip = tooltip;
		return guiContent;
	}

	// GUI ----------------------------------------------------------------------------------------------------------------------------------------------------

	public override void OnInspectorGUI()
	{
		//Options
		EditorGUI.BeginChangeCheck();

#if UNITY_4_2
		EditorGUIUtility.LookLikeControls(230f);
#else
		float labelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 230f;
#endif

		EditorGUILayout.PropertyField(p_hideObjectsInHierarchy, getGuiContent("Hide preloaded objects in Hierarchy", "Will prevent the preloaded instances to show in the Hierarchy"));
		EditorGUILayout.PropertyField(p_spawnAsChildren, getGuiContent("Spawn as children of this GameObject", "Will place the preloaded instances under this GameObject, for better organization of the Hierarchy"));

		EditorGUILayout.PropertyField(p_onlyGetInactiveObjects, getGuiContent("Only retrieve inactive GameObjects", "Will only return preloaded instances if they are inactive.\nThis means that you have to deactivate the GameObject before being able to reuse it\n(this is already handled on Cartoon FX effects with the CFX_AutoDestructShuriken script)"));
		bool guiEnabled = GUI.enabled;
		GUI.enabled = p_onlyGetInactiveObjects.boolValue;
		EditorGUILayout.PropertyField(p_instantiateIfNeeded, getGuiContent("Instantiate new instances if needed", "If no active GameObject is found in the preloaded list, then instantiate new ones as needed"));
		GUI.enabled = guiEnabled;

#if !UNITY_4_2
		EditorGUIUtility.labelWidth = labelWidth;
#endif

		if(EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}

		//Drag/drop Prefabs
		GUI.SetNextControlName("DragDropBox");
		EditorGUILayout.HelpBox("Drag GameObjects you want to preload here!\n\nTIP:\nUse the Inspector Lock at the top right to be able to drag multiple objects at once!", MessageType.None);
		Rect dropRect = GUILayoutUtility.GetLastRect();

		for(int i = 0; i < (this.target as CFX_SpawnSystem).objectsToPreload.Length; i++)
		{
			GUILayout.BeginHorizontal();
			
			EditorGUI.BeginChangeCheck();
			GameObject obj = (GameObject)EditorGUILayout.ObjectField((this.target as CFX_SpawnSystem).objectsToPreload[i], typeof(GameObject), true);
			if(EditorGUI.EndChangeCheck())
			{
#if UNITY_4_2
				Undo.RegisterUndo(target, "change Spawn System object to preload");
#else
				Undo.RecordObject(target, "change Spawn System object to preload");
#endif
				(this.target as CFX_SpawnSystem).objectsToPreload[i] = obj;
			}
			EditorGUILayout.LabelField(new GUIContent("times","Number of times to copy the effect\nin the pool, i.e. the max number of\ntimes the object will be used\nsimultaneously"), GUILayout.Width(40));
			EditorGUI.BeginChangeCheck();
			int nb = EditorGUILayout.IntField("", (this.target as CFX_SpawnSystem).objectsToPreloadTimes[i], GUILayout.Width(50));
			if(nb < 1)
				nb = 1;
			if(EditorGUI.EndChangeCheck())
			{
#if UNITY_4_2
				Undo.RegisterUndo(target, "change Spawn System preload count");
#else
				Undo.RecordObject(target, "change Spawn System preload count");
#endif
				(this.target as CFX_SpawnSystem).objectsToPreloadTimes[i] = nb;
			}
			
			if(GUI.changed)
			{
				EditorUtility.SetDirty(target);
			}
			
			if(GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(24)))
			{
				Object preloadedObject = (this.target as CFX_SpawnSystem).objectsToPreload[i];
				string objectName = (preloadedObject == null) ? "" : preloadedObject.name;
#if UNITY_4_2
				Undo.RegisterUndo(target, string.Format("remove {0} from Spawn System", objectName));
#else
				Undo.RecordObject(target, string.Format("remove {0} from Spawn System", objectName));
#endif
				ArrayUtility.RemoveAt<GameObject>(ref (this.target as CFX_SpawnSystem).objectsToPreload, i);
				ArrayUtility.RemoveAt<int>(ref (this.target as CFX_SpawnSystem).objectsToPreloadTimes, i);
				
				EditorUtility.SetDirty(target);
			}
			
			GUILayout.EndHorizontal();
		}
		
		if((Event.current.type == EventType.DragPerform || Event.current.type == EventType.DragUpdated)
			&& dropRect.Contains(Event.current.mousePosition))
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			
			if(Event.current.type == EventType.DragPerform)
			{
				foreach(Object o in DragAndDrop.objectReferences)
				{
					if(o is GameObject)
					{
						bool already = false;
						foreach(GameObject otherObj in (this.target as CFX_SpawnSystem).objectsToPreload)
						{
							if(o == otherObj)
							{
								already = true;
								Debug.LogWarning("CFX_SpawnSystem: Object has already been added: " + o.name);
								break;
							}
						}
						
						if(!already)
						{
#if UNITY_4_2
							Undo.RegisterUndo(target, string.Format("add {0} to Spawn System", o.name));
#else
							Undo.RecordObject(target, string.Format("add {0} to Spawn System", o.name));
#endif
							ArrayUtility.Add<GameObject>(ref (this.target as CFX_SpawnSystem).objectsToPreload, (GameObject)o);
							ArrayUtility.Add<int>(ref (this.target as CFX_SpawnSystem).objectsToPreloadTimes, 1);
							
							EditorUtility.SetDirty(target);
						}
					}
				}
			}
		}
	}
}
