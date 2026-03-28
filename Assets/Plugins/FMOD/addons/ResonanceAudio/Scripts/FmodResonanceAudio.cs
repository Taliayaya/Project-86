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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMODUnity;

namespace FMODUnityResonance
{
    /// This is the main Resonance Audio class that communicates with the FMOD Unity integration. Native
    /// functions of the system can only be called through this class to preserve the internal system
    /// functionality.
    public static class FmodResonanceAudio
    {
        /// Maximum allowed gain value in decibels.
        public const float MaxGainDb = 24.0f;

        /// Minimum allowed gain value in decibels.
        public const float MinGainDb = -24.0f;

        /// Maximum allowed reverb brightness modifier value.
        public const float MaxReverbBrightness = 1.0f;

        /// Minimum allowed reverb brightness modifier value.
        public const float MinReverbBrightness = -1.0f;

        /// Maximum allowed reverb time modifier value.
        public const float MaxReverbTime = 3.0f;

        /// Maximum allowed reflectivity multiplier of a room surface material.
        public const float MaxReflectivity = 2.0f;

        // Right-handed to left-handed matrix converter (and vice versa).
        private static readonly Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1, 1, -1));

        // Get a handle to the Resonance Audio Listener FMOD Plugin.
        private static readonly string listenerPluginName = "Resonance Audio Listener";

        // Size of |RoomProperties| struct in bytes.
        private static readonly int roomPropertiesSize = Marshal.SizeOf<RoomProperties>();

        // Plugin data parameter index for the room properties.
        private static readonly int roomPropertiesIndex = 1;

        // Boundaries instance to be used in room detection logic.
        private static Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        // Container to store the currently active rooms in the scene.
        private static List<FmodResonanceAudioRoom> enabledRooms = new List<FmodResonanceAudioRoom>();

        // Current listener position.
        private static FMOD.VECTOR listenerPositionFmod = new FMOD.VECTOR();

        // FMOD Resonance Audio Listener Plugin.
        private static FMOD.DSP listenerPlugin;

        /// Updates the room effects of the environment with given |room| properties.
        /// @note This should only be called from the main Unity thread.
        public static void UpdateAudioRoom(FmodResonanceAudioRoom room, bool roomEnabled)
        {
            // Update the enabled rooms list.
            if (roomEnabled)
            {
                if (!enabledRooms.Contains(room))
                {
                    enabledRooms.Add(room);
                }
            }
            else
            {
                enabledRooms.Remove(room);
            }
            // Update the current room effects to be applied.
            if (enabledRooms.Count > 0)
            {
                FmodResonanceAudioRoom currentRoom = enabledRooms[enabledRooms.Count - 1];
                RoomProperties roomProperties = GetRoomProperties(currentRoom);
                // Pass the room properties into a pointer.
                IntPtr roomPropertiesPtr = Marshal.AllocHGlobal(roomPropertiesSize);
                Marshal.StructureToPtr(roomProperties, roomPropertiesPtr, false);
                ListenerPlugin.setParameterData(roomPropertiesIndex, GetBytes(roomPropertiesPtr,
                                                                               roomPropertiesSize));
                Marshal.FreeHGlobal(roomPropertiesPtr);
            }
            else
            {
                // Set the room properties to a null room, which will effectively disable the room effects.
                ListenerPlugin.setParameterData(roomPropertiesIndex, GetBytes(IntPtr.Zero, 0));
            }
        }

        /// Returns whether the listener is currently inside the given |room| boundaries.
        public static bool IsListenerInsideRoom(FmodResonanceAudioRoom room)
        {
            // Compute the room position relative to the listener.
            FMOD.VECTOR unused;
            RuntimeManager.CoreSystem.get3DListenerAttributes(0, out listenerPositionFmod, out unused,
                                                                  out unused, out unused);
            Vector3 listenerPosition = new Vector3(listenerPositionFmod.x, listenerPositionFmod.y,
                                                   listenerPositionFmod.z);
            Vector3 relativePosition = listenerPosition - room.transform.position;
            Quaternion rotationInverse = Quaternion.Inverse(room.transform.rotation);
            // Set the size of the room as the boundary and return whether the listener is inside.
            bounds.size = Vector3.Scale(room.transform.lossyScale, room.Size);
            return bounds.Contains(rotationInverse * relativePosition);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RoomProperties
        {
            // Center position of the room in world space.
            public float PositionX;
            public float PositionY;
            public float PositionZ;

            // Rotation (quaternion) of the room in world space.
            public float RotationX;
            public float RotationY;
            public float RotationZ;
            public float RotationW;

            // Size of the shoebox room in world space.
            public float DimensionsX;
            public float DimensionsY;
            public float DimensionsZ;

            // Material name of each surface of the shoebox room.
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialLeft;
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialRight;
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialBottom;
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialTop;
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialFront;
            public FmodResonanceAudioRoom.SurfaceMaterial MaterialBack;

            // User defined uniform scaling factor for reflectivity. This parameter has no effect when set
            // to 1.0f.
            public float ReflectionScalar;

            // User defined reverb tail gain multiplier. This parameter has no effect when set to 0.0f.
            public float ReverbGain;

            // Adjusts the reverberation time across all frequency bands. RT60 values are multiplied by this
            // factor. Has no effect when set to 1.0f.
            public float ReverbTime;

            // Controls the slope of a line from the lowest to the highest RT60 values (increases high
            // frequency RT60s when positive, decreases when negative). Has no effect when set to 0.0f.
            public float ReverbBrightness;
        };

        // Returns the FMOD Resonance Audio Listener Plugin.
        private static FMOD.DSP ListenerPlugin
        {
            get
            {
                if (!listenerPlugin.hasHandle())
                {
                    listenerPlugin = Initialize();
                }
                return listenerPlugin;
            }
        }

        // Converts given |db| value to its amplitude equivalent where 'dB = 20 * log10(amplitude)'.
        private static float ConvertAmplitudeFromDb(float db)
        {
            return Mathf.Pow(10.0f, 0.05f * db);
        }

        // Converts given |position| and |rotation| from Unity space to audio space.
        private static void ConvertAudioTransformFromUnity(ref Vector3 position,
          ref Quaternion rotation)
        {
            // Compose the transformation matrix.
            Matrix4x4 transformMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            // Convert the transformation matrix from left-handed to right-handed.
            transformMatrix = flipZ * transformMatrix * flipZ;
            // Update |position| and |rotation| respectively.
            position = transformMatrix.GetColumn(3);
            rotation = Quaternion.LookRotation(transformMatrix.GetColumn(2), transformMatrix.GetColumn(1));
        }

        // Returns a byte array of |length| created from |ptr|.
        private static byte[] GetBytes(IntPtr ptr, int length)
        {
            if (ptr != IntPtr.Zero)
            {
                byte[] byteArray = new byte[length];
                Marshal.Copy(ptr, byteArray, 0, length);
                return byteArray;
            }
            // Return an empty array if the pointer is null.
            return new byte[1];
        }

        // Returns room properties of the given |room|.
        private static RoomProperties GetRoomProperties(FmodResonanceAudioRoom room)
        {
            RoomProperties roomProperties;
            Vector3 position = room.transform.position;
            Quaternion rotation = room.transform.rotation;
            Vector3 scale = Vector3.Scale(room.transform.lossyScale, room.Size);
            ConvertAudioTransformFromUnity(ref position, ref rotation);
            roomProperties.PositionX = position.x;
            roomProperties.PositionY = position.y;
            roomProperties.PositionZ = position.z;
            roomProperties.RotationX = rotation.x;
            roomProperties.RotationY = rotation.y;
            roomProperties.RotationZ = rotation.z;
            roomProperties.RotationW = rotation.w;
            roomProperties.DimensionsX = scale.x;
            roomProperties.DimensionsY = scale.y;
            roomProperties.DimensionsZ = scale.z;
            roomProperties.MaterialLeft = room.LeftWall;
            roomProperties.MaterialRight = room.RightWall;
            roomProperties.MaterialBottom = room.Floor;
            roomProperties.MaterialTop = room.Ceiling;
            roomProperties.MaterialFront = room.FrontWall;
            roomProperties.MaterialBack = room.BackWall;
            roomProperties.ReverbGain = ConvertAmplitudeFromDb(room.ReverbGainDb);
            roomProperties.ReverbTime = room.ReverbTime;
            roomProperties.ReverbBrightness = room.ReverbBrightness;
            roomProperties.ReflectionScalar = room.Reflectivity;
            return roomProperties;
        }

        // Initializes and returns the FMOD Resonance Audio Listener Plugin.
        private static FMOD.DSP Initialize()
        {
            // Search through all busses on in banks.
            int numBanks = 0;
            FMOD.DSP dsp = new FMOD.DSP();
            FMOD.Studio.Bank[] banks = null;
            RuntimeManager.StudioSystem.getBankCount(out numBanks);
            RuntimeManager.StudioSystem.getBankList(out banks);
            for (int currentBank = 0; currentBank < numBanks; ++currentBank)
            {
                int numBusses = 0;
                FMOD.Studio.Bus[] busses = null;
                banks[currentBank].getBusCount(out numBusses);
                banks[currentBank].getBusList(out busses);
                for (int currentBus = 0; currentBus < numBusses; ++currentBus)
                {
                    // Make sure the channel group of the current bus is assigned properly.
                    string busPath = null;
                    busses[currentBus].getPath(out busPath);
                    RuntimeManager.StudioSystem.getBus(busPath, out busses[currentBus]);
                    busses[currentBus].lockChannelGroup();
                    RuntimeManager.StudioSystem.flushCommands();
                    FMOD.ChannelGroup channelGroup;
                    busses[currentBus].getChannelGroup(out channelGroup);
                    if (channelGroup.hasHandle())
                    {
                        int numDsps = 0;
                        channelGroup.getNumDSPs(out numDsps);
                        for (int currentDsp = 0; currentDsp < numDsps; ++currentDsp)
                        {
                            channelGroup.getDSP(currentDsp, out dsp);
                            string dspNameSb;
                            int unusedInt = 0;
                            uint unusedUint = 0;
                            dsp.getInfo(out dspNameSb, out unusedUint, out unusedInt, out unusedInt, out unusedInt);
                            if (dspNameSb.ToString().Equals(listenerPluginName) && dsp.hasHandle())
                            {
                                return dsp;
                            }
                        }
                    }
                    busses[currentBus].unlockChannelGroup();
                }
            }
            RuntimeUtils.DebugLogError(listenerPluginName + " not found in the FMOD project.");
            return dsp;
        }
    }
}
