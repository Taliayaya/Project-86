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
using UnityEngine.Serialization;
using System.Collections;

namespace FMODUnityResonance
{
    /// Resonance Audio room component that simulates environmental effects of a room with respect to
    /// the properties of the attached game object.
    [AddComponentMenu("ResonanceAudio/FmodResonanceAudioRoom")]
    public class FmodResonanceAudioRoom : MonoBehaviour
    {
        /// Material type that determines the acoustic properties of a room surface.
        public enum SurfaceMaterial
        {
            Transparent = 0,              ///< Transparent
            AcousticCeilingTiles = 1,     ///< Acoustic ceiling tiles
            BrickBare = 2,                ///< Brick, bare
            BrickPainted = 3,             ///< Brick, painted
            ConcreteBlockCoarse = 4,      ///< Concrete block, coarse
            ConcreteBlockPainted = 5,     ///< Concrete block, painted
            CurtainHeavy = 6,             ///< Curtain, heavy
            FiberglassInsulation = 7,     ///< Fiberglass insulation
            GlassThin = 8,                ///< Glass, thin
            GlassThick = 9,               ///< Glass, thick
            Grass = 10,                   ///< Grass
            LinoleumOnConcrete = 11,      ///< Linoleum on concrete
            Marble = 12,                  ///< Marble
            Metal = 13,                   ///< Galvanized sheet metal
            ParquetOnConcrete = 14,       ///< Parquet on concrete
            PlasterRough = 15,            ///< Plaster, rough
            PlasterSmooth = 16,           ///< Plaster, smooth
            PlywoodPanel = 17,            ///< Plywood panel
            PolishedConcreteOrTile = 18,  ///< Polished concrete or tile
            Sheetrock = 19,               ///< Sheetrock
            WaterOrIceSurface = 20,       ///< Water or ice surface
            WoodCeiling = 21,             ///< Wood ceiling
            WoodPanel = 22                ///< Wood panel
        }

        /// Room surface material in negative x direction.
        [FormerlySerializedAs("leftWall")]
        public SurfaceMaterial LeftWall = SurfaceMaterial.ConcreteBlockCoarse;

        /// Room surface material in positive x direction.
        [FormerlySerializedAs("rightWall")]
        public SurfaceMaterial RightWall = SurfaceMaterial.ConcreteBlockCoarse;

        /// Room surface material in negative y direction.
        [FormerlySerializedAs("floor")]
        public SurfaceMaterial Floor = SurfaceMaterial.ParquetOnConcrete;

        /// Room surface material in positive y direction.
        [FormerlySerializedAs("ceiling")]
        public SurfaceMaterial Ceiling = SurfaceMaterial.PlasterRough;

        /// Room surface material in negative z direction.
        [FormerlySerializedAs("backWall")]
        public SurfaceMaterial BackWall = SurfaceMaterial.ConcreteBlockCoarse;

        /// Room surface material in positive z direction.
        [FormerlySerializedAs("frontWall")]
        public SurfaceMaterial FrontWall = SurfaceMaterial.ConcreteBlockCoarse;

        /// Reflectivity scalar for each surface of the room.
        [FormerlySerializedAs("reflectivity")]
        public float Reflectivity = 1.0f;

        /// Reverb gain modifier in decibels.
        [FormerlySerializedAs("reverbGainDb")]
        public float ReverbGainDb = 0.0f;

        /// Reverb brightness modifier.
        [FormerlySerializedAs("reverbBrightness")]
        public float ReverbBrightness = 0.0f;

        /// Reverb time modifier.
        [FormerlySerializedAs("reverbTime")]
        public float ReverbTime = 1.0f;

        /// Size of the room (normalized with respect to scale of the game object).
        [FormerlySerializedAs("size")]
        public Vector3 Size = Vector3.one;

        private void OnEnable()
        {
            FmodResonanceAudio.UpdateAudioRoom(this, FmodResonanceAudio.IsListenerInsideRoom(this));
        }

        private void OnDisable()
        {
            FmodResonanceAudio.UpdateAudioRoom(this, false);
        }

        private void Update()
        {
            FmodResonanceAudio.UpdateAudioRoom(this, FmodResonanceAudio.IsListenerInsideRoom(this));
        }

        private void OnDrawGizmosSelected()
        {
            // Draw shoebox model wireframe of the room.
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Size);
        }
    }
}
