using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FMODUnity
{
    public class SetupWizardWindow : EditorWindow
    {
        private static SetupWizardWindow instance;

        private static readonly List<string> pageNames = new List<string>
        {
            L10n.Tr("Welcome"),
            L10n.Tr("Updating"),
            L10n.Tr("Linking"),
            L10n.Tr("Listener"),
            L10n.Tr("Unity Audio"),
            L10n.Tr("Unity Sources"),
            L10n.Tr("Source Control"),
            L10n.Tr("End")
        };

        private static readonly List<bool> pageComplete = new List<bool>(new bool[(int)PAGES.Max]);

        private static readonly List<UpdateTask> updateTasks = new List<UpdateTask>() {
            UpdateTask.Create(
                type: UpdateTaskType.ReorganizePluginFiles,
                name: L10n.Tr("Reorganize Plugin Files"),
                description: L10n.Tr("Move FMOD for Unity files to match the latest layout."),
                execute: FileReorganizer.ShowWindow,
                checkComplete: FileReorganizer.IsUpToDate
            ),
            UpdateTask.Create(
                type: UpdateTaskType.UpdateEventReferences,
                name: L10n.Tr("Update Event References"),
                description: L10n.Tr("Find event references that use the obsolete [FMODUnity.EventRef] attribute and update them to use the FMODUnity.EventReference type."),
                execute: EventReferenceUpdater.ShowWindow,
                checkComplete: EventReferenceUpdater.IsUpToDate
            ),
        };

        private static bool updateTaskStatusChecked = false;

        private PAGES currentPage = PAGES.Welcome;

        private AudioListener[] unityListeners;
        private StudioListener[] fmodListeners;
        private Vector2 scroll1, scroll2, pageScrollPos;
        private Vector2 stagingDetailsScroll;
        private bool bFoundUnityListener;
        private bool bFoundFmodListener;

        private AudioSource[] unityAudioSources;

        private GUIStyle titleStyle;
        private GUIStyle titleLeftStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle navButtonStyle;
        private GUIStyle sourceButtonStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle crumbStyle;
        private GUIStyle columnStyle;
        private Color crumbDefault;
        private Color crumbHighlight;

        private const string logoBlack = "FMODLogoBlack.png";
        private const string logoWhite = "FMODLogoWhite.png";

        private Texture2D logoTexture;
        private Texture2D tickTexture;
        private Texture2D crossTexture;
        private GUIStyle iconStyle;

        private SimpleTreeView m_SimpleTreeView;
        private TreeViewState m_TreeViewState;

        private bool bStudioLinked;
        private bool isValidSource = true;
        private string invalidMessage;

        private static StagingSystem.UpdateStep nextStagingStep;

        private static bool IsStagingUpdateInProgress => nextStagingStep != null;

        private Vector2 ignoreFileScrollPosition = Vector2.zero;

        private const string IgnoreFileText =
@"# Never ignore DLLs in the FMOD subfolder.
!/[Aa]ssets/Plugins/FMOD/**/lib/*

# Don't ignore images and gizmos used by FMOD in the Unity Editor.
!/[Aa]ssets/Gizmos/FMOD/*
!/[Aa]ssets/Editor Default Resources/FMOD/*

# Ignore the Cache folder since it is updated locally.
/[Aa]ssets/Plugins/FMOD/Cache/*

# Ignore bank files in the StreamingAssets folder.
/[Aa]ssets/StreamingAssets/**/*.bank
/[Aa]ssets/StreamingAssets/**/*.bank.meta

# If the source bank files are kept outside of the StreamingAssets folder then these can be ignored.
# Log files can be ignored.
fmod_editor.log";

        private const string GitAttributesText =
@"Assets/Plugins/FMOD/**/*.bundle text eol=lf
Assets/Plugins/FMOD/**/Info.plist text eol=lf";

        private enum PAGES : int
        {
            Welcome = 0,
            Updating,
            Linking,
            Listener,
            UnityAudio,
            UnitySources,
            SourceControl,
            End,
            Max
        }

        public enum UpdateTaskType
        {
            ReorganizePluginFiles,
            UpdateEventReferences,
        }

        private class UpdateTask
        {
            public UpdateTaskType Type;
            public string Name;
            public string Description;
            public bool IsComplete;
            public Action Execute;
            public Func<bool> CheckComplete;

            public static UpdateTask Create(UpdateTaskType type, string name, string description,
                Action execute, Func<bool> checkComplete)
            {
                return new UpdateTask() {
                    Type = type,
                    Name = name,
                    Description = description,
                    Execute = execute,
                    CheckComplete = checkComplete
                };
            }
        }

        public static void SetUpdateTaskComplete(UpdateTaskType type)
        {
            foreach (UpdateTask task in updateTasks.Where(t => t.Type == type))
            {
                task.IsComplete = true;
            }
        }

        private static void CheckUpdateTaskStatus()
        {
            if (!updateTaskStatusChecked)
            {
                updateTaskStatusChecked = true;

                foreach (UpdateTask task in updateTasks)
                {
                    task.IsComplete = task.CheckComplete();
                }
            }
        }

        private static void DoNextStagingStep()
        {
            nextStagingStep.Execute();
            nextStagingStep = StagingSystem.GetNextUpdateStep();
        }

        public static void Startup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Settings settings = Settings.Instance;

            if (settings.CurrentVersion != FMOD.VERSION.number)
            {
                // We're updating an existing installation; unhide the setup wizard if needed

                CheckUpdateTaskStatus();

                if (settings.HideSetupWizard && updateTasks.Any(t => !t.IsComplete))
                {
                    settings.HideSetupWizard = false;
                }

                settings.CurrentVersion = FMOD.VERSION.number;
                EditorUtility.SetDirty(settings);
            }

            nextStagingStep = StagingSystem.Startup();

            if (!settings.HideSetupWizard || IsStagingUpdateInProgress)
            {
                ShowAssistant();
            }
        }

        [MenuItem("FMOD/Setup Wizard")]
        public static void ShowAssistant()
        {
            instance = (SetupWizardWindow)GetWindow(typeof(SetupWizardWindow), true, L10n.Tr("FMOD Setup Wizard"));
            instance.ShowUtility();
            instance.minSize = new Vector2(600, 400);
            var position = new Rect(Vector2.zero, new Vector2(800, 600));
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / 2;
            position.center = screenCenter / EditorGUIUtility.pixelsPerPoint;
            instance.position = position;
        }

        private void OnEnable()
        {
            CheckUpdateTaskStatus();

            logoTexture = EditorUtils.LoadImage(EditorGUIUtility.isProSkin ? logoWhite : logoBlack);

            crossTexture = EditorUtils.LoadImage("CrossYellow.png");
            tickTexture = EditorUtils.LoadImage("TickGreen.png");

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            bodyStyle = new GUIStyle(titleStyle);
            crumbStyle = new GUIStyle(titleStyle);
            crumbDefault = EditorGUIUtility.isProSkin ? Color.gray : Color.gray;
            crumbHighlight = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            scroll1 = scroll2 = new Vector2();

            iconStyle = new GUIStyle();
            iconStyle.alignment = TextAnchor.MiddleCenter;

            EditorUtils.ValidateSource(out isValidSource, out invalidMessage);

            CheckUpdatesComplete();
            CheckStudioLinked();
            CheckListeners();
            CheckSources();
            CheckUnityAudio();
        }

        private void OnGUI()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle("Button");
                buttonStyle.fixedHeight = 30;

                sourceButtonStyle = new GUIStyle("button");
                sourceButtonStyle.fixedWidth = 150;
                sourceButtonStyle.fixedHeight = 35;
                sourceButtonStyle.margin = new RectOffset();

                descriptionStyle = new GUIStyle(titleStyle);
                descriptionStyle.fontStyle = FontStyle.Normal;
                descriptionStyle.alignment = TextAnchor.MiddleLeft;
                descriptionStyle.margin = new RectOffset(5,0,0,0);

                titleLeftStyle = new GUIStyle(descriptionStyle);
                titleLeftStyle.fontStyle = FontStyle.Bold;

                descriptionStyle.fixedWidth = 350;
                columnStyle = new GUIStyle();
                columnStyle.margin.left = 50;
                columnStyle.margin.right = 50;
            }

            // Draw Header
            EditorGUILayout.Space();
            GUILayout.Box(logoTexture, titleStyle);
            EditorGUILayout.Space();

            if (IsStagingUpdateInProgress)
            {
                StagingUpdatePage();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                Breadcrumbs();

                // Draw Body
                using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
                {
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(pageScrollPos))
                    {
                        pageScrollPos = scrollView.scrollPosition;

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();

                        switch (currentPage)
                        {
                            case PAGES.Welcome: WelcomePage(); break;
                            case PAGES.Updating: UpdatingPage(); break;
                            case PAGES.Linking: LinkingPage(); break;
                            case PAGES.Listener: ListenerPage(); break;
                            case PAGES.UnityAudio: DisableUnityAudioPage(); break;
                            case PAGES.UnitySources: UnitySources(); break;
                            case PAGES.SourceControl: SourceControl(); break;
                            case PAGES.End: EndPage(); break;
                        }
                    }

                    Buttons();

                    if (currentPage == PAGES.Welcome)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                bool hide = Settings.Instance.HideSetupWizard;

                                hide = EditorGUILayout.Toggle(L10n.Tr("Do not display this again"), hide);

                                if (check.changed)
                                {
                                    Settings.Instance.HideSetupWizard = hide;
                                    EditorUtility.SetDirty(Settings.Instance);
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    else
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                }
            }
        }

        private void OnInspectorUpdate()
        {
            switch (currentPage)
            {
                case PAGES.Welcome:                         break;
                case PAGES.Updating: CheckUpdatesComplete();break;
                case PAGES.Linking:     CheckStudioLinked();break;
                case PAGES.Listener:    CheckListeners();   break;
                case PAGES.UnityAudio:                      break;
                case PAGES.UnitySources:CheckSources();     break;
                case PAGES.SourceControl:                   break;
                case PAGES.End:                             break;
                case PAGES.Max:                             break;
                default:                                    break;
            }
        }

        private void CheckUpdatesComplete()
        {
            pageComplete[(int)PAGES.Updating] = updateTasks.All(t => t.IsComplete);
        }

        private void CheckStudioLinked()
        {
            pageComplete[(int)PAGES.Linking] = isValidSource;
        }

        private bool IsStudioLinked()
        {
            return !string.IsNullOrEmpty(Settings.Instance.SourceBankPath);
        }

        private void CheckListeners()
        {
            var UListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            var FListeners = Resources.FindObjectsOfTypeAll<StudioListener>();
            if ((unityListeners == null || fmodListeners == null) || (!unityListeners.SequenceEqual(UListeners) || !fmodListeners.SequenceEqual(FListeners)))
            {
                unityListeners = UListeners;
                bFoundUnityListener = unityListeners.Length > 0;
                fmodListeners = FListeners;
                bFoundFmodListener = fmodListeners.Length > 0;
                Repaint();
            }
            pageComplete[(int)PAGES.Listener] = (!bFoundUnityListener && bFoundFmodListener);
        }

        private void CheckSources()
        {
            var ASources = Resources.FindObjectsOfTypeAll<AudioSource>();
            if (unityAudioSources == null || !ASources.SequenceEqual(unityAudioSources))
            {
                unityAudioSources = ASources;
                if (m_SimpleTreeView != null && unityAudioSources.Length > 0)
                {
                    m_SimpleTreeView.Reload();
                }
            }
            pageComplete[(int)PAGES.UnitySources] = ASources != null ? (ASources.Length == 0) : true;
        }

        private void CheckUnityAudio()
        {
            var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
            var serializedManager = new SerializedObject(audioManager);
            var prop = serializedManager.FindProperty("m_DisableAudio");
            pageComplete[(int)PAGES.UnityAudio] = prop.boolValue;
        }

        private void WelcomePage()
        {
            GUILayout.FlexibleSpace();

            string message = string.Format(L10n.Tr("Welcome to FMOD for Unity {0}."),
                EditorUtils.VersionString(FMOD.VERSION.number));

            EditorGUILayout.LabelField(message, titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(L10n.Tr("This setup wizard will help you configure your project to use FMOD."), titleStyle);

            GUILayout.FlexibleSpace();
        }

        private void Breadcrumbs()
        {
            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(150)))
            {
                crumbStyle.alignment = TextAnchor.MiddleCenter;
                Color oldColor = GUI.backgroundColor;
                EditorGUILayout.Space();

                for (int i = 0; i < pageNames.Count; i++)
                {
                    if (i > 0 && i < pageNames.Count - 1)
                    {
                        GUI.backgroundColor = (pageComplete[i] ? Color.green : Color.yellow);
                    }
                    crumbStyle.normal.textColor = (i == (int)currentPage ? crumbHighlight : crumbDefault);
                    using (var b = new EditorGUILayout.HorizontalScope("button", GUILayout.Height(22)))
                    {
                        if (GUI.Button(b.rect, pageNames[i], crumbStyle))
                        {
                            currentPage = (PAGES)i;
                        }
                        EditorGUILayout.Space();
                    }
                    GUI.backgroundColor = oldColor;
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void UpdatingPage()
        {
            EditorGUILayout.LabelField(L10n.Tr("If you are updating an existing FMOD installation, you may need to perform some update tasks."), titleLeftStyle);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(L10n.Tr("Choose an update task to perform:"), titleStyle);

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                float buttonWidth = 0;

                foreach (UpdateTask task in updateTasks)
                {
                    buttonWidth = Math.Max(buttonWidth, buttonStyle.CalcSize(new GUIContent(task.Name)).x);
                }

                float buttonHeight = buttonStyle.CalcSize(GUIContent.none).y;

                foreach (UpdateTask task in updateTasks)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.Space();

                        GUILayout.Label(task.IsComplete ? tickTexture : crossTexture, iconStyle,
                            GUILayout.Height(buttonHeight));

                        if (GUILayout.Button(task.Name, buttonStyle, GUILayout.Width(buttonWidth)))
                        {
                            task.Execute();
                        }

                        GUILayout.Label(task.Description, descriptionStyle, GUILayout.MinHeight(buttonHeight));

                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.Space();
                }
            }

            GUILayout.FlexibleSpace();
        }

        private void LinkingPage()
        {
            EditorGUILayout.LabelField(L10n.Tr("In order to access your FMOD Studio content you need to locate the FMOD Studio Project or the .bank files that FMOD Studio produces, and configure a few other settings."), titleLeftStyle);
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(L10n.Tr("Choose how to access your FMOD Studio content:"), titleLeftStyle);

            EditorGUILayout.Space();
            using (new GUILayout.VerticalScope("box"))
            {
                float indent = 5;
                var serializedObject = new SerializedObject(Settings.Instance);

                var boxStyle = new GUIStyle();
                boxStyle.fixedHeight = 10;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(indent);
                    if (GUILayout.Button(L10n.Tr("FMOD Studio Project"), sourceButtonStyle))
                    {
                        isValidSource = SettingsEditor.BrowseForSourceProjectPath(serializedObject);
                    }
                    GUILayout.Label(L10n.Tr("If you have the complete FMOD Studio Project."), descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.Space();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(indent);
                    if (GUILayout.Button("Single Platform Build", sourceButtonStyle))
                    {
                        SettingsEditor.BrowseForSourceBankPath(serializedObject, false);
                        EditorUtils.ValidateSource(out isValidSource, out invalidMessage);
                    }
                    EditorGUILayout.LabelField(L10n.Tr("If you have the contents of the Build folder for a single platform."),
                        descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.Space();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(indent);
                    if (GUILayout.Button(L10n.Tr("Multiple Platform Build"), sourceButtonStyle))
                    {
                        SettingsEditor.BrowseForSourceBankPath(serializedObject, true);
                        EditorUtils.ValidateSource(out isValidSource, out invalidMessage);
                    }
                    EditorGUILayout.LabelField(L10n.Tr("If you have the contents of the Build folder for multiple platforms, with each platform in its own subdirectory."), descriptionStyle, GUILayout.Height(sourceButtonStyle.fixedHeight));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.Space();
            }

            if (IsStudioLinked() || invalidMessage.Length != 0)
            {
                EditorGUILayout.Space();

                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = isValidSource ? Color.green : Color.red;

                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.FlexibleSpace();

                    GUILayout.Label(isValidSource ? tickTexture : crossTexture, iconStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));

                    EditorGUILayout.Space();

                    using (new GUILayout.VerticalScope())
                    {
                        Settings settings = Settings.Instance;

                        if (settings.HasSourceProject)
                        {
                            EditorGUILayout.LabelField(L10n.Tr(String.Format("Using the FMOD Studio project at: {0}", settings.SourceBankPath)), descriptionStyle);
                        }
                        else if (settings.HasPlatforms)
                        {
                            EditorGUILayout.LabelField(L10n.Tr(isValidSource ? String.Format("Using the multiple platform build at: {0}", settings.SourceBankPath) : invalidMessage), descriptionStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(L10n.Tr(isValidSource ? String.Format("Using the single platform build at: {0}", settings.SourceBankPath) : invalidMessage), descriptionStyle);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUI.backgroundColor = oldColor;
            }

            GUILayout.FlexibleSpace();
        }

        private void ListenerPage()
        {
            EditorGUILayout.LabelField(L10n.Tr("If you do not intend to use the built in Unity audio, you can choose to replace the Audio Listener with the FMOD Studio Listener.\n"), titleLeftStyle);
            EditorGUILayout.LabelField(L10n.Tr("Adding the FMOD Studio Listener component to the main camera provides the FMOD Engine with the information it needs to play 3D events correctly."), titleLeftStyle);
            EditorGUILayout.Space();
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // Display found objects containing Unity listeners
                DisplayListeners(unityListeners, ref scroll1);

                // Show FMOD Listeners
                DisplayListeners(fmodListeners, ref scroll2);

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(!bFoundUnityListener))
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(L10n.Tr("Replace Unity Listener(s) with FMOD Audio Listener."), buttonStyle))
                    {
                        for (int i = 0; i < unityListeners.Length; i++)
                        {
                            var listener = unityListeners[i];
                            if (listener)
                            {
                                RuntimeUtils.DebugLog("[FMOD Assistant] Replacing Unity Listener with FMOD Listener on " + listener.gameObject.name);
                                if (listener.GetComponent<StudioListener>() == null)
                                {
                                    listener.gameObject.AddComponent(typeof(StudioListener));
                                }
                                DestroyImmediate(unityListeners[i]);
                                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                                Repaint();
                            }
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DisplayListeners<T>(T[] listeners, ref Vector2 scrollPos)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                bool bUnityListenerType = false;
                if (typeof(T) == typeof(AudioListener))
                {
                    bUnityListenerType = true;
                }

                if (listeners != null && listeners.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField((bUnityListenerType ? "Unity" : "FMOD") + L10n.Tr(" Listener(s) found: ") + listeners.Length, titleStyle, GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                    }

                    using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, GUILayout.ExpandWidth(true)))
                    {
                        scrollPos = scrollView.scrollPosition;
                        foreach (T l in listeners)
                        {
                            var listener = l as Component;
                            if (listener != null && GUILayout.Button(listener.gameObject.name, GUILayout.ExpandWidth(true)))
                            {
                                Selection.activeGameObject = listener.gameObject;
                                EditorGUIUtility.PingObject(listener);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField((bUnityListenerType ? "Unity" : "FMOD") + L10n.Tr(" Listener(s) found: ") + listeners.Length, titleStyle);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DisableUnityAudioPage()
        {
            EditorGUILayout.LabelField(L10n.Tr("We recommend that you disable the built-in Unity audio for all platforms, to prevent it from consuming system audio resources that the FMOD Engine needs."), titleStyle);
            GUILayout.FlexibleSpace();

            var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
            var serializedManager = new SerializedObject(audioManager);
            var prop = serializedManager.FindProperty("m_DisableAudio");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(prop.boolValue))
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(prop.boolValue ? L10n.Tr("Built in audio has been disabled") : L10n.Tr("Disable built in audio"), buttonStyle))
                    {
                        prop.boolValue = true;
                        serializedManager.ApplyModifiedProperties();
                        RuntimeUtils.DebugLog("[FMOD Assistant] Built in Unity audio has been disabled.");
                        Repaint();
                    }

                }
                GUILayout.FlexibleSpace();
            }
            pageComplete[(int)PAGES.UnityAudio] = prop.boolValue;

            GUILayout.FlexibleSpace();
        }

        private void UnitySources()
        {
            if (unityAudioSources != null && unityAudioSources.Length > 0)
            {
                EditorGUILayout.LabelField(L10n.Tr("Listed below are all the Unity Audio Sources found in the currently loaded scenes and the Assets directory.\nSelect an Audio Source and replace it with an FMOD Studio Event Emitter."), titleStyle);
                EditorGUILayout.Space();

                if (m_SimpleTreeView == null)
                {
                    if (m_TreeViewState == null)
                    {
                        m_TreeViewState = new TreeViewState();
                    }
                    m_SimpleTreeView = new SimpleTreeView(m_TreeViewState);
                }

                m_SimpleTreeView.Drawlayout();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(L10n.Tr("No Unity Audio Sources have been found!"), titleStyle);
                GUILayout.FlexibleSpace();
            }
        }

        private void SourceControl()
        {
            EditorGUILayout.LabelField(L10n.Tr("There are a number of files produced by FMOD for Unity that should be ignored by source control. Here is an example of what you should add to your source control ignore file:"), titleLeftStyle);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                ignoreFileScrollPosition = EditorGUILayout.BeginScrollView(ignoreFileScrollPosition, GUILayout.Height(200));
                EditorGUILayout.TextArea(IgnoreFileText);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.LabelField(
                "Add line ending requirements to a .gitattributes file to avoid issues:",
                titleLeftStyle
            );

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.TextArea(GitAttributesText, GUILayout.Width(568));
            }

            pageComplete[(int)PAGES.SourceControl] = true;
        }

        private void EndPage()
        {
            GUILayout.FlexibleSpace();
            bool completed = true;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 1; i < pageNames.Count - 1; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(pageNames[i], titleStyle);
                            EditorGUILayout.Space();
                            GUILayout.Label(pageComplete[i] ? tickTexture : crossTexture, iconStyle, GUILayout.ExpandWidth(false));

                            if (pageComplete[i] == false)
                            {
                                completed = false;
                            }
                        }
                        GUILayout.Space(8);
                    }
                }
                GUILayout.FlexibleSpace();
            }

            string msg = "";
            if (completed)
            {
                // All complete
                msg = L10n.Tr("FMOD for Unity has been set up successfully!");
            }
            // Essential
            else if (pageComplete[(int)PAGES.Linking])
            {
                // Partial complete (linking done)
                msg = L10n.Tr("FMOD for Unity has been partially set up.");
            }
            else
            {
                // Linking not done
                msg = L10n.Tr("FMOD for Unity has not finished being set up.\nLinking to a project or banks is required.");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(msg, titleStyle);
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(L10n.Tr(" Integration Manual "), buttonStyle))
                {
                    EditorUtils.OnlineManual();
                }


                if (GUILayout.Button(L10n.Tr(" FMOD Settings "), buttonStyle))
                {
                    EditorSettings.EditSettings();
                }
                GUILayout.Space(18);

                GUILayout.FlexibleSpace();
            }

            if (completed)
            {
                Settings.Instance.HideSetupWizard = true;
            }
        }

        private void Buttons()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                navButtonStyle = new GUIStyle("Button");
                navButtonStyle.fixedHeight = 45;
                navButtonStyle.fixedWidth = 75;

                GUILayout.FlexibleSpace();
                if (currentPage != 0)
                {
                    if (GUILayout.Button(L10n.Tr("Back"), navButtonStyle))
                    {
                        if (currentPage != 0)
                        {
                            currentPage--;
                        }
                    }
                }

                string button2Text = "Next";
                if (currentPage == 0) button2Text = L10n.Tr("Start");
                else if (currentPage == PAGES.End) button2Text = L10n.Tr("Close");
                else button2Text = L10n.Tr("Next");

                if (GUILayout.Button(button2Text, navButtonStyle))
                {
                    if (currentPage == PAGES.End)
                    {
                        this.Close();
                    }
                    currentPage++;
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void StagingUpdatePage()
        {
            GUILayout.Space(25);

            string message = string.Format(L10n.Tr("Welcome to FMOD for Unity {0}."),
                EditorUtils.VersionString(FMOD.VERSION.number));

            EditorGUILayout.LabelField(message, titleStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(
                L10n.Tr("To complete the installation, we need to update the FMOD native libraries.\n") +
                L10n.Tr("This involves a few steps:"), titleStyle);

            EditorGUILayout.Space();

            float nameWidth = 200;

            using (new GUILayout.VerticalScope(columnStyle))
            {

                foreach (StagingSystem.UpdateStep step in StagingSystem.UpdateSteps)
                {
                    bool complete = step.Stage < nextStagingStep.Stage;

                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = complete ? Color.green : Color.yellow;

                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label(complete ? tickTexture : crossTexture, iconStyle);

                        EditorGUILayout.LabelField(step.Name, titleLeftStyle, GUILayout.Width(nameWidth));
                        EditorGUILayout.LabelField(step.Description, descriptionStyle);
                    }

                    GUI.backgroundColor = oldColor;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(L10n.Tr("Next step:"), titleStyle);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(nextStagingStep.Name, buttonStyle, GUILayout.ExpandWidth(false)))
                    {
                        EditorApplication.delayCall += DoNextStagingStep;
                    }

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space();

                using (var scope = new EditorGUILayout.ScrollViewScope(stagingDetailsScroll))
                {
                    stagingDetailsScroll = scope.scrollPosition;
                    GUIStyle longDescStyle = descriptionStyle;
                    longDescStyle.fixedWidth = 0;
                    EditorGUILayout.LabelField(nextStagingStep.Details, longDescStyle);
                }
            }
        }
    }

    public class SimpleTreeView : TreeView
    {
        private const float BodyHeight = 200;

        public SimpleTreeView(TreeViewState state) : base(state)
        {
            Reload();
            Repaint();
            ExpandAll();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            return !(item is AudioSourceItem);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem);
                GameObject go = null;
                if (item.hasChildren)
                {
                    if (item is ParentItem)
                    {
                        go = ((ParentItem)item).gameObject;
                    }
                }
                else
                {
                    go = ((ParentItem)((AudioSourceItem)item).parent).gameObject;
                }
                Selection.activeGameObject = go;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem (-1, -1);

            CreateItems(root, Resources.FindObjectsOfTypeAll<AudioSource>());
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        private class AudioSourceItem : TreeViewItem
        {
            const string audioIcon = "AudioSource Icon";
            public AudioSourceItem(AudioSource source) : base(source.GetHashCode())
            {
                displayName = (source.clip ? source.clip.name : "None");
                icon = (Texture2D)EditorGUIUtility.IconContent(audioIcon).image;
            }
        }

        private class ParentItem : TreeViewItem
        {
            public GameObject gameObject;
            const string goIcon = "GameObject Icon";
            const string prefabIcon = "Prefab Icon";
            const string prefabModelIcon = "PrefabModel Icon";
            const string prefabVariantIcon = "PrefabVariant Icon";

            public ParentItem(GameObject go) : base(go.GetHashCode(), 0, go.name)
            {
                gameObject = go;
                var foundAudio = gameObject.GetComponents<AudioSource>();
                for (int i = 0; i < foundAudio.Length; i++)
                {
                    AddChild(new AudioSourceItem(foundAudio[i]));
                }

                switch (PrefabUtility.GetPrefabAssetType(go))
                {
                    case PrefabAssetType.NotAPrefab:
                    icon = (Texture2D)EditorGUIUtility.IconContent(goIcon).image;
                        break;
                    case PrefabAssetType.Regular:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabIcon).image;
                        break;
                    case PrefabAssetType.Model:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabModelIcon).image;
                        break;
                    case PrefabAssetType.Variant:
                    icon = (Texture2D)EditorGUIUtility.IconContent(prefabVariantIcon).image;
                        break;
                }
            }
        }

        private class SceneItem : TreeViewItem
        {
            public Scene m_scene;
            const string sceneIcon = "SceneAsset Icon";
            const string folderIcon = "Folder Icon";

            public SceneItem(Scene scene) : base (scene.GetHashCode())
            {
                m_scene = scene;
                if (m_scene.IsValid())
                {
                    displayName = m_scene.name;
                    icon = (Texture2D)EditorGUIUtility.IconContent(sceneIcon).image;
                }
                else
                {
                    displayName = "Assets";
                    icon = (Texture2D)EditorGUIUtility.IconContent(folderIcon).image;
                }
            }
        }

        private void CreateItems(TreeViewItem root, AudioSource[] audioSources)
        {
            for(int i = 0; i < audioSources.Length; i++)
            {
                AudioSource audioSource = audioSources[i];

                GameObject obj = audioSource.gameObject;
                var sourceItem = FindItem(obj.GetHashCode(), root);
                if (sourceItem == null)
                {
                    List<GameObject> gameObjects = new List<GameObject>();
                    gameObjects.Add(obj);
                    while (obj.transform.parent != null)
                    {
                        obj = obj.transform.parent.gameObject;
                        gameObjects.Add(obj);
                    }
                    gameObjects.Reverse();

                    var parentItem = FindItem(obj.scene.GetHashCode(), root);
                    if (parentItem == null)
                    {
                        parentItem = new SceneItem(obj.scene);
                        root.AddChild(parentItem);
                    }

                    foreach (var go in gameObjects)
                    {
                        var objItem = FindItem(go.GetHashCode(), root);
                        if (objItem == null)
                        {
                            objItem = new ParentItem(go);
                            parentItem.AddChild(objItem);
                        }
                        parentItem = objItem;
                    }
                }
            }
        }

        public void Drawlayout()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, BodyHeight);
            rect = EditorGUI.IndentedRect(rect);

            OnGUI(rect);
            Toolbar();
        }

        public void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                if (GUILayout.Button(L10n.Tr("Expand All"), style))
                {
                    ExpandAll();
                }

                if (GUILayout.Button(L10n.Tr("Collapse All"), style))
                {
                    CollapseAll();
                }
            }
        }
    }
}
