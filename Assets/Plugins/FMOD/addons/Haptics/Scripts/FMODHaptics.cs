#if UNITY_OPENXR_EXIST && UNITY_ANDROID
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using FMODUnity;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace FMODUnityHaptics
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = displayName,
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "FMOD",
        Desc = "OpenXR feature to enable haptics using the fmod_haptics plugin.",
        DocumentationLink = "https://fmod.com/docs/unity",
        OpenxrExtensionStrings = "XR_FB_haptic_pcm",
        Version = "1.0.0",
        FeatureId = featureId)]
#endif
    public class FMODHapticsFeature : OpenXRFeature
    {
        internal const string featureId = "com.fmod.feature.haptic";
        internal const string displayName = "FMOD: Haptics";
        internal const string pluginName = "fmod_haptics";

        private static ulong XrSession;
        private static ulong XrInstance;
        private static ulong XrAction;

        private static InputAction inputAction = null;
        private static InputAction hapticAction = null;

        private static bool configured = false;
#if UNITY_EDITOR
        protected override void GetValidationChecks(List<ValidationRule> results, BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Android)
            {
                results.Add(new ValidationRule(this)
                {
                    message = "FMOD Unity Settings require the fmod_haptics plugin.",
                    error = true,
                    checkPredicate = () => Settings.Instance.Platforms.Find(p => p.Plugins.Contains(pluginName)) != null
                    && Settings.Instance.DefaultPlatform.Plugins.Contains(pluginName)
                    && Settings.Instance.PlayInEditorPlatform.Plugins.Contains(pluginName),
                    fixIt = () =>
                    {
                        var platforms = Settings.Instance.Platforms.FindAll(p => !p.Plugins.Contains(pluginName));
                        platforms.ForEach(p =>
                        {
                            if (!p.Plugins.Contains(pluginName))
                            {
                                p.Plugins.Add(pluginName);
                            }
                        });
                    }
                });
            }
        }
#endif

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            XrInstance = xrInstance;
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            if (hapticAction != null)
            {
                hapticAction.Disable();
                hapticAction.Dispose();
                hapticAction = null;
            }

            if (inputAction != null)
            {
                inputAction.Disable();
                inputAction.Dispose();
                inputAction = null;
            }
        }

        protected override void OnSessionStateChange(int oldState, int newState)
        {
            if (newState == (int)XrSessionState.Focused)
            {
                if (inputAction != null)
                {
                    inputAction.Disable();
                    inputAction.Dispose();
                    inputAction = null;
                }

                if (!configured)
                {
                    inputAction = new InputAction(type: InputActionType.Button, binding: "<XRController>/*");
                    //Defer native initialization until input actions are bound
                    inputAction.performed += context => RefreshConfig();
                    inputAction.Enable();
                }
            }
        }

        protected override void OnSessionBegin(ulong xrSession)
        {
            XrSession = xrSession;
        }

        private void RefreshConfig()
        {
            if (!configured)
            {
                if (hapticAction != null)
                {
                    hapticAction.Dispose();
                }

                // This works for both controllers, not just the RightHand. We just need 'a' haptic action.
                hapticAction = new InputAction(type: InputActionType.PassThrough, binding: "<XRController>{RightHand}/{Haptic}");
                hapticAction.Enable();

                XrAction = GetAction(hapticAction);

                FMOD_Haptics_OpenXrFocused(XrSession, XrInstance, XrAction);
            }
            configured = true;
        }

        [DllImport("libfmod_haptics.so")]
        private static extern FMOD.RESULT FMOD_Haptics_OpenXrFocused(ulong session, ulong instance, ulong actions);
    }
}
#endif