using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMOD
{
    [Serializable]
    public partial struct GUID : IEquatable<GUID>
    {
        public GUID(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            Data1 = BitConverter.ToInt32(bytes,  0);
            Data2 = BitConverter.ToInt32(bytes,  4);
            Data3 = BitConverter.ToInt32(bytes,  8);
            Data4 = BitConverter.ToInt32(bytes, 12);
        }

        public static GUID Parse(string s)
        {
            return new GUID(new Guid(s));
        }

        public bool IsNull
        {
            get
            {
                return Data1 == 0
                    && Data2 == 0
                    && Data3 == 0
                    && Data4 == 0;
            }
        }

        public override bool Equals(object other)
        {
            return (other is GUID) && Equals((GUID)other);
        }

        public bool Equals(GUID other)
        {
            return Data1 == other.Data1
                && Data2 == other.Data2
                && Data3 == other.Data3
                && Data4 == other.Data4;
        }

        public static bool operator==(GUID a, GUID b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(GUID a, GUID b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return Data1 ^ Data2 ^ Data3 ^ Data4;
        }

        public static implicit operator Guid(GUID guid)
        {
            return new Guid(guid.Data1,
                    (short) ((guid.Data2 >>  0) & 0xFFFF),
                    (short) ((guid.Data2 >> 16) & 0xFFFF),
                    (byte)  ((guid.Data3 >>  0) & 0xFF),
                    (byte)  ((guid.Data3 >>  8) & 0xFF),
                    (byte)  ((guid.Data3 >> 16) & 0xFF),
                    (byte)  ((guid.Data3 >> 24) & 0xFF),
                    (byte)  ((guid.Data4 >>  0) & 0xFF),
                    (byte)  ((guid.Data4 >>  8) & 0xFF),
                    (byte)  ((guid.Data4 >> 16) & 0xFF),
                    (byte)  ((guid.Data4 >> 24) & 0xFF)
                );
        }

        public override string ToString()
        {
            return ((Guid)this).ToString("B");
        }
    }
}

namespace FMODUnity
{
    public class EventNotFoundException : Exception
    {
        public FMOD.GUID Guid;
        public string Path;

        public EventNotFoundException(string path)
            : base("[FMOD] Event not found: '" + path + "'")
        {
            Path = path;
        }

        public EventNotFoundException(FMOD.GUID guid)
            : base("[FMOD] Event not found: " + guid)
        {
            Guid = guid;
        }

        public EventNotFoundException(EventReference eventReference)
            : base("[FMOD] Event not found: " + eventReference.ToString())
        {
            Guid = eventReference.Guid;

#if UNITY_EDITOR
#if !FMOD_SERIALIZE_GUID_ONLY
            Path = eventReference.Path;
#endif
#endif
        }
    }

    public class BusNotFoundException : Exception
    {
        public string Path;

        public BusNotFoundException(string path)
            : base("[FMOD] Bus not found '" + path + "'")
        {
            Path = path;
        }
    }

    public class VCANotFoundException : Exception
    {
        public string Path;

        public VCANotFoundException(string path)
            : base("[FMOD] VCA not found '" + path + "'")
        {
            Path = path;
        }
    }

    public class BankLoadException : Exception
    {
        public string Path;
        public FMOD.RESULT Result;

        public BankLoadException(string path, FMOD.RESULT result)
            : base(string.Format("[FMOD] Could not load bank '{0}' : {1} : {2}", path, result.ToString(), FMOD.Error.String(result)))
        {
            Path = path;
            Result = result;
        }
        public BankLoadException(string path, string error)
            : base(string.Format("[FMOD] Could not load bank '{0}' : {1}", path, error))
        {
            Path = path;
            Result = FMOD.RESULT.ERR_INTERNAL;
        }
    }

    public class SystemNotInitializedException : Exception
    {
        public FMOD.RESULT Result;
        public string Location;

        public SystemNotInitializedException(FMOD.RESULT result, string location)
            : base(string.Format("[FMOD] Initialization failed : {2} : {0} : {1}", result.ToString(), FMOD.Error.String(result), location))
        {
            Result = result;
            Location = location;
        }

        public SystemNotInitializedException(Exception inner)
            : base("[FMOD] Initialization failed", inner)
        {
        }
    }

    public enum EmitterGameEvent : int
    {
        None,
        ObjectStart,
        ObjectDestroy,
        TriggerEnter,
        TriggerExit,
        TriggerEnter2D,
        TriggerExit2D,
        CollisionEnter,
        CollisionExit,
        CollisionEnter2D,
        CollisionExit2D,
        ObjectEnable,
        ObjectDisable,
        ObjectMouseEnter,
        ObjectMouseExit,
        ObjectMouseDown,
        ObjectMouseUp,
        UIMouseEnter,
        UIMouseExit,
        UIMouseDown,
        UIMouseUp,
    }

    public enum LoaderGameEvent : int
    {
        None,
        ObjectStart,
        ObjectDestroy,
        TriggerEnter,
        TriggerExit,
        TriggerEnter2D,
        TriggerExit2D,
        ObjectEnable,
        ObjectDisable,
    }

    // We use our own enum to avoid serialization issues if FMOD.THREAD_TYPE changes
    public enum ThreadType
    {
        Mixer,
        Feeder,
        Stream,
        File,
        Nonblocking,
        Record,
        Geometry,
        Profiler,
        Studio_Update,
        Studio_Load_Bank,
        Studio_Load_Sample,
        Convolution_1,
        Convolution_2,
    }

    // We use our own enum to avoid serialization issues if FMOD.THREAD_AFFINITY changes
    [Flags]
    public enum ThreadAffinity : uint
    {
        Any = 0,
        Core0 = 1 << 0,
        Core1 = 1 << 1,
        Core2 = 1 << 2,
        Core3 = 1 << 3,
        Core4 = 1 << 4,
        Core5 = 1 << 5,
        Core6 = 1 << 6,
        Core7 = 1 << 7,
        Core8 = 1 << 8,
        Core9 = 1 << 9,
        Core10 = 1 << 10,
        Core11 = 1 << 11,
        Core12 = 1 << 12,
        Core13 = 1 << 13,
        Core14 = 1 << 14,
        Core15 = 1 << 15,
    }

    // Using a separate enum to avoid serialization issues if FMOD.SOUND_TYPE changes.
    public enum CodecType : int
    {
        FADPCM,
        Vorbis,
        AT9,
        XMA,
        Opus
    }

    [Serializable]
    public class ThreadAffinityGroup
    {
        public List<ThreadType> threads = new List<ThreadType>();
        public ThreadAffinity affinity = ThreadAffinity.Any;

        public ThreadAffinityGroup()
        {
        }

        public ThreadAffinityGroup(ThreadAffinityGroup other)
        {
            threads = new List<ThreadType>(other.threads);
            affinity = other.affinity;
        }

        public ThreadAffinityGroup(ThreadAffinity affinity, params ThreadType[] threads)
        {
            this.threads = new List<ThreadType>(threads);
            this.affinity = affinity;
        }
    }

    [Serializable]
    public class CodecChannelCount
    {
        public CodecType format;
        public int channels;

        public CodecChannelCount() { }

        public CodecChannelCount(CodecChannelCount other)
        {
            format = other.format;
            channels = other.channels;
        }
    }

    public static class RuntimeUtils
    {
#if UNITY_EDITOR
        private static string pluginBasePath;

        public const string BaseFolderGUID = "06ae579381df01a4a87bb149dec89954";
        public const string PluginBasePathDefault = "Assets/Plugins/FMOD";

        public static string PluginBasePath
        {
            get
            {
                if (pluginBasePath == null)
                {
                    pluginBasePath = AssetDatabase.GUIDToAssetPath(BaseFolderGUID);

                    if (string.IsNullOrEmpty(pluginBasePath))
                    {
                        pluginBasePath = PluginBasePathDefault;

                        DebugLogWarningFormat("FMOD: Couldn't find base folder with GUID {0}; defaulting to {1}",
                            BaseFolderGUID, pluginBasePath);
                    }
                }

                return pluginBasePath;
            }
        }
#endif

        public static string GetCommonPlatformPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.Replace('\\', '/');
        }

        public static FMOD.VECTOR ToFMODVector(this Vector3 vec)
        {
            FMOD.VECTOR temp;
            temp.x = vec.x;
            temp.y = vec.y;
            temp.z = vec.z;

            return temp;
        }

        public static FMOD.ATTRIBUTES_3D To3DAttributes(this Vector3 pos)
        {
            FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D();
            attributes.forward = ToFMODVector(Vector3.forward);
            attributes.up = ToFMODVector(Vector3.up);
            attributes.position = ToFMODVector(pos);

            return attributes;
        }

        public static FMOD.ATTRIBUTES_3D To3DAttributes(this Transform transform)
        {
            FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D();
            attributes.forward = transform.forward.ToFMODVector();
            attributes.up = transform.up.ToFMODVector();
            attributes.position = transform.position.ToFMODVector();

            return attributes;
        }

        public static FMOD.ATTRIBUTES_3D To3DAttributes(this Transform transform, Vector3 velocity)
        {
            FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D();
            attributes.forward = transform.forward.ToFMODVector();
            attributes.up = transform.up.ToFMODVector();
            attributes.position = transform.position.ToFMODVector();
            attributes.velocity = velocity.ToFMODVector();

            return attributes;
        }

        public static FMOD.ATTRIBUTES_3D To3DAttributes(this GameObject go)
        {
            return go.transform.To3DAttributes();
        }

#if UNITY_PHYSICS_EXIST
        public static FMOD.ATTRIBUTES_3D To3DAttributes(Transform transform, Rigidbody rigidbody = null)
        {
            FMOD.ATTRIBUTES_3D attributes = transform.To3DAttributes();

            if (rigidbody)
            {
#if UNITY_6000_0_OR_NEWER
                attributes.velocity = rigidbody.linearVelocity.ToFMODVector();
#else
                attributes.velocity = rigidbody.velocity.ToFMODVector();
#endif
            }

            return attributes;
        }

        public static FMOD.ATTRIBUTES_3D To3DAttributes(GameObject go, Rigidbody rigidbody)
        {
            FMOD.ATTRIBUTES_3D attributes = go.transform.To3DAttributes();

            if (rigidbody)
            {
#if UNITY_6000_0_OR_NEWER
                attributes.velocity = rigidbody.linearVelocity.ToFMODVector();
#else
                attributes.velocity = rigidbody.velocity.ToFMODVector();
#endif
            }

            return attributes;
        }
#endif

#if UNITY_PHYSICS2D_EXIST
        public static FMOD.ATTRIBUTES_3D To3DAttributes(Transform transform, Rigidbody2D rigidbody)
        {
            FMOD.ATTRIBUTES_3D attributes = transform.To3DAttributes();

            if (rigidbody)
            {
                FMOD.VECTOR vel;
#if UNITY_6000_1_OR_NEWER
                vel.x = rigidbody.linearVelocity.x;
                vel.y = rigidbody.linearVelocity.y;
#else
#pragma warning disable CS0618
                vel.x = rigidbody.velocity.x;
                vel.y = rigidbody.velocity.y;
#pragma warning restore CS0618
#endif
                vel.z = 0;
                attributes.velocity = vel;
            }

            return attributes;
        }


        public static FMOD.ATTRIBUTES_3D To3DAttributes(GameObject go, Rigidbody2D rigidbody)
        {
            FMOD.ATTRIBUTES_3D attributes = go.transform.To3DAttributes();

            if (rigidbody)
            {
                FMOD.VECTOR vel;
#if UNITY_6000_1_OR_NEWER
                vel.x = rigidbody.linearVelocity.x;
                vel.y = rigidbody.linearVelocity.y;
#else
#pragma warning disable CS0618
                vel.x = rigidbody.velocity.x;
                vel.y = rigidbody.velocity.y;
#pragma warning restore CS0618
#endif
                vel.z = 0;
                attributes.velocity = vel;
            }

            return attributes;
        }
#endif

        public static FMOD.THREAD_TYPE ToFMODThreadType(ThreadType threadType)
        {
            switch (threadType)
            {
                case ThreadType.Mixer:
                    return FMOD.THREAD_TYPE.MIXER;
                case ThreadType.Feeder:
                    return FMOD.THREAD_TYPE.FEEDER;
                case ThreadType.Stream:
                    return FMOD.THREAD_TYPE.STREAM;
                case ThreadType.File:
                    return FMOD.THREAD_TYPE.FILE;
                case ThreadType.Nonblocking:
                    return FMOD.THREAD_TYPE.NONBLOCKING;
                case ThreadType.Record:
                    return FMOD.THREAD_TYPE.RECORD;
                case ThreadType.Geometry:
                    return FMOD.THREAD_TYPE.GEOMETRY;
                case ThreadType.Profiler:
                    return FMOD.THREAD_TYPE.PROFILER;
                case ThreadType.Studio_Update:
                    return FMOD.THREAD_TYPE.STUDIO_UPDATE;
                case ThreadType.Studio_Load_Bank:
                    return FMOD.THREAD_TYPE.STUDIO_LOAD_BANK;
                case ThreadType.Studio_Load_Sample:
                    return FMOD.THREAD_TYPE.STUDIO_LOAD_SAMPLE;
                case ThreadType.Convolution_1:
                    return FMOD.THREAD_TYPE.CONVOLUTION1;
                case ThreadType.Convolution_2:
                    return FMOD.THREAD_TYPE.CONVOLUTION2;
                default:
                    throw new ArgumentException("Unrecognised thread type '" + threadType.ToString() + "'");
            }
        }

        public static string DisplayName(this ThreadType thread)
        {
            return thread.ToString().Replace('_', ' ');
        }

        public static FMOD.THREAD_AFFINITY ToFMODThreadAffinity(ThreadAffinity affinity)
        {
            FMOD.THREAD_AFFINITY fmodAffinity = FMOD.THREAD_AFFINITY.CORE_ALL;

            SetFMODAffinityBit(affinity, ThreadAffinity.Core0, FMOD.THREAD_AFFINITY.CORE_0, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core1, FMOD.THREAD_AFFINITY.CORE_1, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core2, FMOD.THREAD_AFFINITY.CORE_2, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core3, FMOD.THREAD_AFFINITY.CORE_3, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core4, FMOD.THREAD_AFFINITY.CORE_4, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core5, FMOD.THREAD_AFFINITY.CORE_5, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core6, FMOD.THREAD_AFFINITY.CORE_6, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core7, FMOD.THREAD_AFFINITY.CORE_7, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core8, FMOD.THREAD_AFFINITY.CORE_8, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core9, FMOD.THREAD_AFFINITY.CORE_9, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core10, FMOD.THREAD_AFFINITY.CORE_10, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core11, FMOD.THREAD_AFFINITY.CORE_11, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core12, FMOD.THREAD_AFFINITY.CORE_12, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core13, FMOD.THREAD_AFFINITY.CORE_13, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core14, FMOD.THREAD_AFFINITY.CORE_14, ref fmodAffinity);
            SetFMODAffinityBit(affinity, ThreadAffinity.Core15, FMOD.THREAD_AFFINITY.CORE_15, ref fmodAffinity);

            return fmodAffinity;
        }

        private static void SetFMODAffinityBit(ThreadAffinity affinity, ThreadAffinity mask,
            FMOD.THREAD_AFFINITY fmodMask, ref FMOD.THREAD_AFFINITY fmodAffinity)
        {
            if ((affinity & mask) != 0)
            {
                fmodAffinity |= fmodMask;
            }
        }

        public static void EnforceLibraryOrder()
        {
            // Call a function in fmod.dll to make sure it's loaded before fmodstudio.dll
            int temp1, temp2;
            FMOD.Memory.GetStats(out temp1, out temp2);

            FMOD.GUID temp3;
            FMOD.Studio.Util.parseID("", out temp3);
        }

        public static void DebugLog(string message)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel == FMOD.DEBUG_FLAGS.LOG)
            {
                Debug.Log(message);
            }
        }

        public static void DebugLogFormat(string format, params object[] args)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel == FMOD.DEBUG_FLAGS.LOG)
            {
                Debug.LogFormat(format, args);
            }
        }

        public static void DebugLogWarning(string message)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel >= FMOD.DEBUG_FLAGS.WARNING)
            {
                Debug.LogWarning(message);
            }
        }

        public static void DebugLogWarningFormat(string format, params object[] args)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel >= FMOD.DEBUG_FLAGS.WARNING)
            {
                Debug.LogWarningFormat(format, args);
            }
        }

        public static void DebugLogError(string message)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel >= FMOD.DEBUG_FLAGS.ERROR)
            {
                Debug.LogError(message);
            }
        }

        public static void DebugLogErrorFormat(string format, params object[] args)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel >= FMOD.DEBUG_FLAGS.ERROR)
            {
                Debug.LogErrorFormat(format, args);
            }
        }

        public static void DebugLogException(Exception e)
        {
            if (!Settings.IsInitialized() || Settings.Instance.LoggingLevel >= FMOD.DEBUG_FLAGS.ERROR)
            {
                Debug.LogException(e);
            }
        }

        public static string GetPluginArchitectureFolder()
        {
            switch (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture)
            {
                case System.Runtime.InteropServices.Architecture.Arm:
                    throw new System.NotSupportedException("[FMOD] Attempted to load FMOD plugins on a 32 bit ARM platform.");
                case System.Runtime.InteropServices.Architecture.Arm64:
                    return "arm64";
                case System.Runtime.InteropServices.Architecture.X86:
                    return "x86";
                default:
                    return "x86_64";
            }
        }

#if UNITY_EDITOR
        public static string WritableAssetPath(string subPath)
        {
            if (RuntimeUtils.PluginBasePath.StartsWith("Assets/"))
            {
                return $"{RuntimeUtils.PluginBasePath}/{subPath}.asset";
            }
            else
            {
                return $"Assets/Plugins/FMOD/{subPath}.asset";
            }
        }
#endif
    }
}
