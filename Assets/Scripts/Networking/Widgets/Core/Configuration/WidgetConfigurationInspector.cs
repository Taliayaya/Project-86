#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Widgets.Editor
{
    [CustomEditor(typeof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration))]
    internal class WidgetConfigurationInspector : UnityEditor.Editor
    {
        const string k_PackageId = "com.unity.services.vivox";
        const string k_VivoxWarningText = "Vivox SDK is required to enable voice or text chat in your session.";
        static bool IsVivoxPackageInstalled => IsInstalled(k_PackageId);
        AddRequest m_Request;
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var iterator = serializedObject.GetIterator();
            var drawNextElement = true;
            
            Dictionary<string, PropertyField> propertyFieldMapping = new();
            
            // Draw the default hierarchy from WidgetConfiguration until Settings
            while (iterator.NextVisible(drawNextElement))
            {
                var propertyField = new PropertyField(iterator);
                
                propertyFieldMapping.Add(iterator.name, propertyField);
                
                root.Add(propertyField);
                if (iterator.name == nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.MaxPlayers))
                    break;
                drawNextElement = false;
            }

            // Draw the header for Vivox section
            var vivoxHeader = new Label("Vivox Settings");
            vivoxHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            vivoxHeader.style.marginLeft = 2;
            vivoxHeader.style.marginTop = 13;
            vivoxHeader.style.marginBottom = 4;
            root.Add(vivoxHeader);

            var helpBox = new HelpBox(k_VivoxWarningText, HelpBoxMessageType.Info);

            var installButton = new Button();
            installButton.text = "Install Vivox Package";
            installButton.clicked += () =>
            {
                InstallVivoxPackage(installButton);
            };

            if (IsVivoxPackageInstalled)
            {
                helpBox.style.display = DisplayStyle.None;
            }

            helpBox.Add(installButton);
            root.Add(helpBox);

            // Draw the rest 
            while (iterator.NextVisible(true))
            {
                var propertyField = new PropertyField(iterator);

                if (!IsVivoxPackageInstalled)
                {
                    propertyField.SetEnabled(false);
                }

                propertyFieldMapping.Add(iterator.name, propertyField);
                
                root.Add(propertyField);
            }

            SetupPropertyTracking(root, propertyFieldMapping);
            
            return root;
        }

        void SetupPropertyTracking(VisualElement root, Dictionary<string, PropertyField> propertyFieldMapping)
        {
            var connectionTypeProperty = serializedObject.FindProperty(nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ConnectionType));
            var networkModeProperty = serializedObject.FindProperty(nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ConnectionMode));
            
            // Call it once to set the initial visibility.
            SetDirectConnectionDependencyVisibility((global::Networking.Widgets.Core.Configuration.ConnectionType)connectionTypeProperty.enumValueIndex, (global::Networking.Widgets.Core.Configuration.ConnectionMode)networkModeProperty.enumValueIndex, propertyFieldMapping);
            
            root.TrackPropertyValue(connectionTypeProperty, property =>
            {
                SetDirectConnectionDependencyVisibility((global::Networking.Widgets.Core.Configuration.ConnectionType)property.enumValueIndex, (global::Networking.Widgets.Core.Configuration.ConnectionMode)networkModeProperty.enumValueIndex, propertyFieldMapping);
            });
            root.TrackPropertyValue(networkModeProperty, property =>
            {
                SetDirectConnectionDependencyVisibility((global::Networking.Widgets.Core.Configuration.ConnectionType)connectionTypeProperty.enumValueIndex, (global::Networking.Widgets.Core.Configuration.ConnectionMode)property.enumValueIndex, propertyFieldMapping);
            });
        }

        static void SetDirectConnectionDependencyVisibility(global::Networking.Widgets.Core.Configuration.ConnectionType connectionType, global::Networking.Widgets.Core.Configuration.ConnectionMode connectionMode, Dictionary<string, PropertyField> propertyFieldMapping)
        {
            if (connectionType == global::Networking.Widgets.Core.Configuration.ConnectionType.Direct)
            {
                SetNetworkTypeDependencyVisibility(connectionMode, propertyFieldMapping);
                return;
            }
            
            var networkTypeElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ConnectionMode)];
            var listenIpAddressElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ListenIpAddress)];
            var publishIpAddressElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.PublishIpAddress)];
            var portElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.Port)];
        
            SetPropertyFieldVisibility(networkTypeElement, true);
            SetPropertyFieldVisibility(listenIpAddressElement, true);
            SetPropertyFieldVisibility(publishIpAddressElement, true);
            SetPropertyFieldVisibility(portElement, true);
        }
        
        static void SetNetworkTypeDependencyVisibility(global::Networking.Widgets.Core.Configuration.ConnectionMode connectionMode, Dictionary<string, PropertyField> propertyFieldMapping)
        {
            var networkTypeElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ConnectionMode)];
            var listenIpAddressElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.ListenIpAddress)];
            var publishIpAddressElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.PublishIpAddress)];
            var portElement = propertyFieldMapping[nameof(global::Networking.Widgets.Core.Configuration.WidgetConfiguration.Port)];
            
            SetPropertyFieldVisibility(networkTypeElement, false);
            
            if (connectionMode == global::Networking.Widgets.Core.Configuration.ConnectionMode.Listen)
            {
                SetPropertyFieldVisibility(listenIpAddressElement, false);
                SetPropertyFieldVisibility(publishIpAddressElement, true);
            }
            else
            {
                SetPropertyFieldVisibility(listenIpAddressElement, false);
                SetPropertyFieldVisibility(publishIpAddressElement, false);
            }
            
            SetPropertyFieldVisibility(portElement, false);
        }

        static void SetPropertyFieldVisibility(VisualElement element, bool isHidden)
        {
            element.style.display = isHidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void InstallVivoxPackage(Button button)
        {
            button.SetEnabled(false);
            m_Request = Client.Add(k_PackageId);
            EditorApplication.update += Progress;
        }

        void Progress()
        {
            if (m_Request.IsCompleted)
            {
                if (m_Request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + m_Request.Result.packageId);
                else if (m_Request.Status >= StatusCode.Failure)
                    Debug.Log(m_Request.Error.message);

                EditorApplication.update -= Progress;
            }
        }

        static bool IsInstalled(string packageId) => UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageId) != null;
    }
}

#endif