// ALIyerEdon@gmail.com - Writed at July 2021
// All rights reserved

using UnityEditor;
using UnityEngine;

public class ALIyerEdonAssets_Lighting : EditorWindow
{
    [MenuItem("Window/Lighting Tools")]
    public static void ShowWindow()
    {
        GetWindow<ALIyerEdonAssets_Lighting>(false, "Lighting Tools", true);
    }
    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

    private const int windowWidth = 610;
    private const int windowHeight = 500;
    Vector2 _scrollPosition;
    public bool dontShow;

    void OnEnable()
    {
        titleContent = new GUIContent("Lighting Tools and Assets");
        maxSize = new Vector2(windowWidth, windowHeight);
        minSize = maxSize;

        if (EditorPrefs.GetInt("dontShow_urpflare") == 3)
            dontShow = true;
        if (EditorPrefs.GetInt("dontShow_urpflare") != 3)
            dontShow = false;
                        
    }

    private void OnGUI()
    {
        
        Texture2D border = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/border.psd") as Texture2D;
        Texture2D ad1 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad1.psd") as Texture2D;
        Texture2D ad2 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad2.psd") as Texture2D;
        Texture2D ad3 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad3.psd") as Texture2D;
        Texture2D ad4 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad4.psd") as Texture2D;
        Texture2D ad5 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad5.psd") as Texture2D;
        Texture2D ad6 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad6.psd") as Texture2D;
        Texture2D ad7 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad7.psd") as Texture2D;
        Texture2D ad8 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad8.psd") as Texture2D;
        Texture2D ad9 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad9.psd") as Texture2D;
        Texture2D ad10 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad10.psd") as Texture2D;
        Texture2D ad11 = EditorGUIUtility.Load("Assets/URP_Flares_Pack/Editor/Textures/UI/Ads/ad11.psd") as Texture2D;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Buy My Lighting Assets", MessageType.None);
        EditorGUILayout.Space();


        var dontShowRef = dontShow;

        dontShow = EditorGUILayout.Toggle("Don't show this page", dontShow);

        if (dontShowRef != dontShow)
        {
            if (dontShow == true)            
                EditorPrefs.SetInt("dontShow_urpflare", 3); // 3 == true
            if (dontShow == false)
                EditorPrefs.SetInt("dontShow_urpflare", 0); // 0 = false
        }

        


        EditorGUILayout.Space();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                     false,
                     false,
                     GUILayout.Width(windowWidth),
                     GUILayout.Height(windowHeight-20));        //---------Ad 1-------------------------------------------------
                                                                //  GUILayout.BeginVertical("Box");

        //_scrollPosition = EditorGUILayout.BeginScrollView(scrollViewRect, _scrollPosition, new Rect(0, 0, 2000, 2000));
       
        if (GUILayout.Button(border, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/23606");
        }

        if (GUILayout.Button(ad1, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/hdrp-lighting-box-2-nextgen-lighting-solution-180283");
        }

        if (GUILayout.Button(ad2, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/urp-lighting-box-2-181550");
        }

        if (GUILayout.Button(ad3, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/lighting-box-2-next-gen-lighting-solution-2021-93057");
        }

        if (GUILayout.Button(ad4, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/built-in-mobile-lighting-box-2021-181005");
        }

        if (GUILayout.Button(ad5, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/terrain-tessellation-shader-hdrp-and-built-in-191250");
        }

        if (GUILayout.Button(ad6, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/3d/environments/interior-lighting-kit-vr-mobile-standalone-84542");
        }

        if (GUILayout.Button(ad7, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/templates/tutorials/archviz-lighting-kit-paris-95071");
        }

        if (GUILayout.Button(ad8, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/3d/environments/interior-lighting-template-vol-2-88464");
        }

        if (GUILayout.Button(ad9, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/vfx/shaders/realistic-car-lighting-sample-2020-94970");
        }

        if (GUILayout.Button(ad10, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/3d/vegetation/trees/tree-collection-pack-76974");
        }

        if (GUILayout.Button(ad11, "", GUILayout.Width(600), GUILayout.Height(130)))
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/lighting-tools-2021-107069");
        }
        EditorGUILayout.EndScrollView();

    }
}


[InitializeOnLoad]
public class Startup
{
    static Startup()
    {
        EditorPrefs.SetInt("showCounts_urpflare", EditorPrefs.GetInt("showCounts_urpflare") + 1);
        if (EditorPrefs.GetInt("showCounts_urpflare") == 10)
        { 
            EditorApplication.ExecuteMenuItem("Window/Lighting Tools");
        }        
    }
} 
