using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
///     A SceneAnnotation component allows you to add a rich text display
///     to a GameObject and to align the scene camera to ths object for
///     tutorial purposes
/// </summary>
[ExecuteInEditMode]
public class SceneAnnotation : MonoBehaviour, IComparable<SceneAnnotation>
{
    public string headline = "Headline Goes Here";
    public TextAsset textAsset;
    public int id;

    public void OnDrawGizmos()
    {
#if UNITY_EDITOR
        var xform = gameObject.transform;
        Gizmos.DrawIcon(xform.position + Vector3.up, "Assets/TerrainToolsDemo/Scripts/Help_Icon.png", true);
        Gizmos.matrix = xform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.back, 90, 0.5f, 0.25f, 2);
        Gizmos.DrawCube(Vector3.back, new Vector3(1, 0.5f, 0.5f));
#endif
    }

    public int CompareTo(SceneAnnotation other)
    {
        //  Use the ID Value to sort scene objects for the inspector tool
        //  Add an extra check in case somebody got lazy and duplicated w/o changing IDs
        var idx = id.CompareTo(other.id);
        if (idx == 0) idx = string.Compare(headline, other.headline, StringComparison.Ordinal);
        return idx;
    }
}


#if UNITY_EDITOR
[InitializeOnLoad]
[CustomEditor(typeof(SceneAnnotation))]
public class SceneAnnotationEditor : Editor
{
    private static readonly string UXMLPath = "SceneAnnotation";
    public static bool isLoaded = false;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var visualTree = Resources.Load<VisualTreeAsset>(UXMLPath);
        VisualElement inspectorUI = visualTree.CloneTree();

        root.Add(inspectorUI);
        root.Q<Button>("CameraBtn").clicked += () => { AlignCamera(target.GameObject().transform); };
        root.Q<Button>("NextBtn").clicked += () => { GoToAnnotation(1); };
        root.Q<Button>("BackBtn").clicked += () => { GoToAnnotation(-1); };


        var sceneAnnotation = (SceneAnnotation) target;
        if (sceneAnnotation == null || sceneAnnotation.textAsset == null) return root;

        var displayElement = root.Q<VisualElement>("Spans");

        foreach (var eachParagraphText in sceneAnnotation.textAsset.text.Split('\n'))
        {
            var paragraph = new VisualElement();
            paragraph.AddToClassList("paragraph-container");

            foreach (var word in eachParagraphText.Split(' '))
            {
                var displayText = word;
                var linkText = "";

                // Markdown-styel link, but we have to use underscores as word seps:
                // [link_text_here](https:the-url.com)
                if (word.StartsWith("["))
                {
                    var paren = word.IndexOf("(");
                    Debug.Assert(paren > -1, $"Incorrectly formatted link {word}");
                    displayText = word.Substring(1, paren - 2);
                    displayText = displayText.Replace("_", " ");
                    displayText = "<b><u>" + displayText + "</u></b>";
                    linkText = word.Substring(paren + 1, word.Length - paren - 2);
                }

                // Narkdown * for bold
                if (word.StartsWith("*")) displayText = $"<b>{word.Replace("*", "")}</b>";

                // Markdown _ for italics
                if (word.StartsWith("_")) displayText = $"<i>{word.Replace("_", "")}</i>";

                var displaySpan = new Label(displayText);
                displaySpan.AddToClassList("display-text");
                if (linkText != "")
                {
                    displaySpan.RegisterCallback<MouseDownEvent>(evt => OpenURL(linkText, sceneAnnotation.gameObject));
                    displaySpan.AddToClassList("link");
                    displaySpan.tooltip = $"opens {linkText}";
                }

                paragraph.Add(displaySpan);
            }

            displayElement.Add(paragraph);
        }

        return root;
    }

    private static void OpenURL(string link, Object target)
    {
        if (link.StartsWith("http"))
        {
            Application.OpenURL(link);
        }
        else
        {
            var linked = AssetDatabase.LoadAssetAtPath<Object>(link);
            EditorGUIUtility.PingObject(linked);
            AssetDatabase.OpenAsset(linked);
            // Restore the selection to make it less confusing when selection is changed...
            Selection.objects = new Object[] {target};
        }
    }


    /// <summary>
    ///     Select the next or previous SceneAnnotation in the scene and
    ///     move the camera to match it's orientation
    /// </summary>
    /// <param name="delta">1 for next annotation, -1 for the previous one</param>
    private void GoToAnnotation(int delta)
    {
        var self = (SceneAnnotation) target;
        var annotationList = new List<SceneAnnotation>(FindObjectsOfType<SceneAnnotation>());
        annotationList.Sort();
        var here = annotationList.IndexOf(self);
        here += delta;
        // C# doesn't handle negative modulo
        here += annotationList.Count;
        here %= annotationList.Count;
        Selection.SetActiveObjectWithContext(annotationList[here].GameObject(), null);
        AlignCamera(annotationList[here].GameObject().transform);
    }

    /// <summary>
    ///     Move the scene camera to the location and orientation of a transform
    /// </summary>
    /// <param name="targetTransform">Transform to match</param>
    private static void AlignCamera(Transform targetTransform)
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return;

        var target = view.camera.GameObject();
        target.transform.position = targetTransform.position;
        target.transform.rotation = targetTransform.rotation;
        view.AlignViewToObject(target.transform);
    }
    
    static SceneAnnotationEditor()
    {
        if (!isLoaded)
        {
            EditorApplication.delayCall += AutoSelectFirstItem;
            isLoaded = true;
        }
        
    }

    static void AutoSelectFirstItem()
    {
        var annotationList = new List<SceneAnnotation>(FindObjectsOfType<SceneAnnotation>());
        if (annotationList.Count < 1) return;
        
        annotationList.Sort();
        Selection.objects = new UnityEngine.Object[] {annotationList[0].gameObject};
        AlignCamera(annotationList[0].gameObject.transform);
    }
}
#endif