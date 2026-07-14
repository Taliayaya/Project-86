using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace FMODUnity
{
    [CustomEditor(typeof(Settings))]
    public class SettingsEditor : Editor
    {
        private static readonly string[] ToggleDisplay = new string[] { L10n.Tr("Disabled"), L10n.Tr("Enabled"), L10n.Tr("Development Build Only"), };

        private static readonly string[] OverlayLocations = new string[] { L10n.Tr("TopLeft"), L10n.Tr("TopCenter"), L10n.Tr("TopRight"), L10n.Tr("BottomLeft"), L10n.Tr("BottomCenter"), L10n.Tr("BottomRight"), L10n.Tr("Center"), L10n.Tr("VR") };

        private static readonly string[] FrequencyDisplay = new string[] {
            L10n.Tr("Platform Default"),
            "22.05 kHz",
            "24 kHz",
            "32 kHz",
            "44.1 kHz",
            "48 kHz"
        };

        private static readonly int[] FrequencyValues = new int[] { 0, 22050, 24000, 32000, 44100, 48000 };

        private static readonly string[] SpeakerModeDisplay = new string[] {
            L10n.Tr("Stereo"),
            L10n.Tr("Surround 5.1"),
            L10n.Tr("Surround 7.1"),
            L10n.Tr("Surround 7.1.4")
        };

        private static readonly FMOD.SPEAKERMODE[] SpeakerModeValues = new FMOD.SPEAKERMODE[] {
            FMOD.SPEAKERMODE.STEREO,
            FMOD.SPEAKERMODE._5POINT1,
            FMOD.SPEAKERMODE._7POINT1,
            FMOD.SPEAKERMODE._7POINT1POINT4
        };

        private static readonly string[] ThreadTypeDisplay = new string[] {
            L10n.Tr("Mixer"),
            L10n.Tr("Feeder"),
            L10n.Tr("Stream"),
            L10n.Tr("File"),
            L10n.Tr("Nonblocking"),
            L10n.Tr("Record"),
            L10n.Tr("Geometry"),
            L10n.Tr("Profiler"),
            L10n.Tr("Studio Update"),
            L10n.Tr("Studio Load Bank"),
            L10n.Tr("Studio Load Sample"),
            L10n.Tr("Convolution 1"),
            L10n.Tr("Convolution 2")
        };

        private bool hasBankSourceChanged = false;
        private bool hasBankTargetChanged = false;

        private bool expandThreadAffinity;
        private bool expandCodecChannels;
        private bool expandDynamicPlugins;
        private bool expandStaticPlugins;

        private static Section sExpandedSections;

        private SerializedProperty automaticEventLoading;
        private SerializedProperty automaticSampleLoading;
        private SerializedProperty bankLoadType;
        private SerializedProperty banksToLoad;
        private SerializedProperty enableMemoryTracking;
        private SerializedProperty encryptionKey;
        private SerializedProperty hasSourceProject;
        private SerializedProperty hasPlatforms;
        private SerializedProperty importType;
        private SerializedProperty loggingLevel;
        private SerializedProperty meterChannelOrdering;
        private SerializedProperty sourceBankPath;
        private SerializedProperty sourceProjectPath;
        private SerializedProperty stopEventsOutsideMaxDistance;
        private SerializedProperty enableErrorCallback;
        private SerializedProperty targetAssetPath;
        private SerializedProperty targetBankFolder;
        private SerializedProperty bankRefreshCooldown;
        private SerializedProperty showBankRefreshWindow;
        private SerializedProperty eventLinkage;
        private SerializedProperty serializeGUIDsOnly;

        [NonSerialized]
        private bool resourcesLoaded = false;

        private GUIStyle mainHeaderStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle platformHeaderStyle;
        private GUIStyle dropdownStyle;
        private GUIStyle inheritedPropertyLabelStyle;
        private GUIStyle overriddenPropertyLabelStyle;
        private GUIStyle inheritedPropertyFoldoutStyle;
        private GUIStyle overriddenPropertyFoldoutStyle;

        private GUIContent mainHeaderIcon;

        private Texture2D propertyOverrideIndicator;

        private const int THREAD_AFFINITY_CORES_PER_ROW = 8;

        private const string EditPlatformUndoMessage = "Edit FMOD Platform Properties";

        private PlatformPropertyStringListView staticPluginsView;
        private PlatformPropertyStringListView dynamicPluginsView;

        private static readonly int[] LoggingValues = new int[] {
            (int)FMOD.DEBUG_FLAGS.NONE,
            (int)FMOD.DEBUG_FLAGS.ERROR,
            (int)FMOD.DEBUG_FLAGS.WARNING,
            (int)FMOD.DEBUG_FLAGS.LOG,
        };

        private static readonly string[] LoggingDisplay = new string[] {
            L10n.Tr("None"),
            L10n.Tr("Error"),
            L10n.Tr("Warning"),
            L10n.Tr("Log"),
        };

        private static readonly int[] BankLoadTypeValues = new int[] {
            (int)BankLoadType.All,
            (int)BankLoadType.Specified,
            (int)BankLoadType.None
        };

        private static readonly string[] BankLoadTypeDisplay = new string[] {
            L10n.Tr("All"),
            L10n.Tr("Specified"),
            L10n.Tr("None"),
        };

        private static readonly int[] MeterChannelOrderingeValues = new int[] {
            (int)MeterChannelOrderingType.Standard,
            (int)MeterChannelOrderingType.SeparateLFE,
            (int)MeterChannelOrderingType.Positional
        };

        private static readonly string[] MeterChannelOrderingeDisplay = new string[] {
            L10n.Tr("Standard"),
            L10n.Tr("SeparateLFE"),
            L10n.Tr("Positional"),
        };
        private ReorderableList banksToLoadView;

        private PlatformsView platformsView;
        private TreeViewState platformTreeViewState = new TreeViewState();

        private string lastSourceBankPath;

        private static readonly GUIContent BankRefreshLabel = new GUIContent(L10n.Tr("Refresh Banks"));

        private static readonly GUIContent[] BankRefreshCooldownLabels = new GUIContent[] {
            new GUIContent(L10n.Tr("After 1 second")),
            new GUIContent(L10n.Tr("After 5 seconds")),
            new GUIContent(L10n.Tr("After 10 seconds")),
            new GUIContent(L10n.Tr("After 20 seconds")),
            new GUIContent(L10n.Tr("After 30 seconds")),
            new GUIContent(L10n.Tr("After 1 minute")),
            new GUIContent(L10n.Tr("Prompt Me")),
            new GUIContent(L10n.Tr("Manually")),
        };

        private static readonly int[] BankRefreshCooldownValues = new int[] {
            1,
            5,
            10,
            20,
            30,
            60,
            Settings.BankRefreshPrompt,
            Settings.BankRefreshManual,
        };

        internal enum SourceType : uint
        {
            FMODStudioProject = 0,
            SinglePlatformBuild,
            MultiplePlatformBuild
        }

        [Flags]
        private enum Section
        {
            BankImport = 1 << 0,
            Initialization = 1 << 1,
            Behavior = 1 << 2,
            UserInterface = 1 << 3,
            PlatformSpecific = 1 << 4,
        }

        private void OnEnable()
        {
            automaticEventLoading = serializedObject.FindProperty("AutomaticEventLoading");
            automaticSampleLoading = serializedObject.FindProperty("AutomaticSampleLoading");
            bankLoadType = serializedObject.FindProperty("BankLoadType");
            banksToLoad = serializedObject.FindProperty("BanksToLoad");
            enableMemoryTracking = serializedObject.FindProperty("EnableMemoryTracking");
            encryptionKey = serializedObject.FindProperty("EncryptionKey");
            hasSourceProject = serializedObject.FindProperty("HasSourceProject");
            hasPlatforms = serializedObject.FindProperty("HasPlatforms");
            importType = serializedObject.FindProperty("ImportType");
            loggingLevel = serializedObject.FindProperty("LoggingLevel");
            meterChannelOrdering = serializedObject.FindProperty("MeterChannelOrdering");
            sourceBankPath = serializedObject.FindProperty("sourceBankPath");
            sourceProjectPath = serializedObject.FindProperty("sourceProjectPath");
            stopEventsOutsideMaxDistance = serializedObject.FindProperty("StopEventsOutsideMaxDistance");
            enableErrorCallback = serializedObject.FindProperty("EnableErrorCallback");
            targetAssetPath = serializedObject.FindProperty("TargetAssetPath");
            targetBankFolder = serializedObject.FindProperty("TargetBankFolder");
            bankRefreshCooldown = serializedObject.FindProperty("BankRefreshCooldown");
            showBankRefreshWindow = serializedObject.FindProperty("ShowBankRefreshWindow");
            eventLinkage = serializedObject.FindProperty("EventLinkage");
            serializeGUIDsOnly = serializedObject.FindProperty("SerializeGUIDsOnly");

            platformsView = new PlatformsView(target as Settings, platformTreeViewState);

            banksToLoadView = new ReorderableList(banksToLoad);
            banksToLoadView.onAddDropdownCallback = (rect, list) => {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(L10n.Tr("Browse")), false, BrowseForBankToLoad);
                menu.AddItem(new GUIContent(L10n.Tr("Add All")), false, AddAllBanksToLoad);

                menu.DropDown(rect);
            };

            staticPluginsView = new PlatformPropertyStringListView(Platform.PropertyAccessors.StaticPlugins);
            dynamicPluginsView = new PlatformPropertyStringListView(Platform.PropertyAccessors.Plugins);
            dynamicPluginsView.onListChanged += () =>
            {
                if (dynamicPluginsView.platform is PlatformPlayInEditor)
                {
                    EditorUtils.RecreateSystem();
                }
            };

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            platformsView.ForceReload();

            RefreshBanks();

            Repaint();
        }

        private void AffirmResources()
        {
            if (!resourcesLoaded)
            {
                resourcesLoaded = true;

                mainHeaderStyle = new GUIStyle(EditorStyles.label) {
                    fontStyle = FontStyle.Bold,
                    fontSize = 18,
                };
                mainHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

                sectionHeaderStyle = new GUIStyle(GUI.skin.FindStyle("Foldout")) {
                    fontStyle = FontStyle.Bold,
                };

                platformHeaderStyle = new GUIStyle(GUI.skin.label) {
                    richText = true,
                };

                dropdownStyle = new GUIStyle(GUI.skin.FindStyle("dropdownButton"));
                dropdownStyle.fixedHeight = 0;

                inheritedPropertyLabelStyle = GUI.skin.label;

                overriddenPropertyLabelStyle = new GUIStyle(inheritedPropertyLabelStyle) {
                    fontStyle = FontStyle.Bold,
                };

                inheritedPropertyFoldoutStyle = EditorStyles.foldout;

                overriddenPropertyFoldoutStyle = new GUIStyle(inheritedPropertyFoldoutStyle) {
                    fontStyle = FontStyle.Bold,
                };

                mainHeaderIcon = new GUIContent(EditorUtils.LoadImage("StudioIcon.png"));

                propertyOverrideIndicator = new Texture2D(2, 1);

                Color darkBlue;
                ColorUtility.TryParseHtmlString("#1974a5", out darkBlue);

                Color blue;
                ColorUtility.TryParseHtmlString("#0f81be", out blue);

                propertyOverrideIndicator.SetPixel(0, 0, darkBlue);
                propertyOverrideIndicator.SetPixel(1, 0, blue);

                propertyOverrideIndicator.Apply();
            }
        }

        private Rect DrawPlatformPropertyLabel(string label, Platform platform,
            params Platform.PropertyOverrideControl[] properties)
        {
            PlatformPropertyLabelData data;
            PreparePlatformPropertyLabel(platform, properties, out data);

            GUI.Label(data.labelRect, label, data.overridden ? overriddenPropertyLabelStyle : inheritedPropertyLabelStyle);

            DecoratePlatformPropertyLabel(data, platform, properties);

            return data.remainderRect;
        }

        private Rect DrawPlatformPropertyFoldout(string label, ref bool expand, Platform platform,
            params Platform.PropertyOverrideControl[] properties)
        {
            PlatformPropertyLabelData data;
            PreparePlatformPropertyLabel(platform, properties, out data);

            using (new NoIndentScope())
            {
                expand = EditorGUI.Foldout(data.labelRect, expand, label, true,
                    data.overridden ? overriddenPropertyFoldoutStyle : inheritedPropertyFoldoutStyle);
            }

            DecoratePlatformPropertyLabel(data, platform, properties);

            return data.remainderRect;
        }

        private struct PlatformPropertyLabelData
        {
            public bool hasParent;
            public bool overridden;
            public Rect labelRect;
            public Rect remainderRect;
        }

        private void PreparePlatformPropertyLabel(Platform platform, Platform.PropertyOverrideControl[] properties,
            out PlatformPropertyLabelData data)
        {
            AffirmResources();

            Rect rect = EditorGUILayout.GetControlRect();

            data.hasParent = (platform.Parent != null || platform is PlatformPlayInEditor);
            data.overridden = data.hasParent && properties.Any(p => p.HasValue(platform));
            data.labelRect = LabelRect(rect);
            data.remainderRect = new Rect(rect) { xMin = data.labelRect.xMax };
        }

        private void DecoratePlatformPropertyLabel(PlatformPropertyLabelData data, Platform platform,
            Platform.PropertyOverrideControl[] properties)
        {
            if (data.hasParent)
            {
                if (data.overridden)
                {
                    Rect indicatorRect = new Rect(data.labelRect) { x = 1, width = 2 };
                    GUI.DrawTexture(indicatorRect, propertyOverrideIndicator);
                }

                if (Event.current.type == EventType.MouseUp
                    && Event.current.button == 1
                    && data.labelRect.Contains(Event.current.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();

                    GUIContent revertContent = new GUIContent(L10n.Tr("Revert"));

                    if (data.overridden)
                    {
                        menu.AddItem(revertContent, false, () => {
                            Undo.RecordObject(platform, L10n.Tr("Revert FMOD Platform Properties"));

                            foreach (Platform.PropertyOverrideControl property in properties)
                            {
                                property.Clear(platform);
                            }
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(revertContent);
                    }

                    menu.ShowAsContext();
                }
            }
        }

        private static int DrawPopup(Rect position, int selectedIndex, string[] displayedOptions)
        {
            using (new NoIndentScope())
            {
                return EditorGUI.Popup(position, selectedIndex, displayedOptions);
            }
        }

        private void DisplayTriStateBool(string label, Platform platform, Platform.PropertyAccessor<TriStateBool> property)
        {
            Rect rect = DrawPlatformPropertyLabel(label, platform, property);

            EditorGUI.BeginChangeCheck();

            int next = DrawPopup(rect, (int)property.Get(platform), ToggleDisplay);

            if (EditorGUI.EndChangeCheck())
            {
                property.Set(platform, (TriStateBool)next);
            }
        }

        private void DisplayPositionRect(string label, Platform platform, Platform.PropertyAccessor<ScreenPosition> property)
        {
            Rect rect = DrawPlatformPropertyLabel(label, platform, property);
            EditorGUI.BeginChangeCheck();
            int next = DrawPopup(rect, (int)property.Get(platform), OverlayLocations);
            if (EditorGUI.EndChangeCheck())
            {
                property.Set(platform, (ScreenPosition)next);
            }
        }

        private void DisplayDebugFontSelection(string label, Platform platform, Platform.PropertyAccessor<int> property)
        {
            Rect rect = DrawPlatformPropertyLabel(label, platform, property);
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.IntField(rect, property.Get(platform));
            if (EditorGUI.EndChangeCheck())
            {
                property.Set(platform, Mathf.Min(next, 20));
            }
        }

        private void DisplayOutputMode(string label, Platform platform)
        {
            if (platform.ValidOutputTypes != null)
            {
                string[] valuesChild = new string[platform.ValidOutputTypes.Length + 3];
                string[] valuesChildEnum = new string[platform.ValidOutputTypes.Length + 3];
                valuesChild[0] = string.Format(L10n.Tr("Auto"));
                valuesChild[1] = string.Format(L10n.Tr("No Sound"));
                valuesChild[2] = string.Format(L10n.Tr("Wav Writer"));
                valuesChildEnum[0] = Enum.GetName(typeof(FMOD.OUTPUTTYPE), FMOD.OUTPUTTYPE.AUTODETECT);
                valuesChildEnum[1] = Enum.GetName(typeof(FMOD.OUTPUTTYPE), FMOD.OUTPUTTYPE.NOSOUND);
                valuesChildEnum[2] = Enum.GetName(typeof(FMOD.OUTPUTTYPE), FMOD.OUTPUTTYPE.WAVWRITER);
                for (int i = 0; i < platform.ValidOutputTypes.Length; i++)
                {
                    valuesChild[i + 3] = platform.ValidOutputTypes[i].displayName;
                    valuesChildEnum[i + 3] = Enum.GetName(typeof(FMOD.OUTPUTTYPE), platform.ValidOutputTypes[i].outputType);
                }
                int currentIndex = Array.IndexOf(valuesChildEnum, platform.OutputTypeName);
                if (currentIndex == -1)
                {
                    currentIndex = 0;
                    platform.OutputTypeName = Enum.GetName(typeof(FMOD.OUTPUTTYPE), FMOD.OUTPUTTYPE.AUTODETECT);
                }
                int next = EditorGUILayout.Popup(label, currentIndex, valuesChild);
                platform.OutputTypeName = valuesChildEnum[next];
            }
        }

        private void DisplayThreadAffinity(string label, Platform platform)
        {
            if (platform.CoreCount > 0 && DisplayThreadAffinityFoldout(label, platform))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DisplayThreadAffinityGroups(platform);
                }
            }
        }

        private bool DisplayThreadAffinityFoldout(string label, Platform platform)
        {
            Rect headerRect = EditorGUILayout.GetControlRect();

            Rect labelRect = headerRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            expandThreadAffinity = EditorGUI.Foldout(labelRect, expandThreadAffinity, label, true);

            bool useDefaults = !platform.ThreadAffinitiesProperty.HasValue;

            EditorGUI.BeginChangeCheck();

            Rect toggleRect = headerRect;
            toggleRect.xMin = labelRect.xMax;

            useDefaults = GUI.Toggle(toggleRect, useDefaults, L10n.Tr("Use Defaults"));

            if (EditorGUI.EndChangeCheck())
            {
                if (useDefaults)
                {
                    platform.ThreadAffinitiesProperty.Value.Clear();
                    platform.ThreadAffinitiesProperty.HasValue = false;
                }
                else
                {
                    platform.ThreadAffinitiesProperty.Value = new List<ThreadAffinityGroup>();
                    platform.ThreadAffinitiesProperty.HasValue = true;

                    foreach (ThreadAffinityGroup group in platform.DefaultThreadAffinities)
                    {
                        platform.ThreadAffinitiesProperty.Value.Add(new ThreadAffinityGroup(group));
                    }
                }
            }

            return expandThreadAffinity;
        }

        private void DisplayThreadAffinityGroups(Platform platform)
        {
            GUIStyle affinityStyle = EditorStyles.miniButton;
            float affinityWidth = affinityStyle.CalcSize(new GUIContent("00")).x;

            GUIContent anyButtonContent = new GUIContent(L10n.Tr("Any"));
            float anyButtonWidth = affinityStyle.CalcSize(anyButtonContent).x;

            float threadsWidth = EditorGUIUtility.labelWidth;
            float affinitiesWidth = affinityWidth * THREAD_AFFINITY_CORES_PER_ROW + anyButtonWidth;

            bool editable = platform.ThreadAffinitiesProperty.HasValue;

            if (platform.ThreadAffinities.Any())
            {
                DisplayThreadAffinitiesHeader(threadsWidth, affinitiesWidth);

                using (new EditorGUI.DisabledScope(!editable))
                {
                    ThreadAffinityGroup groupToDelete = null;

                    foreach (ThreadAffinityGroup group in platform.ThreadAffinities)
                    {
                        bool delete;
                        DisplayThreadAffinityGroup(group, platform, threadsWidth, affinitiesWidth,
                            anyButtonWidth, anyButtonContent, affinityStyle, affinityWidth, out delete);

                        if (delete)
                        {
                            groupToDelete = group;
                        }
                    }

                    if (groupToDelete != null)
                    {
                        platform.ThreadAffinitiesProperty.Value.Remove(groupToDelete);
                    }
                }
            }
            else
            {
                Rect messageRect = EditorGUILayout.GetControlRect();
                messageRect.width = threadsWidth + affinitiesWidth;
                messageRect = EditorGUI.IndentedRect(messageRect);

                GUI.Label(messageRect, L10n.Tr("List is Empty"));
            }

            if (editable)
            {
                Rect addButtonRect = EditorGUILayout.GetControlRect();
                addButtonRect.width = threadsWidth + affinitiesWidth;
                addButtonRect = EditorGUI.IndentedRect(addButtonRect);

                if (GUI.Button(addButtonRect, L10n.Tr("Add")))
                {
                    platform.ThreadAffinitiesProperty.Value.Add(new ThreadAffinityGroup());
                }
            }
        }

        private void DisplayThreadAffinitiesHeader(float threadsWidth, float affinitiesWidth)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect threadsRect = controlRect;
            threadsRect.width = threadsWidth;

            threadsRect = EditorGUI.IndentedRect(threadsRect);

            GUI.Label(threadsRect, L10n.Tr("Threads"));

            Rect coresRect = controlRect;
            coresRect.x = threadsRect.xMax;
            coresRect.width = affinitiesWidth;

            GUI.Label(coresRect, L10n.Tr("Cores"));
        }

        private void DisplayThreadAffinityGroup(ThreadAffinityGroup group, Platform platform,
            float threadsWidth, float affinitiesWidth, float anyButtonWidth, GUIContent anyButtonContent,
            GUIStyle affinityStyle, float affinityWidth, out bool delete)
        {
            delete = false;

            GUIStyle editButtonStyle = EditorStyles.popup;

            GUIContent editButtonContent = new GUIContent(L10n.Tr("Edit"));
            Rect editButtonRect = new Rect(Vector2.zero, editButtonStyle.CalcSize(editButtonContent));

            float threadsHeight = group.threads.Count * EditorGUIUtility.singleLineHeight;

            bool editable = platform.ThreadAffinitiesProperty.HasValue;

            if (editable)
            {
                 threadsHeight += EditorGUIUtility.standardVerticalSpacing + editButtonRect.height;
            }

            float affinitiesHeight =
                Mathf.Ceil(platform.CoreCount / (float)THREAD_AFFINITY_CORES_PER_ROW) * EditorGUIUtility.singleLineHeight;

            Rect controlRect = EditorGUILayout.GetControlRect(false, Math.Max(threadsHeight, affinitiesHeight));

            Rect threadsRect = controlRect;
            threadsRect.width = threadsWidth;

            threadsRect = EditorGUI.IndentedRect(threadsRect);

            GUIStyle boxStyle = EditorStyles.textArea;

            GUI.Box(threadsRect, string.Empty, boxStyle);

            Rect threadRect = threadsRect;
            threadRect.height = EditorGUIUtility.singleLineHeight;

            foreach (ThreadType thread in group.threads)
            {
                GUI.Label(threadRect, ThreadTypeDisplay[(int)thread]);
                threadRect.y += threadRect.height;
            }

            if (editable)
            {
                editButtonRect.y = threadsRect.yMax - editButtonRect.height - editButtonStyle.margin.bottom;
                editButtonRect.center = new Vector2(threadsRect.center.x, editButtonRect.center.y);

                if (EditorGUI.DropdownButton(editButtonRect, editButtonContent, FocusType.Passive, editButtonStyle))
                {
                    ThreadListEditor.Show(editButtonRect, group, platform, this);
                }
            }

            Rect affinitiesRect = controlRect;
            affinitiesRect.xMin = threadsRect.xMax;
            affinitiesRect.width = affinitiesWidth;

            GUI.Box(affinitiesRect, string.Empty, boxStyle);

            Rect anyButtonRect = affinitiesRect;
            anyButtonRect.height = affinitiesHeight;
            anyButtonRect.width = anyButtonWidth;

            if (GUI.Toggle(anyButtonRect, group.affinity == ThreadAffinity.Any, anyButtonContent, affinityStyle))
            {
                group.affinity = ThreadAffinity.Any;
            }

            Rect affinityRect = affinitiesRect;
            affinityRect.x = anyButtonRect.xMax;
            affinityRect.height = EditorGUIUtility.singleLineHeight;
            affinityRect.width = affinityWidth;

            for (int i = 0; i < platform.CoreCount; ++i)
            {
                ThreadAffinity mask = (ThreadAffinity)(1U << i);

                if (GUI.Toggle(affinityRect, (group.affinity & mask) == mask, i.ToString(), affinityStyle))
                {
                    group.affinity |= mask;
                }
                else
                {
                    group.affinity &= ~mask;
                }

                if (i % THREAD_AFFINITY_CORES_PER_ROW == THREAD_AFFINITY_CORES_PER_ROW - 1)
                {
                    affinityRect.x = anyButtonRect.xMax;
                    affinityRect.y += affinityRect.height;
                }
                else
                {
                    affinityRect.x += affinityRect.width;
                }
            }

            if (editable)
            {
                GUIStyle deleteButtonStyle = GUI.skin.button;
                GUIContent deleteButtonContent = new GUIContent(L10n.Tr("Delete"));

                Rect deleteButtonRect = controlRect;
                deleteButtonRect.x = affinitiesRect.xMax;
                deleteButtonRect.width = deleteButtonStyle.CalcSize(deleteButtonContent).x;

                if (GUI.Button(deleteButtonRect, deleteButtonContent, deleteButtonStyle))
                {
                    delete = true;
                }
            }
        }

        private class ThreadListEditor : EditorWindow
        {
            private ThreadAffinityGroup group;
            private Platform platform;
            private Editor parent;

            public static void Show(Rect buttonRect, ThreadAffinityGroup group, Platform platform, Editor parent)
            {
                ThreadListEditor editor = CreateInstance<ThreadListEditor>();
                editor.group = group;
                editor.platform = platform;
                editor.parent = parent;

                Rect rect = new Rect(GUIUtility.GUIToScreenPoint(buttonRect.position), buttonRect.size);

                editor.ShowAsDropDown(rect, CalculateSize());
            }

            private static GUIStyle FrameStyle { get { return GUI.skin.box; } }
            private static GUIStyle ThreadStyle { get { return EditorStyles.toggle; } }

            private static Vector2 CalculateSize()
            {
                Vector2 result = Vector2.zero;

                Array enumValues = Enum.GetValues(typeof(ThreadType));

                foreach (ThreadType thread in enumValues)
                {
                    Vector2 size = ThreadStyle.CalcSize(new GUIContent(ThreadTypeDisplay[(int)thread]));
                    result.x = Mathf.Max(result.x, size.x);
                }

                result.y = enumValues.Length * EditorGUIUtility.singleLineHeight
                    + (enumValues.Length - 1) * EditorGUIUtility.standardVerticalSpacing;

                result.x += FrameStyle.padding.horizontal;
                result.y += FrameStyle.padding.vertical;

                return result;
            }

            private void OnGUI()
            {
                Rect frameRect = new Rect(0, 0, position.width, position.height);

                GUI.Box(frameRect, string.Empty, FrameStyle);

                Rect threadRect = FrameStyle.padding.Remove(frameRect);
                threadRect.height = EditorGUIUtility.singleLineHeight;

                foreach (ThreadType thread in Enum.GetValues(typeof(ThreadType)))
                {
                    EditorGUI.BeginChangeCheck();

                    bool include = EditorGUI.ToggleLeft(threadRect, ThreadTypeDisplay[(int)thread], group.threads.Contains(thread));

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(platform,L10n.Tr(EditPlatformUndoMessage));

                        if (include)
                        {
                            // Make sure each thread is only in one group
                            foreach (ThreadAffinityGroup other in platform.ThreadAffinities)
                            {
                                other.threads.Remove(thread);
                            }

                            group.threads.Add(thread);
                            group.threads.Sort();
                        }
                        else
                        {
                            group.threads.Remove(thread);
                        }

                        parent.Repaint();
                    }

                    threadRect.y = threadRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }

        private void DisplayCodecChannels(string label, Platform platform)
        {
            if (platform is PlatformGroup || platform is PlatformDefault)
            {
                return;
            }

            if (DisplayCodecChannelsFoldout(label, platform))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    bool editable = platform.CodecChannelsProperty.HasValue;

                    using (new EditorGUI.DisabledScope(!editable))
                    {
                        foreach (CodecChannelCount channelCount in platform.CodecChannels)
                        {
                            EditorGUI.BeginChangeCheck();

                            int channels = EditorGUILayout.IntSlider(channelCount.format.ToString(), channelCount.channels, 0, 256);

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(platform, L10n.Tr("Edit Codec Channels"));

                                channelCount.channels = channels;
                            }
                        }
                    }
                }
            }
        }

        private bool DisplayCodecChannelsFoldout(string label, Platform platform)
        {
            Rect controlRect = EditorGUILayout.GetControlRect();

            Rect labelRect = controlRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            expandCodecChannels = EditorGUI.Foldout(labelRect, expandCodecChannels, label, true);

            bool useDefaults = !platform.CodecChannelsProperty.HasValue;

            EditorGUI.BeginChangeCheck();

            Rect toggleRect = controlRect;
            toggleRect.xMin = labelRect.xMax;

            useDefaults = GUI.Toggle(toggleRect, useDefaults, L10n.Tr("Use Defaults"));

            if (EditorGUI.EndChangeCheck())
            {
                if (useDefaults)
                {
                    platform.CodecChannelsProperty.Value = null;
                    platform.CodecChannelsProperty.HasValue = false;
                }
                else
                {
                    platform.CodecChannelsProperty.Value = new List<CodecChannelCount>();
                    platform.CodecChannelsProperty.HasValue = true;

                    foreach (CodecChannelCount channelCount in platform.DefaultCodecChannels)
                    {
                        platform.CodecChannelsProperty.Value.Add(new CodecChannelCount(channelCount));
                    }
                }
            }

            return expandCodecChannels;
        }

        private void DisplaySampleRate(string label, Platform platform)
        {
            Platform.PropertyAccessor<int> property = Platform.PropertyAccessors.SampleRate;

            Rect rect = DrawPlatformPropertyLabel(label, platform, property);

            EditorGUI.BeginChangeCheck();

            int currentIndex = Math.Max(0, Array.IndexOf(FrequencyValues, platform.SampleRate));
            int nextIndex = DrawPopup(rect, currentIndex, FrequencyDisplay);

            if (EditorGUI.EndChangeCheck())
            {
                property.Set(platform, FrequencyValues[nextIndex]);
            }
        }

        private void DisplayProjectPlatform(string label, Platform platform)
        {
            Rect rect = DrawPlatformPropertyLabel(label, platform,
                Platform.PropertyAccessors.BuildDirectory, Platform.PropertyAccessors.SpeakerMode);

            int speakerModeIndex = Math.Max(0, Array.IndexOf(SpeakerModeValues, platform.SpeakerMode));
            string speakerModeName = SpeakerModeDisplay[speakerModeIndex];

            if (GUI.Button(rect, string.Format("{0} ({1})", platform.BuildDirectory, speakerModeName)))
            {
                PopupWindow.Show(rect, new ProjectPlatformSelector(platform, this));
            }
        }

        private class ProjectPlatformSelector : PopupWindowContent
        {
            private Platform platform;
            private SettingsEditor settingsEditor;
            private string[] outputSubdirectories;

            private GUIStyle headerStyle;
            private GUIStyle toggleStyle;

            private GUIContent subdirectoryHeader = new GUIContent(L10n.Tr("Output sub-directory:"));
            private GUIContent speakerModeHeader = new GUIContent(L10n.Tr("Surround speaker mode:"));

            private const float InterColumnSpace = 25;

            private Vector2 subdirectorySize;
            private Vector2 speakerModeSize;
            private Vector2 helpButtonSize;

            private Vector2 windowSize;

            public ProjectPlatformSelector(Platform platform, SettingsEditor settingsEditor)
            {
                this.platform = platform;
                this.settingsEditor = settingsEditor;

                headerStyle = GUI.skin.label;

                toggleStyle = new GUIStyle(EditorStyles.radioButton);
                toggleStyle.margin.left = headerStyle.margin.left;

                outputSubdirectories = EditorUtils.GetBankPlatforms();

                Vector2 subdirectoryHeaderSize = headerStyle.CalcSize(subdirectoryHeader);

                subdirectorySize = ToggleGroupSize(outputSubdirectories);
                subdirectorySize.x = Math.Max(subdirectoryHeaderSize.x, subdirectorySize.x);
                subdirectorySize.y += subdirectoryHeaderSize.y + headerStyle.margin.bottom;

                Vector2 speakerModeHeaderSize = headerStyle.CalcSize(speakerModeHeader);

                speakerModeSize = ToggleGroupSize(SpeakerModeDisplay);
                speakerModeSize.x = Math.Max(speakerModeHeaderSize.x, speakerModeSize.x);
                speakerModeSize.y += speakerModeHeaderSize.y + headerStyle.margin.bottom;

                helpButtonSize = EditorUtils.GetHelpButtonSize();

                float width = headerStyle.margin.left + subdirectorySize.x + InterColumnSpace + speakerModeSize.x
                    + helpButtonSize.x;
                float height = Math.Max(subdirectorySize.y, speakerModeSize.y);

                windowSize = new Vector2(width, height);
            }

            private Vector2 ToggleGroupSize(IEnumerable<string> labels)
            {
                Vector2 totalSize = Vector2.zero;

                foreach (string label in labels)
                {
                    Vector2 size = toggleStyle.CalcSize(new GUIContent(label));

                    totalSize.x = Math.Max(totalSize.x, size.x);
                    totalSize.y += size.y + toggleStyle.margin.top;
                }

                totalSize.y += toggleStyle.margin.bottom;

                return totalSize;
            }

            public override Vector2 GetWindowSize()
            {
                return windowSize;
            }

            public override void OnGUI(Rect rect)
            {
                float y = rect.y + headerStyle.margin.top;

                Rect subdirectoryRect = new Rect(rect.x + headerStyle.margin.left, y, subdirectorySize.x, rect.height);

                using (new GUILayout.AreaScope(subdirectoryRect))
                {
                    GUILayout.Label(subdirectoryHeader, headerStyle);

                    foreach (string buildDirectory in outputSubdirectories)
                    {
                        bool selected = (platform.BuildDirectory == buildDirectory);

                        EditorGUI.BeginChangeCheck();

                        selected = GUILayout.Toggle(selected, buildDirectory, toggleStyle);

                        if (EditorGUI.EndChangeCheck() && selected)
                        {
                            Undo.RecordObject(platform, L10n.Tr("Edit FMOD Platform Settings"));

                            Platform.PropertyAccessors.BuildDirectory.Set(platform, buildDirectory);

                            // Ensure SpeakerMode is also overridden
                            Platform.PropertyAccessors.SpeakerMode.Set(platform, platform.SpeakerMode);

                            settingsEditor.Repaint();
                        }
                    }
                }

                Rect speakerModeRect = new Rect(subdirectoryRect.xMax + InterColumnSpace, y, speakerModeSize.x, rect.height);

                using (new GUILayout.AreaScope(speakerModeRect))
                {
                    GUILayout.Label(speakerModeHeader, headerStyle);

                    for (int i = 0; i < SpeakerModeValues.Length; ++i)
                    {
                        bool selected = (platform.SpeakerMode == SpeakerModeValues[i]);

                        EditorGUI.BeginChangeCheck();

                        selected = GUILayout.Toggle(selected, SpeakerModeDisplay[i], toggleStyle);

                        if (EditorGUI.EndChangeCheck() && selected)
                        {
                            Undo.RecordObject(platform, L10n.Tr("Edit FMOD Platform Settings"));

                            Platform.PropertyAccessors.SpeakerMode.Set(platform, SpeakerModeValues[i]);

                            // Ensure BuildDirectory is also overridden
                            Platform.PropertyAccessors.BuildDirectory.Set(platform, platform.BuildDirectory);

                            settingsEditor.Repaint();
                        }
                    }
                }

                Rect helpButtonRect = new Rect(speakerModeRect.xMax, y, helpButtonSize.x, helpButtonSize.y);
                EditorUtils.DrawHelpButton(helpButtonRect, () => new SimpleHelp(L10n.Tr("Select the output sub-directory and surround speaker mode that match the project platform settings in the FMOD Studio build preferences.")));
            }
        }

        private void DisplaySpeakerMode(string label, Platform platform)
        {
            Rect rect = EditorUtils.DrawHelpButtonLayout(() => new SimpleHelp(L10n.Tr("Select the speaker mode that matches the project platform settings in the FMOD Studio build preferences.")));

            Rect labelRect = LabelRect(rect);

            GUI.Label(labelRect, label);

            Rect speakerModeRect = rect;
            speakerModeRect.xMin = labelRect.xMax;

            int currentIndex = Math.Max(0, Array.IndexOf(SpeakerModeValues, platform.SpeakerMode));

            int next = DrawPopup(speakerModeRect, currentIndex, SpeakerModeDisplay);

            Platform.PropertyAccessors.SpeakerMode.Set(platform, SpeakerModeValues[next]);
        }

        private void DisplayCallbackHandler(string label, Platform platform)
        {
            Platform.PropertyAccessor<PlatformCallbackHandler> property = Platform.PropertyAccessors.CallbackHandler;

            Rect rect = DrawPlatformPropertyLabel(label, platform, property);

            using (new NoIndentScope())
            {
                EditorGUI.BeginChangeCheck();

                PlatformCallbackHandler next = EditorGUI.ObjectField(rect, property.Get(platform),
                    typeof(PlatformCallbackHandler), false) as PlatformCallbackHandler;

                if (EditorGUI.EndChangeCheck())
                {
                    property.Set(platform, next);
                }
            }
        }

        private void DisplayInt(string label, Platform platform, Platform.PropertyAccessor<int> property, int min, int max)
        {
            int currentValue = property.Get(platform);

            Rect rect = DrawPlatformPropertyLabel(label, platform, property);

            using (new NoIndentScope())
            {
                EditorGUI.BeginChangeCheck();

                int next = EditorGUI.IntSlider(rect, currentValue, min, max);

                if (EditorGUI.EndChangeCheck())
                {
                    property.Set(platform, next);
                }
            }
        }

        private void DisplayLiveUpdatePort(string label, Platform platform, Platform.PropertyAccessor<int> property)
        {
            Rect rect = DrawPlatformPropertyLabel(label, platform, property);

            GUIContent resetContent = new GUIContent(L10n.Tr("Reset"));

            Rect resetRect = rect;
            resetRect.xMin = resetRect.xMax - GUI.skin.button.CalcSize(resetContent).x;

            Rect textRect = rect;
            textRect.xMax = resetRect.xMin;

            using (new NoIndentScope())
            {
                EditorGUI.BeginChangeCheck();

                int next = EditorGUI.IntField(textRect, property.Get(platform));

                if (GUI.Button(resetRect, resetContent))
                {
                    next = 9264;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    property.Set(platform, next);
                }
            }
        }

        private void DisplayPlatform(Platform platform)
        {
            if (!platform.Active)
            {
                return;
            }

            DisplayPlatformHeader(platform);

            Undo.RecordObject(platform,L10n.Tr(EditPlatformUndoMessage));

            Settings settings = target as Settings;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                DisplayTriStateBool(L10n.Tr("Live Update"), platform, Platform.PropertyAccessors.LiveUpdate);

                if (platform.IsLiveUpdateEnabled)
                {
                    DisplayLiveUpdatePort(L10n.Tr("Live Update Port"), platform, Platform.PropertyAccessors.LiveUpdatePort);
                }

                DisplayTriStateBool(L10n.Tr("Debug Overlay"), platform, Platform.PropertyAccessors.Overlay);
                if (platform.IsOverlayEnabled)
                {
                    DisplayPositionRect(L10n.Tr("Debug Location"), platform, Platform.PropertyAccessors.OverlayPosition);
                    DisplayDebugFontSelection(L10n.Tr("Font size"), platform, Platform.PropertyAccessors.OverlayFontSize);
                }

                DisplayOutputMode(L10n.Tr("Output Mode"), platform);
                DisplaySampleRate(L10n.Tr("Sample Rate"), platform);

                if (settings.HasPlatforms)
                {
                    DisplayProjectPlatform(L10n.Tr("Project Platform"), platform);
                }
                else if (platform is PlatformDefault)
                {
                    DisplaySpeakerMode(L10n.Tr("Speaker Mode"), platform);
                }

                DisplayCallbackHandler(L10n.Tr("Callback Handler"), platform);

                DisplayInt(L10n.Tr("Virtual Channel Count"), platform, Platform.PropertyAccessors.VirtualChannelCount, 1, 2048);
                DisplayInt(L10n.Tr("Real Channel Count"), platform, Platform.PropertyAccessors.RealChannelCount, 1, 256);

                DisplayCodecChannels(L10n.Tr("Codec Counts"), platform);

                DisplayDSPBufferSettings(platform);

                string warning = null;

                BuildTargetGroup buildTargetGroup =
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
                ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);

                if (scriptingBackend != ScriptingImplementation.IL2CPP)
                {
                    warning = L10n.Tr("Only supported on the IL2CPP scripting backend");
                }

                DisplayPlugins(L10n.Tr("Static Plugins"), staticPluginsView, platform, ref expandStaticPlugins, warning);

                DisplayPlugins(L10n.Tr("Dynamic Plugins"), dynamicPluginsView, platform, ref expandDynamicPlugins);

                DisplayThreadAffinity(L10n.Tr("Thread Affinity"), platform);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private void DisplayPlatformHeader(Platform platform)
        {
            string type;

            if (platform is PlatformGroup)
            {
                type = L10n.Tr("platform group");
            }
            else if (platform.IsIntrinsic)
            {
                type = L10n.Tr("built-in platform");
            }
            else
            {
                type = L10n.Tr("platform");
            }

            if (platform.Parent != null || platform is PlatformPlayInEditor)
            {
                Platform parent;
                GUIContent labelContent;

                if (platform is PlatformPlayInEditor)
                {
                    labelContent = new GUIContent(string.Format("<b>{0}</b>: {1} " + L10n.Tr("inheriting from Unity build target: "),
                        platform.DisplayName, type));
                    parent = EditorSettings.Instance.CurrentEditorPlatform;

                    while (!parent.Active)
                    {
                        parent = parent.Parent;
                    }
                }
                else
                {
                    labelContent = new GUIContent(string.Format("<b>{0}</b>: {1} " + L10n.Tr("inheriting from"), platform.DisplayName, type));
                    parent = platform.Parent;
                }

                Rect rect = EditorGUILayout.GetControlRect();

                GUIContent buttonContent = new GUIContent(string.Format("<b>{0}</b>", parent.DisplayName));
                GUIContent iconContent = EditorGUIUtility.IconContent("UnityEditor.FindDependencies");

                Rect labelRect = LabelRect(rect);
                labelRect.width = platformHeaderStyle.CalcSize(labelContent).x;

                Rect buttonRect = rect;
                buttonRect.x = labelRect.xMax;
                buttonRect.width = platformHeaderStyle.CalcSize(buttonContent).x;

                Rect iconRect = rect;
                iconRect.x = buttonRect.xMax;
                iconRect.width = iconContent.image.width;
                iconRect.height = iconContent.image.height;
                iconRect.y += (rect.height - iconRect.height) / 2;

                buttonRect.width += iconRect.width;

                GUI.Label(labelRect, labelContent, platformHeaderStyle);

                if (GUI.Button(buttonRect, buttonContent, platformHeaderStyle))
                {
                    platformsView.SelectAndFramePlatform(parent);
                }

                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(iconRect, iconContent.image);
                }

                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
            }
            else
            {
                string text = string.Format("<b>{0}</b>: {1}", platform.DisplayName, type);
                EditorGUILayout.LabelField(text, platformHeaderStyle);
            }
        }

        private void DisplayDSPBufferSettings(Platform platform)
        {
            Rect rect = DrawPlatformPropertyLabel(L10n.Tr("DSP Buffer Settings"), platform,
                Platform.PropertyAccessors.DSPBufferLength, Platform.PropertyAccessors.DSPBufferCount);

            bool useAutoDSPBufferSettings = DisplayAutoDSPBufferSettings(rect, platform);

            if (!useAutoDSPBufferSettings)
            {
                DisplayDSPBufferFields(platform);
            }
        }

        private bool DisplayAutoDSPBufferSettings(Rect rect, Platform platform)
        {
            Platform.PropertyAccessor<int> lengthProperty = Platform.PropertyAccessors.DSPBufferLength;
            Platform.PropertyAccessor<int> countProperty = Platform.PropertyAccessors.DSPBufferCount;

            GUIStyle style = GUI.skin.toggle;

            GUIContent content = new GUIContent(L10n.Tr("Auto"));
            rect.width = style.CalcSize(content).x;

            bool useAutoDSPBufferSettings = lengthProperty.Get(platform) == 0 && countProperty.Get(platform) == 0;

            EditorGUI.BeginChangeCheck();

            useAutoDSPBufferSettings = GUI.Toggle(rect, useAutoDSPBufferSettings, content, style);

            if (EditorGUI.EndChangeCheck())
            {
                if (useAutoDSPBufferSettings)
                {
                    lengthProperty.Set(platform, 0);
                    countProperty.Set(platform, 0);

                }
                else
                {
                    // set a helpful default value (real default is 0 for auto behaviour)
                    lengthProperty.Set(platform, 512);
                    countProperty.Set(platform, 4);
                }
            }

            return useAutoDSPBufferSettings;
        }

        private void DisplayDSPBufferFields(Platform platform)
        {
            Platform.PropertyAccessor<int> lengthProperty = Platform.PropertyAccessors.DSPBufferLength;
            Platform.PropertyAccessor<int> countProperty = Platform.PropertyAccessors.DSPBufferCount;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();

                int nextLength = Mathf.Max(EditorGUILayout.IntField(L10n.Tr("DSP Buffer Length"), lengthProperty.Get(platform)), 8);
                int nextCount = Mathf.Max(EditorGUILayout.IntField(L10n.Tr("DSP Buffer Count"), countProperty.Get(platform)), 2);

                if (EditorGUI.EndChangeCheck())
                {
                    lengthProperty.Set(platform, nextLength);
                    countProperty.Set(platform, nextCount);
                }
            }
        }

        private void DisplayPlugins(string title, PlatformPropertyStringListView view, Platform platform,
            ref bool expand, string warning = null)
        {
            List<string> plugins = view.property.Get(platform);

            string fullTitle = string.Format("{0}: {1}", title, plugins.Count);

            DrawPlatformPropertyFoldout(fullTitle, ref expand, platform, view.property);

            if (expand)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (warning != null)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }

                    view.platform = platform;
                    view.DrawLayout();
                }
            }
        }

        protected override void OnHeaderGUI()
        {
            AffirmResources();

            GUIContent text = new GUIContent(L10n.Tr("FMOD Settings"));

            Vector2 textSize = mainHeaderStyle.CalcSize(text);

            Vector2 iconSize = GUI.skin.label.CalcSize(mainHeaderIcon);

            Rect rect = EditorGUILayout.GetControlRect(false, (Math.Max(textSize.y, iconSize.y)) * 1.75f);

            Rect iconRect = rect;
            iconRect.width = iconSize.x;
            iconRect.height = iconSize.y;
            iconRect.y += (rect.height - iconRect.height) / 2;

            Rect textRect = rect;
            textRect.xMin = iconRect.xMax;
            textRect.height = textSize.y;
            textRect.y += (rect.height - textRect.height) / 2;

            GUI.Label(iconRect, mainHeaderIcon);
            GUI.Label(textRect, text, mainHeaderStyle);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            string invalidSourceMessage = CheckValidSource();

            DrawImportSection(invalidSourceMessage);

            if (invalidSourceMessage != null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Separator();

            DrawInitializationSection();

            EditorGUILayout.Separator();

            DrawBehaviorSection();

            EditorGUILayout.Separator();

            DrawUserInterfaceSection();

            EditorGUILayout.Separator();

            DrawPlatforms();

            serializedObject.ApplyModifiedProperties();

            ApplyPendingActions();
        }

        private bool DrawSectionHeaderLayout(Section section, string title)
        {
            Rect rect = EditorGUILayout.GetControlRect();

            return DrawSectionHeader(rect, section, title);
        }

        private bool DrawSectionHeader(Rect rect, Section section, string title)
        {
            AffirmResources();

            bool expanded = (section & sExpandedSections) == section;

            expanded = EditorGUI.Foldout(rect, expanded, title, true, sectionHeaderStyle);

            sExpandedSections = expanded ? (sExpandedSections | section) : (sExpandedSections & ~section);

            return expanded;
        }

        private void DrawImportSection(string invalidSourceMessage)
        {
            if (DrawSectionHeaderLayout(Section.BankImport, L10n.Tr("Bank Import")))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSourceSelection(invalidSourceMessage);

                    if (invalidSourceMessage != null)
                    {
                        return;
                    }

                    DrawTargetSelection();
                }
            }
        }

        private void DrawSourceSelection(string invalidSourceMessage)
        {
            Rect popupRect = EditorUtils.DrawHelpButtonLayout(() => new SourceSelectionHelp());

            hasBankSourceChanged = false;

            SourceType sourceType = hasSourceProject.boolValue
                ? SourceType.FMODStudioProject
                : (hasPlatforms.boolValue ? SourceType.MultiplePlatformBuild : SourceType.SinglePlatformBuild);

            sourceType = (SourceType)EditorGUI.EnumPopup(popupRect, L10n.Tr("Source Type"), sourceType);

            if (sourceType == SourceType.FMODStudioProject)
            {
                string oldPath = sourceProjectPath.stringValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    string newPath = EditorGUILayout.TextField(L10n.Tr("Studio Project Path"), sourceProjectPath.stringValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newPath.EndsWith(".fspro"))
                        {
                            sourceProjectPath.stringValue = newPath;
                        }
                    }

                    if (GUILayout.Button(L10n.Tr("Browse"), GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);
                        EditorApplication.delayCall += BrowseForSourceProjectPathAndRefresh;
                    }
                }

                // Cache in settings for runtime access in play-in-editor mode
                sourceBankPath.stringValue = GetBankDirectory(serializedObject);
                hasPlatforms.boolValue = true;
                hasSourceProject.boolValue = true;

                // First time project path is set or changes, copy to streaming assets
                if (sourceProjectPath.stringValue != oldPath)
                {
                    hasBankSourceChanged = true;
                }
            }
            else if (sourceType == SourceType.SinglePlatformBuild || sourceType == SourceType.MultiplePlatformBuild)
            {
                string oldPath = sourceBankPath.stringValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(sourceBankPath, new GUIContent(L10n.Tr("Build Path")));

                    if (GUILayout.Button(L10n.Tr("Browse"), GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);
                        EditorApplication.delayCall += BrowseForSourceBankPathAndRefresh;
                    }
                }

                hasPlatforms.boolValue = (sourceType == SourceType.MultiplePlatformBuild);
                hasSourceProject.boolValue = false;

                // First time project path is set or changes, copy to streaming assets
                if (sourceBankPath.stringValue != oldPath)
                {
                    hasBankSourceChanged = true;
                }
            }

            if (invalidSourceMessage != null)
            {
                EditorGUILayout.HelpBox(invalidSourceMessage, MessageType.Error, true);
            }
        }

        private void BrowseForSourceProjectPathAndRefresh()
        {
            if (BrowseForSourceProjectPath(serializedObject))
            {
                Repaint();
            }
        }

        private void BrowseForSourceBankPathAndRefresh()
        {
            if (BrowseForSourceBankPath(serializedObject, hasPlatforms.boolValue))
            {
                Repaint();
            }
        }

        private string CheckValidSource()
        {
            bool validSource;
            string invalidMessage;
            EditorUtils.ValidateSource(out validSource, out invalidMessage);

            if (validSource)
            {
                return null;
            }
            else
            {
                sExpandedSections |= Section.BankImport;

                return invalidMessage + L10n.Tr("\n\nFor detailed setup instructions, please see the FMOD/Help/Getting Started menu item.");
            }
        }

        private class SourceSelectionHelp : HelpContent
        {
            private GUIStyle style;

            private readonly GUIContent introduction = new GUIContent(L10n.Tr("Choose how to access your FMOD Studio content:"));

            private readonly ListEntry[] listEntries = {
                new ListEntry(L10n.Tr("FMOD Studio Project"),
                    L10n.Tr("If you have the complete FMOD Studio project.")
                ),
                new ListEntry(L10n.Tr("Single Platform Build"),
                    L10n.Tr("If you have the contents of the <b>Build</b> folder for a single platform.")
                ),
                new ListEntry(L10n.Tr("Multiple Platform Build"),
                    L10n.Tr("If you have the contents of the <b>Build</b> folder for multiple platforms, with each platform in its own subdirectory.")
                ),
            };

            protected override void Prepare()
            {
                style = new GUIStyle(GUI.skin.label) {
                    richText = true,
                    wordWrap = true,
                };
            }

            private struct ListEntry
            {
                public ListEntry(string label, string description)
                {
                    this.label = new GUIContent(label);
                    this.description = new GUIContent(description);
                }

                public GUIContent label;
                public GUIContent description;
            }

            protected override Vector2 GetContentSize()
            {
                Vector2 size = new Vector2(440, 0);

                size.y += style.margin.top;
                size.y += style.CalcHeight(introduction, size.x);

                foreach (ListEntry entry in listEntries)
                {
                    size.y += style.margin.top;
                    size.y += style.CalcHeight(entry.description, size.x - EditorGUIUtility.labelWidth);
                }

                size.y += style.margin.bottom;

                return size;
            }

            protected override void DrawContent()
            {
                EditorGUILayout.LabelField(introduction, style);

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (ListEntry entry in listEntries)
                    {
                        EditorGUILayout.LabelField(entry.label, entry.description, style);
                    }
                }
            }
        }

        private static string GetBankDirectory(SerializedObject serializedObject)
        {
            var sourceProjectPath = serializedObject.FindProperty("sourceProjectPath");
            var sourceBankPath = serializedObject.FindProperty("sourceBankPath");
            var hasSourceProject = serializedObject.FindProperty("HasSourceProject");

            if (hasSourceProject.boolValue && !string.IsNullOrEmpty(sourceProjectPath.stringValue))
            {
                string projectFolder = Path.GetDirectoryName(sourceProjectPath.stringValue);
                return RuntimeUtils.GetCommonPlatformPath(Path.Combine(projectFolder, EditorUtils.BuildFolder));
            }
            else if (!string.IsNullOrEmpty(sourceBankPath.stringValue))
            {
                return RuntimeUtils.GetCommonPlatformPath(Path.GetFullPath(sourceBankPath.stringValue));
            }
            else
            {
                return null;
            }
        }

        private void DrawTargetSelection()
        {
            Settings settings = target as Settings;

            hasBankTargetChanged = false;

            string[] importTypeNames = {L10n.Tr("Streaming Asset"), L10n.Tr("Asset Bundle") };
            int importTypeIndex = importType.enumValueIndex;

            int newImportTypeIndex = EditorGUILayout.Popup(L10n.Tr("Import Type"), importTypeIndex, importTypeNames);

            if (newImportTypeIndex != importType.enumValueIndex)
            {
                bool deleteBanks = EditorUtility.DisplayDialog(
                    L10n.Tr("FMOD Bank Import Type Changed"),
                    string.Format(L10n.Tr("Do you want to delete the {0} banks in {1} "), importTypeNames[importTypeIndex], settings.TargetPath),
                    L10n.Tr("Yes"), L10n.Tr("No"));

                if (deleteBanks)
                {
                    // Delete the old banks
                    EventManager.RemoveBanks(settings.TargetPath);
                }

                hasBankTargetChanged = true;
                importType.enumValueIndex = newImportTypeIndex;
            }

            // ----- Asset Sub Directory -------------
            SerializedProperty targetSubFolder;
            string label;

            if (importType.intValue == (int)ImportType.AssetBundle)
            {
                targetSubFolder = targetAssetPath;
                label =L10n.Tr("FMOD Asset Sub Folder");
            }
            else
            {
                targetSubFolder = targetBankFolder;
                label = L10n.Tr("FMOD Bank Sub Folder");
            }

            string newSubFolder = EditorGUILayout.DelayedTextField(label, targetSubFolder.stringValue);

            if (newSubFolder != targetSubFolder.stringValue)
            {
                EventManager.RemoveBanks(settings.TargetPath);
                targetSubFolder.stringValue = newSubFolder;
                hasBankTargetChanged = true;
            }

            DisplayBankRefreshSettings(bankRefreshCooldown, showBankRefreshWindow, true);

            DrawLinkageSettings();
        }

        private void DrawBehaviorSection()
        {
            if (DrawSectionHeaderLayout(Section.Behavior, L10n.Tr("Behavior")))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(stopEventsOutsideMaxDistance,
                        new GUIContent(L10n.Tr("Stop Events Outside Max Distance")));
                }
            }
        }

        private void DrawUserInterfaceSection()
        {
            if (DrawSectionHeaderLayout(Section.UserInterface, L10n.Tr("User Interface")))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();

                    meterChannelOrdering.intValue = EditorGUILayout.IntPopup(L10n.Tr("Meter Channel Ordering"),
                    meterChannelOrdering.intValue, MeterChannelOrderingeDisplay, MeterChannelOrderingeValues);

                    if (EditorGUI.EndChangeCheck() && EventBrowser.IsOpen)
                    {
                        EditorWindow.GetWindow<EventBrowser>("FMOD Events", false).Repaint();
                    }
                }
            }
        }

        private void DrawInitializationSection()
        {
            if (DrawSectionHeaderLayout(Section.Initialization, L10n.Tr("Initialization")))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    loggingLevel.intValue = EditorGUILayout.IntPopup(L10n.Tr("Logging Level"),
                        loggingLevel.intValue, LoggingDisplay, LoggingValues);

                    EditorGUILayout.PropertyField(enableErrorCallback,
                        new GUIContent(L10n.Tr("Enable API Error Logging")));

                    EditorGUILayout.PropertyField(enableMemoryTracking, new GUIContent(L10n.Tr("Enable Memory Tracking")));

                    using (new EditorGUI.DisabledScope(importType.intValue == (int)ImportType.AssetBundle))
                    {
                        bankLoadType.intValue = EditorGUILayout.IntPopup(L10n.Tr("Load Banks"),
                        bankLoadType.intValue, BankLoadTypeDisplay, BankLoadTypeValues);

                        switch ((BankLoadType)bankLoadType.intValue)
                        {
                            case BankLoadType.All:
                                break;
                            case BankLoadType.Specified:
                                automaticEventLoading.boolValue = false;
                                DisplayBanksToLoad();
                                break;
                            case BankLoadType.None:
                                automaticEventLoading.boolValue = false;
                                break;
                            default:
                                break;
                        }

                        using (new EditorGUI.DisabledScope(bankLoadType.intValue == (int)BankLoadType.None))
                        {
                            EditorGUILayout.PropertyField(automaticSampleLoading, new GUIContent(L10n.Tr("Load Bank Sample Data")));
                        }

                        EditorGUILayout.DelayedTextField(encryptionKey, new GUIContent(L10n.Tr("Bank Encryption Key")));
                    }
                }
            }
        }

        private void DisplayBanksToLoad()
        {
            banksToLoad.isExpanded = EditorGUILayout.Foldout(banksToLoad.isExpanded, L10n.Tr("Specified Banks"), true);

            if (banksToLoad.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    banksToLoadView.DrawLayout();
                }
            }
        }

        private void BrowseForBankToLoad()
        {
            string bankDirectory = CurrentBankDirectory();
            string path = EditorUtility.OpenFilePanel(L10n.Tr("Locate Bank"), bankDirectory, "bank");

            if (!string.IsNullOrEmpty(path))
            {
                serializedObject.Update();

                path = RuntimeUtils.GetCommonPlatformPath(path);
                path = path.Replace(bankDirectory, "");
                path = Regex.Replace(path, "\\.bank$", "");

                banksToLoad.ArrayAdd(p => p.stringValue = path);

                serializedObject.ApplyModifiedProperties();

                Repaint();
            }
        }

        private void AddAllBanksToLoad()
        {
            string sourceDir = CurrentBankDirectory();
            string[] banksFound = Directory.GetFiles(sourceDir, "*.bank", SearchOption.AllDirectories);

            serializedObject.Update();

            for (int i = 0; i < banksFound.Length; i++)
            {
                string bankLongName = RuntimeUtils.GetCommonPlatformPath(Path.GetFullPath(banksFound[i]));
                string bankShortName = bankLongName.Replace(sourceDir, "");
                bankShortName = Regex.Replace(bankShortName, "\\.bank$", "");

                if (!banksToLoad.ArrayContains(p => p.stringValue == bankShortName))
                {
                    banksToLoad.ArrayAdd(p => p.stringValue = bankShortName);
                }
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private string CurrentBankDirectory()
        {
            Settings settings = target as Settings;

            string bankDirectory;

            if (settings.HasPlatforms)
            {
                bankDirectory = string.Format("{0}/{1}/",
                    settings.SourceBankPath, EditorSettings.Instance.CurrentEditorPlatform.BuildDirectory);
            }
            else
            {
                bankDirectory = settings.SourceBankPath + "/";
            }

            return RuntimeUtils.GetCommonPlatformPath(Path.GetFullPath(bankDirectory));
        }

        private void DrawPlatforms()
        {
            platformsView.ReloadIfNecessary();

            if (DrawSectionHeaderLayout(Section.PlatformSpecific, L10n.Tr("Platform Specific")))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    platformsView.DrawLayout();

                    Platform selectedPlatform = platformsView.SelectedPlatform;

                    if (selectedPlatform != null)
                    {
                        DisplayPlatform(selectedPlatform);
                    }
                }
            }
        }

        private class PlatformsView : TreeView
        {
            private const float RowPadding = 2;

            private Settings settings;

            private static UnityEditorInternal.ReorderableList.Defaults s_Defaults;

            private const float HeaderHeight = 3;
            private const float BodyHeight = 150;
            private const float FooterHeight = 13;
            private const float TotalHeight = HeaderHeight + BodyHeight + FooterHeight;

            private const float ButtonWidth = 25;
            private const float ButtonHeight = 16;
            private const float ButtonMarginTop = 0;

            private const float FooterMarginRight = 10;

            private static readonly RectOffset BodyPadding = new RectOffset(1, 2, 1, 4);
            private static readonly RectOffset FooterPadding = new RectOffset(4, 4, 0, 0);

            private static readonly Vector2 DragHandleSize = new Vector2(10, 7);
            private static readonly Vector2 DragHandlePadding = new Vector2(5, 6);

            public PlatformsView(Settings settings, TreeViewState state) : base(state)
            {
                this.settings = settings;
                rowHeight = EditorGUIUtility.singleLineHeight + RowPadding;
            }

            public Platform SelectedPlatform
            {
                get
                {
                    IList<int> selection = GetSelection();

                    if (selection.Count != 1)
                    {
                        return null;
                    }

                    PlatformItem selectedItem = FindItem(selection[0], rootItem) as PlatformItem;

                    if (selectedItem == null)
                    {
                        return null;
                    }

                    return selectedItem.platform;
                }
            }

            private static UnityEditorInternal.ReorderableList.Defaults defaultBehaviours
            {
                get
                {
                    if (s_Defaults == null)
                    {
                        s_Defaults = new UnityEditorInternal.ReorderableList.Defaults();
                    }

                    return s_Defaults;
                }
            }

            public void DrawLayout()
            {
                Rect rect = EditorGUILayout.GetControlRect(false, TotalHeight);
                rect = EditorGUI.IndentedRect(rect);

                Rect headerRect = rect;
                headerRect.height = HeaderHeight;

                Rect bodyRect = rect;
                bodyRect.y = headerRect.yMax;
                bodyRect.height = BodyHeight;

                Rect footerRect = rect;
                footerRect.xMax -= FooterMarginRight;
                footerRect.y = bodyRect.yMax;
                footerRect.height = FooterHeight;

                Rect removeRect = footerRect;
                removeRect.x = footerRect.xMax - FooterPadding.right - ButtonWidth;
                removeRect.y += ButtonMarginTop;
                removeRect.width = ButtonWidth;
                removeRect.height = ButtonHeight;

                Rect addRect = footerRect;
                addRect.x = removeRect.x - ButtonWidth;
                addRect.y += ButtonMarginTop;
                addRect.width = ButtonWidth;
                addRect.height = ButtonHeight;

                footerRect.xMin = addRect.xMin - FooterPadding.left;
                footerRect.xMax = removeRect.xMax + FooterPadding.right;

                defaultBehaviours.DrawHeaderBackground(headerRect);

                if (Event.current.type == EventType.Repaint)
                {
                    defaultBehaviours.boxBackground.Draw(bodyRect, false, false, false, false);
                }

                Rect contentRect = BodyPadding.Remove(bodyRect);

                using (new NoIndentScope())
                {
                    base.OnGUI(contentRect);
                }

                if (Event.current.type == EventType.Repaint)
                {
                    defaultBehaviours.footerBackground.Draw(footerRect, false, false, false, false);
                }

                if (GUI.Button(addRect, defaultBehaviours.iconToolbarPlusMore, defaultBehaviours.preButton))
                {
                    DoAddMenu(addRect);
                }

                using (new EditorGUI.DisabledScope(SelectedPlatform == null))
                {
                    if (GUI.Button(removeRect, defaultBehaviours.iconToolbarMinus, defaultBehaviours.preButton))
                    {
                        DeleteSelectedPlatform();
                    }
                }
            }

            private void DoAddMenu(Rect rect)
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(L10n.Tr("New Group")), false, AddGroup);

                menu.AddSeparator(string.Empty);

                IEnumerable<Platform> missingPlatforms = settings.Platforms
                    .Where(p => !p.Active)
                    .OrderBy(p => p.DisplayName, new NaturalComparer());

                foreach (Platform platform in missingPlatforms)
                {
                    menu.AddItem(new GUIContent(platform.DisplayName), false, AddPlatform, platform.Identifier);
                }

                menu.DropDown(rect);
            }

            private void AddPlatform(object data)
            {
                string identifier = data as string;

                Platform platform = settings.FindPlatform(identifier);

                const string UndoMessage = "Add FMOD Platform";

                Undo.RecordObjects(new UnityEngine.Object[] { settings, platform, platform.Parent }, L10n.Tr(UndoMessage));

                platform.DisplaySortOrder = UpdateSortOrderForChildren(platform.Parent, platform, L10n.Tr(UndoMessage));

                settings.AddPlatformProperties(platform);

                ForceReload();

                SelectAndFramePlatform(platform);
            }

            private void AddGroup()
            {
                const string UndoMessage = "Add FMOD Platform Group";

                Undo.RecordObjects(new UnityEngine.Object[] { settings, settings.DefaultPlatform }, L10n.Tr(UndoMessage));

                int sortOrder = UpdateSortOrderForChildren(settings.DefaultPlatform, null, L10n.Tr(UndoMessage));

                PlatformGroup group = EditorSettings.Instance.AddPlatformGroup(L10n.Tr("New Group"), sortOrder);

                Undo.RegisterCreatedObjectUndo(group, UndoMessage);

                ForceReload();

                SelectAndFramePlatform(group);

                // Bring up the rename UI
                DoubleClickedItem(group.Identifier.GetHashCode());
            }

            private int UpdateSortOrderForChildren(Platform platform, Platform skipChild, string undoMessage)
            {
                int sortOrder = 0;

                foreach (string childID in platform.ChildIdentifiers)
                {
                    Platform child = settings.FindPlatform(childID);

                    if (child.Active && child != skipChild)
                    {
                        Undo.RecordObject(child, undoMessage);

                        child.DisplaySortOrder = sortOrder;
                        ++sortOrder;
                    }
                }

                return sortOrder;
            }

            // Removes a platform from the inheritance hierarchy and clears its properties, thus hiding
            // it in the UI. Also destroys the platform if it is a group.
            private void DeleteSelectedPlatform()
            {
                Platform platform = SelectedPlatform;

                if (platform == null || platform == settings.DefaultPlatform || platform == settings.PlayInEditorPlatform)
                {
                    return;
                }


                const string UndoMessage = "Delete FMOD Platform";

                Undo.RecordObjects(new UnityEngine.Object[] { platform, platform.Parent, settings }, L10n.Tr(UndoMessage));

                while (platform.ChildIdentifiers.Count > 0)
                {
                    Platform child = settings.FindPlatform(platform.ChildIdentifiers[platform.ChildIdentifiers.Count - 1]);

                    SetPlatformParent(L10n.Tr(UndoMessage), settings, child, platform.Parent, (int)platform.DisplaySortOrder + 1);
                }

                if (platform is PlatformGroup)
                {
                    PlatformGroup group = platform as PlatformGroup;

                    settings.SetPlatformParent(group, null);
                    settings.RemovePlatform(group.Identifier);

                    Undo.DestroyObjectImmediate(group);
                }
                else
                {
                    platform.ClearProperties();

                    Undo.RecordObject(settings.DefaultPlatform, UndoMessage);

                    settings.SetPlatformParent(platform, settings.DefaultPlatform);
                }

                ForceReload();
            }

            public void SelectAndFramePlatform(Platform platform)
            {
                SetSelection(new List<int>() { platform.Identifier.GetHashCode() },
                    TreeViewSelectionOptions.RevealAndFrame);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    defaultBehaviours.elementBackground.Draw(args.rowRect, false, args.selected, args.selected, args.focused);

                    if (IsItemDraggable(args.item))
                    {
                        Rect dragRect = new Rect(args.rowRect.position + DragHandlePadding, DragHandleSize);

                        defaultBehaviours.draggingHandle.Draw(dragRect, false, false, false, false);
                    }

                    GUIContent labelContent = new GUIContent(args.label);

                    GUIStyle labelStyle = GUI.skin.label;

                    Rect labelRect = args.rowRect;
                    CenterRectUsingSingleLineHeight(ref labelRect);

                    labelRect.x = GetContentIndent(args.item);
                    labelRect.width = GUI.skin.label.CalcSize(labelContent).x;

                    Texture renameIcon = EditorGUIUtility.IconContent("SettingsIcon").image;

                    bool canRename = CanRename(args.item);

                    if (canRename)
                    {
                        labelContent.tooltip = L10n.Tr("Double-click to rename");
                        labelRect.width += renameIcon.width;
                    }

                    GUI.Label(labelRect, labelContent);

                    if (canRename && Event.current.type == EventType.Repaint)
                    {
                        Rect iconRect = new Rect() {
                            x = labelRect.xMax - renameIcon.width,
                            y = labelRect.yMax - labelStyle.padding.bottom - renameIcon.height,
                            width = renameIcon.width,
                            height = renameIcon.height,
                        };

                        GUI.DrawTexture(iconRect, renameIcon, ScaleMode.ScaleToFit,
                            true, 0, labelStyle.normal.textColor, 0, 0);
                    }
                }
            }

            public void ForceReload()
            {
                Reload();
                ExpandAll();
            }

            public void ReloadIfNecessary()
            {
                if (!isInitialized)
                {
                    ForceReload();
                }
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override bool CanChangeExpandedState(TreeViewItem item)
            {
                return false;
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(-1, -1);

                root.AddChild(CreateItem(settings.PlayInEditorPlatform));

                TreeViewItem defaultItem = CreateItem(settings.DefaultPlatform);
                root.AddChild(defaultItem);

                CreateItems(defaultItem, settings.DefaultPlatform.ChildIdentifiers);

                SetupDepthsFromParentsAndChildren(root);

                return root;
            }

            private class PlatformItem : TreeViewItem
            {
                public Platform platform;

                public PlatformItem(Platform platform)
                    : base(platform.Identifier.GetHashCode(), 0, platform.DisplayName)
                {
                    this.platform = platform;
                }
            }

            private void CreateItems(TreeViewItem parent, IEnumerable<string> platformIdentifiers)
            {
                foreach (string identifier in platformIdentifiers)
                {
                    Platform platform = settings.FindPlatform(identifier);

                    if (platform.Active)
                    {
                        TreeViewItem item = CreateItem(platform);
                        parent.AddChild(item);

                        CreateItems(item, platform.ChildIdentifiers);
                    }
                }
            }

            private static TreeViewItem CreateItem(Platform platform)
            {
                return new PlatformItem(platform);
            }

            protected override void DoubleClickedItem(int id)
            {
                TreeViewItem item = FindItem(id, rootItem);

                if (CanRename(item))
                {
                    BeginRename(item);
                }
            }

            protected override bool CanRename(TreeViewItem item)
            {
                PlatformItem platformItem = item as PlatformItem;
                return (platformItem != null) && (platformItem.platform is PlatformGroup);
            }

            protected override void RenameEnded(RenameEndedArgs args)
            {
                if (!args.acceptedRename || string.IsNullOrEmpty(args.newName))
                {
                    return;
                }

                PlatformItem item = FindItem(args.itemID, rootItem) as PlatformItem;

                if (item == null)
                {
                    return;
                }

                PlatformGroup group = item.platform as PlatformGroup;

                if (group == null)
                {
                    return;
                }

                // Undo.RecordObject doesn't capture PlatformGroup.displayName, maybe due to inheritance?
                // This means we need to use the SerializedObject interface instead.
                SerializedObject serializedGroup = new SerializedObject(group);
                SerializedProperty displayName = serializedGroup.FindProperty("displayName");

                displayName.stringValue = args.newName;

                serializedGroup.ApplyModifiedProperties();

                item.displayName = args.newName;
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                return IsItemDraggable(args.draggedItem);
            }

            private bool IsItemDraggable(TreeViewItem draggedItem)
            {
                PlatformItem item = draggedItem as PlatformItem;

                return (item != null) && !item.platform.IsIntrinsic;
            }

            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                PlatformItem item = FindItem(args.draggedItemIDs[0], rootItem) as PlatformItem;

                if (item != null)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { item.platform };
                    DragAndDrop.StartDrag(L10n.Tr("Change FMOD Platform Inheritance"));
                }
            }

            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                if (DragAndDrop.objectReferences.Length != 1)
                {
                    return DragAndDropVisualMode.None;
                }

                Platform draggedPlatform = DragAndDrop.objectReferences[0] as Platform;

                if (draggedPlatform == null)
                {
                    return DragAndDropVisualMode.None;
                }

                PlatformItem parentItem = args.parentItem as PlatformItem;

                if (parentItem == null)
                {
                    return DragAndDropVisualMode.None;
                }

                Platform parent = parentItem.platform;

                if (parent is PlatformPlayInEditor)
                {
                    return DragAndDropVisualMode.None;
                }

                switch (args.dragAndDropPosition)
                {
                    case DragAndDropPosition.UponItem:
                        return HandleDragOverPlatform(draggedPlatform, parent, args.performDrop);
                    case DragAndDropPosition.BetweenItems:
                        return HandleDragBetweenChildren(draggedPlatform, parent, args.insertAtIndex, args.performDrop);
                    case DragAndDropPosition.OutsideItems:
                        return DragAndDropVisualMode.Move;
                    default:
                        return DragAndDropVisualMode.None;
                }
            }

            private DragAndDropVisualMode HandleDragOverPlatform(Platform draggedPlatform, Platform parent, bool performDrop)
            {
                if (parent == draggedPlatform)
                {
                    return DragAndDropVisualMode.Move;
                }

                if (parent.InheritsFrom(draggedPlatform))
                {
                    return DragAndDropVisualMode.None;
                }

                if (performDrop)
                {
                    SetPlatformParent(L10n.Tr("Set FMOD Platform Inheritance"), settings, draggedPlatform, parent);
                    ForceReload();
                }

                return DragAndDropVisualMode.Link;
            }

            private DragAndDropVisualMode HandleDragBetweenChildren(Platform draggedPlatform, Platform parent,
                int insertAtIndex, bool performDrop)
            {
                if (parent.InheritsFrom(draggedPlatform))
                {
                    return DragAndDropVisualMode.None;
                }

                if (performDrop)
                {
                    SetPlatformParent(L10n.Tr("Set FMOD Platform Inheritance"), settings, draggedPlatform, parent, insertAtIndex);
                    ForceReload();
                }

                return DragAndDropVisualMode.Move;
            }
        }

        private class ReorderableList : UnityEditorInternal.ReorderableList
        {
            private const float ElementPadding = 2;

            public ReorderableList(SerializedProperty property)
                : base(property.serializedObject, property, true, false, true, true)
            {
                headerHeight = 3;
                elementHeight = EditorGUIUtility.singleLineHeight + ElementPadding;
                drawElementCallback = DrawElement;
            }

            public void DrawLayout()
            {
                Rect rect = EditorGUILayout.GetControlRect(false, GetHeight());
                rect = EditorGUI.IndentedRect(rect);

                DoList(rect);
            }

            private void DrawElement(Rect rect, int index, bool active, bool focused)
            {
                using (new NoIndentScope())
                {
                    rect.height -= ElementPadding;

                    EditorGUI.PropertyField(rect, serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
                }
            }
        }

        private class PlatformPropertyStringListView : UnityEditorInternal.ReorderableList
        {
            private const float ElementPadding = 2;

            public Platform platform;

            private List<string> displayList;

            public Action onListChanged;

            public PlatformPropertyStringListView(Platform.PropertyAccessor<List<string>> property)
                : base(null, typeof(string), true, false, true, true)
            {
                this.property = property;

                displayList = new List<string>();
                list = displayList;

                headerHeight = 3;
                elementHeight = EditorGUIUtility.singleLineHeight + ElementPadding;

                drawElementCallback = DrawElement;
                onAddCallback = AddElement;
                onRemoveCallback = RemoveElement;
                onReorderCallback = OnReorder;
            }

            public Platform.PropertyAccessor<List<string>> property { get; private set; }

            // We need this because ReorderableList modifies the list before calling
            // onReorderCallback, meaning we can't call AffirmOverriddenList
            // soon enough.
            public void DrawLayout()
            {
                if (IsReloadNeeded())
                {
                    displayList.Clear();
                    displayList.AddRange(property.Get(platform));
                }

                Rect rect = EditorGUILayout.GetControlRect(false, GetHeight());
                rect = EditorGUI.IndentedRect(rect);

                DoList(rect);
            }

            public bool IsReloadNeeded()
            {
                List<string> propertyList = property.Get(platform);

                if (displayList.Count != propertyList.Count)
                {
                    return true;
                }

                for (int i = 0; i < displayList.Count; ++i)
                {
                    if (displayList[i] != propertyList[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            private void DrawElement(Rect rect, int index, bool active, bool focused)
            {
                using (new NoIndentScope())
                {
                    rect.height -= ElementPadding;

                    EditorGUI.BeginChangeCheck();

                    string newValue = EditorGUI.DelayedTextField(rect, list[index] as string);

                    if (EditorGUI.EndChangeCheck())
                    {
                        displayList[index] = newValue;
                        AffirmOverriddenList()[index] = newValue;
                        onListChanged?.Invoke();
                    }
                }
            }

            private void AddElement(UnityEditorInternal.ReorderableList list)
            {
                AffirmOverriddenList().Add(string.Empty);
            }

            private void RemoveElement(UnityEditorInternal.ReorderableList list)
            {
                AffirmOverriddenList().RemoveAt(list.index);
                onListChanged?.Invoke();
            }

            private void OnReorder(UnityEditorInternal.ReorderableList list)
            {
                List<string> propertyList = AffirmOverriddenList();

                propertyList.Clear();
                propertyList.AddRange(displayList);
            }

            private List<string> AffirmOverriddenList()
            {
                if (!property.HasValue(platform))
                {
                    List<string> newList = new List<string>(property.Get(platform));

                    property.Set(platform, newList);
                }

                return property.Get(platform);
            }
        }

        // If insertAtIndex == -1, insert at the end
        private static void SetPlatformParent(string undoMessage, Settings settings, Platform child, Platform parent, int insertAtIndex = -1)
        {
            if (parent == child.Parent)
            {
                if (insertAtIndex > child.DisplaySortOrder)
                {
                    --insertAtIndex;
                }

                if (insertAtIndex == child.DisplaySortOrder)
                {
                    return;
                }
            }

            Undo.RecordObjects(new[] { child, child.Parent, parent }, undoMessage);

            int index = 0;

            for (int i = 0; i < parent.ChildIdentifiers.Count; ++i)
            {
                Platform sibling = settings.FindPlatform(parent.ChildIdentifiers[i]);

                if (sibling.Active && sibling != child)
                {
                    if (index == insertAtIndex)
                    {
                        ++index;
                    }

                    Undo.RecordObject(sibling, undoMessage);

                    sibling.DisplaySortOrder = index;
                    ++index;
                }
            }

            if (insertAtIndex == -1)
            {
                insertAtIndex = index;
            }

            child.DisplaySortOrder = insertAtIndex;

            settings.SetPlatformParent(child, parent);
        }

        private void ApplyPendingActions()
        {
            if (hasBankSourceChanged || hasBankTargetChanged)
            {
                RefreshBanks();
            }
        }

        private void RefreshBanks()
        {
            Settings settings = target as Settings;

            if (lastSourceBankPath != settings.SourceBankPath)
            {
                lastSourceBankPath = settings.SourceBankPath;
                EventManager.RefreshBanks();
            }
        }

        public static void DisplayBankRefreshSettings(SerializedProperty cooldown, SerializedProperty showWindow,
            bool inInspector)
        {
            Rect controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect labelRect;

            if (inInspector)
            {
                labelRect = LabelRect(controlRect);
            }
            else
            {
                labelRect = EditorGUI.IndentedRect(controlRect);
                labelRect.width = GUI.skin.label.CalcSize(BankRefreshLabel).x;
            }


            Rect popupRect = controlRect;
            popupRect.x = labelRect.xMax;
            popupRect.width = BankRefreshCooldownLabels.Max(l => EditorStyles.popup.CalcSize(l).x);

            using (new NoIndentScope())
            {
                GUI.Label(labelRect, BankRefreshLabel);

                cooldown.intValue = EditorGUI.IntPopup(popupRect, cooldown.intValue,
                    BankRefreshCooldownLabels, BankRefreshCooldownValues);

                if (cooldown.intValue >= 0)
                {
                    Rect toggleRect = controlRect;
                    toggleRect.xMin = popupRect.xMax + GUI.skin.toggle.margin.left;

                    showWindow.boolValue = EditorGUI.ToggleLeft(toggleRect, L10n.Tr("Show Status Window"), showWindow.boolValue);
                }
            }
        }

        private static Rect LabelRect(Rect controlRect)
        {
            Rect result = controlRect;
            result.width = EditorGUIUtility.labelWidth;
            result = EditorGUI.IndentedRect(result);

            return result;
        }

        public static bool BrowseForSourceProjectPath(SerializedObject serializedObject)
        {
            serializedObject.Update();
            var sourceProjectPath = serializedObject.FindProperty("sourceProjectPath");
            var sourceBankPath = serializedObject.FindProperty("sourceBankPath");
            var hasSourceProject = serializedObject.FindProperty("HasSourceProject");
            var hasPlatforms = serializedObject.FindProperty("HasPlatforms");

            string newPath = EditorUtility.OpenFilePanel(L10n.Tr("Locate Studio Project"), sourceProjectPath.stringValue, "fspro");

            if (string.IsNullOrEmpty(newPath))
            {
                return false;
            }
            else
            {
                hasSourceProject.boolValue = true;
                hasPlatforms.boolValue = true;
                newPath = MakePathRelative(newPath);
                sourceProjectPath.stringValue = newPath;
                sourceBankPath.stringValue = GetBankDirectory(serializedObject);
                serializedObject.ApplyModifiedProperties();
                EventManager.RefreshBanks();
                return true;
            }
        }

        public static bool BrowseForSourceBankPath(SerializedObject serializedObject, bool multiPlatform = false)
        {
            serializedObject.Update();
            var sourceBankPath = serializedObject.FindProperty("sourceBankPath");
            var hasSourceProject = serializedObject.FindProperty("HasSourceProject");
            var hasPlatforms = serializedObject.FindProperty("HasPlatforms");

            string newPath = EditorUtility.OpenFolderPanel(L10n.Tr("Locate Build Folder"), sourceBankPath.stringValue, null);

            if (string.IsNullOrEmpty(newPath))
            {
                return false;
            }
            else
            {
                hasSourceProject.boolValue = false;
                hasPlatforms.boolValue = multiPlatform;
                newPath = MakePathRelative(newPath);
                sourceBankPath.stringValue = newPath;
                serializedObject.ApplyModifiedProperties();
                EventManager.RefreshBanks();
                return true;
            }
        }

        private static string MakePathRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";
            string fullPath = Path.GetFullPath(path);
            string fullProjectPath = Path.GetFullPath(Environment.CurrentDirectory + Path.DirectorySeparatorChar);

            // If the path contains the Unity project path remove it and return the result
            if (fullPath.Contains(fullProjectPath))
            {
                fullPath = fullPath.Replace(fullProjectPath, "");
            }
            // If not, attempt to find a relative path on the same drive
            else if (Path.GetPathRoot(fullPath) == Path.GetPathRoot(fullProjectPath))
            {
                // Remove trailing slash from project path for split count simplicity
                if (fullProjectPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.CurrentCulture)) fullProjectPath = fullProjectPath.Substring(0, fullProjectPath.Length - 1);

                string[] fullPathSplit = fullPath.Split(Path.DirectorySeparatorChar);
                string[] projectPathSplit = fullProjectPath.Split(Path.DirectorySeparatorChar);
                int minNumSplits = Mathf.Min(fullPathSplit.Length, projectPathSplit.Length);
                int numCommonElements = 0;
                for (int i = 0; i < minNumSplits; i++)
                {
                    if (fullPathSplit[i] == projectPathSplit[i])
                    {
                        numCommonElements++;
                    }
                    else
                    {
                        break;
                    }
                }
                string result = "";
                int fullPathSplitLength = fullPathSplit.Length;
                for (int i = numCommonElements; i < fullPathSplitLength; i++)
                {
                    result += fullPathSplit[i];
                    if (i < fullPathSplitLength - 1)
                    {
                        result += '/';
                    }
                }

                int numAdditionalElementsInProjectPath = projectPathSplit.Length - numCommonElements;
                for (int i = 0; i < numAdditionalElementsInProjectPath; i++)
                {
                    result = "../" + result;
                }

                fullPath = result;
            }

            return fullPath.Replace(Path.DirectorySeparatorChar, '/');
        }

        void DrawLinkageSettings()
        {
            // If GUID-only serialization mode is enabled, we don't need to display the linkage setting
#if !FMOD_SERIALIZE_GUID_ONLY
            EditorGUILayout.PropertyField(eventLinkage);
#endif
            EditorGUI.BeginChangeCheck();
            // Custom GUIContent object to stop the Unity Editor from spacing "GUIDs" out
            EditorGUILayout.PropertyField(serializeGUIDsOnly, new GUIContent("Serialize GUIDs Only"));
            if (EditorGUI.EndChangeCheck())
            {
                HandleGUIDOnlySerializationToggle(serializeGUIDsOnly.boolValue);
            }
        }
        void HandleGUIDOnlySerializationToggle(bool oldValue)
        {
            string action = oldValue ? "Enable" : "Disable";
            string content =
                "You are about to " + action.ToLower() + " GUID-Only Serialization. "
                + "\n\nThe GUID-Only Serialization setting causes FMOD for Unity to not serialize event paths for EventReferences in editor. "
                + "Toggling this setting will cause Unity to refresh the Asset Database and perform a script recompilation, and may break existing event path usage in your project. "
                + "With this setting enabled, the Event Reference Updater will not be able to detect renamed events."
                + "\n\nAre you sure you want to " + action.ToLower() + " this setting?";
            if (EditorUtility.DisplayDialog(action + " GUID-Only Serialization", content, action, "Don't " + action))
            {
                NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
                if (oldValue)
                {
                    if (!defines.Contains("FMOD_SERIALIZE_GUID_ONLY"))
                    {
                        Array.Resize(ref defines, defines.Length + 1);
                        defines[defines.Length - 1] = "FMOD_SERIALIZE_GUID_ONLY";
                        PlayerSettings.SetScriptingDefineSymbols(target, defines);
                        eventLinkage.intValue = (int)EventLinkage.GUID;
                        serializedObject.ApplyModifiedProperties();
                        AssetDatabase.Refresh();
                    }
                }
                else
                {
                    if (defines.Contains("FMOD_SERIALIZE_GUID_ONLY"))
                    {
                        defines = defines.Where(d => d != "FMOD_SERIALIZE_GUID_ONLY").ToArray();
                        PlayerSettings.SetScriptingDefineSymbols(target, defines);
                        serializedObject.ApplyModifiedProperties();
                        AssetDatabase.Refresh();
                    }
                }
            }
            else
            {
                serializeGUIDsOnly.boolValue = !oldValue;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
