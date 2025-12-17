using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    public class BankRefreshWindow : EditorWindow
    {
        private static BankRefreshWindow instance = null;

        private SerializedObject serializedSettings;
        private SerializedProperty cooldown;
        private SerializedProperty showWindow;

        private bool readyToRefreshBanks = false;
        private float closeTime = float.MaxValue;
        private string lastRefreshError = null;

        private const float CloseDelay = 5;

        public static bool IsVisible { get { return instance != null; } }

        public static bool ReadyToRefreshBanks { get { return instance == null || instance.readyToRefreshBanks; } }

        public static void ShowWindow()
        {
            if (instance == null)
            {
                instance = CreateInstance<BankRefreshWindow>();
                instance.titleContent = new GUIContent(L10n.Tr("FMOD Bank Refresh Status"));
                instance.minSize = new Vector2(400, 200);
                instance.maxSize = new Vector2(1000, 200);

                instance.ShowUtility();
            }
        }

        private void OnEnable()
        {
            serializedSettings = new SerializedObject(Settings.Instance);
            cooldown = serializedSettings.FindProperty("BankRefreshCooldown");
            showWindow = serializedSettings.FindProperty("ShowBankRefreshWindow");

            // instance is set to null when scripts are recompiled
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();

            if (BankRefresher.TimeUntilBankRefresh() != float.MaxValue)
            {
                closeTime = float.MaxValue;
            }

            if (Time.realtimeSinceStartup > closeTime)
            {
                Close();
            }
        }

        public static void HandleBankRefresh(string error)
        {
            if (error != null)
            {
                RuntimeUtils.DebugLogErrorFormat("FMOD: Bank refresh failed: {0}", error);
            }

            if (instance != null)
            {
                instance.readyToRefreshBanks = false;
                instance.lastRefreshError = error;

                if (error == null)
                {
                    instance.closeTime = Time.realtimeSinceStartup + CloseDelay;
                }
            }
        }

        private void OnGUI()
        {
            serializedSettings.Update();

            DrawStatus();

            GUILayout.FlexibleSpace();

            SettingsEditor.DisplayBankRefreshSettings(cooldown, showWindow, false);

            DrawButtons();

            serializedSettings.ApplyModifiedProperties();
        }

        private bool ConsumeEscapeKey()
        {
            if ((focusedWindow == this) && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DrawStatus()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteLargeLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle largeErrorStyle = new GUIStyle(labelStyle);
            largeErrorStyle.normal.textColor = Color.red;

            GUIStyle errorStyle = new GUIStyle(GUI.skin.box);
            errorStyle.alignment = TextAnchor.UpperLeft;
            errorStyle.wordWrap = true;
            errorStyle.normal.textColor = Color.red;

            float timeSinceFileChange = BankRefresher.TimeSinceSourceFileChange();

            if (timeSinceFileChange != float.MaxValue)
            {
                GUILayout.Label(string.Format(L10n.Tr("The FMOD source banks changed {0} ago."),
                    EditorUtils.DurationString(timeSinceFileChange)), labelStyle);

                float timeUntilBankRefresh = BankRefresher.TimeUntilBankRefresh();

                if (timeUntilBankRefresh == 0)
                {
                    GUILayout.Label(L10n.Tr("Refreshing banks now..."), labelStyle);
                    readyToRefreshBanks = true;
                }
                else if (timeUntilBankRefresh != float.MaxValue)
                {
                    if (DrawCountdown(L10n.Tr("Refreshing banks"), timeUntilBankRefresh, Settings.Instance.BankRefreshCooldown, labelStyle)
                        || ConsumeEscapeKey())
                    {
                        BankRefresher.DisableAutoRefresh();
                    }
                }
                else
                {
                    GUILayout.Label(L10n.Tr("Would you like to refresh banks?"), labelStyle);
                }
            }
            else
            {
                if (lastRefreshError == null)
                {
                    GUILayout.Label(L10n.Tr("The FMOD banks are up to date."), labelStyle);
                }
                else
                {
                    GUILayout.Label(L10n.Tr("Bank refresh failed:"), largeErrorStyle);
                    GUILayout.Box(lastRefreshError, errorStyle, GUILayout.ExpandWidth(true));
                }
            }

            if (closeTime != float.MaxValue)
            {
                float timeUntilClose = Mathf.Max(0, closeTime - Time.realtimeSinceStartup);

                if (DrawCountdown(L10n.Tr("Closing"), timeUntilClose, CloseDelay, labelStyle) || ConsumeEscapeKey())
                {
                    closeTime = float.MaxValue;
                }
            }
        }

        private static bool DrawCountdown(string text, float remainingTime, float totalTime, GUIStyle labelStyle)
        {
            GUILayout.Label(string.Format(L10n.Tr("{0} in {1}..."), text, EditorUtils.DurationString(remainingTime)), labelStyle);

            const float boxHeight = 2;

            Rect controlRect = EditorGUILayout.GetControlRect(false, boxHeight * 2);

            Rect boxRect = controlRect;
            boxRect.width *= remainingTime / totalTime;
            boxRect.x += (controlRect.width - boxRect.width) / 2;
            boxRect.height = 2;

            GUI.DrawTexture(boxRect, EditorGUIUtility.whiteTexture);

            GUIContent cancelContent = new GUIContent(L10n.Tr("Cancel"));

            controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);

            Rect buttonRect = controlRect;
            buttonRect.width = 100;
            buttonRect.x += (controlRect.width - buttonRect.width) / 2;

            return GUI.Button(buttonRect, cancelContent);
        }

        private void DrawButtons()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2);

            int buttonCount = 2;

            Rect closeRect = rect;
            closeRect.width = rect.width / buttonCount;

            Rect refreshRect = rect;
            refreshRect.xMin = closeRect.xMax;

            if (GUI.Button(closeRect, L10n.Tr("Close")))
            {
                Close();
            }

            if (GUI.Button(refreshRect, L10n.Tr("Refresh Banks Now")))
            {
                EventManager.RefreshBanks();
            }
        }
    }
}
