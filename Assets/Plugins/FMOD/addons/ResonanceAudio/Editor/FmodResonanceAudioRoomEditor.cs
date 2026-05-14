// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using UnityEditor;
using System.Collections;
using static FMODUnityResonance.FmodResonanceAudioRoom;

namespace FMODUnityResonance
{
    /// A custom editor for properties on the FmodResonanceAudioRoom script. This appears in the
    /// Inspector window of a FmodResonanceAudioRoom object.
    [CustomEditor(typeof(FmodResonanceAudioRoom))]
    [CanEditMultipleObjects]
    public class FmodResonanceAudioRoomEditor : Editor
    {
        private SerializedProperty leftWall = null;
        private SerializedProperty rightWall = null;
        private SerializedProperty floor = null;
        private SerializedProperty ceiling = null;
        private SerializedProperty backWall = null;
        private SerializedProperty frontWall = null;
        private SerializedProperty reflectivity = null;
        private SerializedProperty reverbGainDb = null;
        private SerializedProperty reverbBrightness = null;
        private SerializedProperty reverbTime = null;
        private SerializedProperty size = null;

        private GUIContent surfaceMaterialsLabel;
        private GUIContent surfaceMaterialLabel;
        private GUIContent reflectivityLabel;
        private GUIContent reverbGainLabel;
        private GUIContent reverbPropertiesLabel;
        private GUIContent reverbBrightnessLabel;
        private GUIContent reverbTimeLabel;
        private GUIContent sizeLabel;

        private static readonly string[] SurfaceMaterialDisplay = new string[] {
            L10n.Tr("Transparent"),
            L10n.Tr("Acoustic Ceiling Tiles"),
            L10n.Tr("Brick Bare"),
            L10n.Tr("Brick Painted"),
            L10n.Tr("Concrete Block Coarse"),
            L10n.Tr("Concrete Block Painted"),
            L10n.Tr("Curtain Heavy"),
            L10n.Tr("Fiberglass Insulation"),
            L10n.Tr("Glass Thin"),
            L10n.Tr("Glass Thick"),
            L10n.Tr("Grass"),
            L10n.Tr("Linoleum On Concrete"),
            L10n.Tr("Marble"),
            L10n.Tr("Metal"),
            L10n.Tr("Parquet On Concrete"),
            L10n.Tr("Plaster Rough"),
            L10n.Tr("Plaster Smooth"),
            L10n.Tr("Plywood Panel"),
            L10n.Tr("Polished Concrete Or Tile"),
            L10n.Tr("Sheetrock"),
            L10n.Tr("Water Or Ice Surface"),
            L10n.Tr("Wood Ceiling"),
            L10n.Tr("Wood Panel"),
        };

        private static readonly int[] SurfaceMaterialValues = new int[] {
            (int)SurfaceMaterial.Transparent,
            (int)SurfaceMaterial.AcousticCeilingTiles,
            (int)SurfaceMaterial.BrickBare,
            (int)SurfaceMaterial.BrickPainted,
            (int)SurfaceMaterial.ConcreteBlockCoarse,
            (int)SurfaceMaterial.ConcreteBlockPainted,
            (int)SurfaceMaterial.CurtainHeavy,
            (int)SurfaceMaterial.FiberglassInsulation,
            (int)SurfaceMaterial.GlassThin,
            (int)SurfaceMaterial.GlassThick,
            (int)SurfaceMaterial.Grass,
            (int)SurfaceMaterial.LinoleumOnConcrete,
            (int)SurfaceMaterial.Marble,
            (int)SurfaceMaterial.Metal,
            (int)SurfaceMaterial.ParquetOnConcrete,
            (int)SurfaceMaterial.PlasterRough,
            (int)SurfaceMaterial.PlasterSmooth,
            (int)SurfaceMaterial.PlywoodPanel,
            (int)SurfaceMaterial.PolishedConcreteOrTile,
            (int)SurfaceMaterial.Sheetrock,
            (int)SurfaceMaterial.WaterOrIceSurface,
            (int)SurfaceMaterial.WoodCeiling,
            (int)SurfaceMaterial.WoodPanel,
        };

        private void OnEnable()
        {
            surfaceMaterialsLabel = new GUIContent(L10n.Tr("Surface Materials"),
            L10n.Tr("Room surface materials to calculate the acoustic properties of the room."));
            surfaceMaterialLabel = new GUIContent(L10n.Tr("Surface Material"),
            L10n.Tr("Surface material used to calculate the acoustic properties of the room."));
            reflectivityLabel = new GUIContent(L10n.Tr("Reflectivity"),
            L10n.Tr("Adjusts what proportion of the direct sound is reflected back by each surface, after an appropriate delay. Reverberation is unaffected by this setting."));
            reverbGainLabel = new GUIContent(L10n.Tr("Gain (dB)"),
            L10n.Tr("Applies a gain adjustment to the reverberation in the room. The default value will leave reverb unaffected."));
            reverbPropertiesLabel = new GUIContent(L10n.Tr("Reverb Properties"),
            L10n.Tr("Parameters to adjust the reverb properties of the room."));
            reverbBrightnessLabel = new GUIContent(L10n.Tr("Brightness"),
            L10n.Tr("Adjusts the balance between high and low frequencies in the reverb."));
            reverbTimeLabel = new GUIContent(L10n.Tr("Time"),
            L10n.Tr("Adjusts the overall duration of the reverb by a positive scaling factor."));
            sizeLabel = new GUIContent(L10n.Tr("Size"), L10n.Tr("Sets the room dimensions."));
            leftWall = serializedObject.FindProperty("LeftWall");
            rightWall = serializedObject.FindProperty("RightWall");
            floor = serializedObject.FindProperty("Floor");
            ceiling = serializedObject.FindProperty("Ceiling");
            backWall = serializedObject.FindProperty("BackWall");
            frontWall = serializedObject.FindProperty("FrontWall");
            reflectivity = serializedObject.FindProperty("Reflectivity");
            reverbGainDb = serializedObject.FindProperty("ReverbGainDb");
            reverbBrightness = serializedObject.FindProperty("ReverbBrightness");
            reverbTime = serializedObject.FindProperty("ReverbTime");
            size = serializedObject.FindProperty("Size");
        }

        /// @cond
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Add clickable script field, as would have been provided by DrawDefaultInspector()
            MonoScript script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField(surfaceMaterialsLabel);
            ++EditorGUI.indentLevel;
            DrawSurfaceMaterial(leftWall, L10n.Tr("Left Wall"));
            DrawSurfaceMaterial(rightWall, L10n.Tr("Right Wall"));
            DrawSurfaceMaterial(floor, L10n.Tr("Floor"));
            DrawSurfaceMaterial(ceiling, L10n.Tr("Ceiling"));
            DrawSurfaceMaterial(backWall, L10n.Tr("Back Wall"));
            DrawSurfaceMaterial(frontWall, L10n.Tr("Front Wall"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();

            EditorGUILayout.Slider(reflectivity, 0.0f, FmodResonanceAudio.MaxReflectivity, reflectivityLabel);

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(reverbPropertiesLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.Slider(reverbGainDb, FmodResonanceAudio.MinGainDb, FmodResonanceAudio.MaxGainDb,
                                   reverbGainLabel);
            EditorGUILayout.Slider(reverbBrightness, FmodResonanceAudio.MinReverbBrightness,
                                   FmodResonanceAudio.MaxReverbBrightness, reverbBrightnessLabel);
            EditorGUILayout.Slider(reverbTime, 0.0f, FmodResonanceAudio.MaxReverbTime, reverbTimeLabel);
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(size, sizeLabel);

            serializedObject.ApplyModifiedProperties();
        }
        /// @endcond

        private void DrawSurfaceMaterial(SerializedProperty surfaceMaterial, string displayName)
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent labelContent = new GUIContent(displayName, surfaceMaterialLabel.tooltip);
            EditorGUILayout.LabelField(labelContent, GUILayout.Width(150));
            surfaceMaterial.intValue = EditorGUILayout.IntPopup(
                        surfaceMaterial.intValue, SurfaceMaterialDisplay, SurfaceMaterialValues);

            EditorGUILayout.EndHorizontal();
        }
    }
}
