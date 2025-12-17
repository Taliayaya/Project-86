using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformMobileHigh : PlatformMobileLow
    {
        static PlatformMobileHigh()
        {
            Settings.AddPlatformTemplate<PlatformMobileHigh>("fd7c55dab0fce234b8c25f6ffca523c1");
        }

        internal override string DisplayName { get { return "High-End Mobile"; } }
#if UNITY_EDITOR
        internal override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.MobileHigh; } }
#endif

        internal override float Priority { get { return base.Priority + 1; } }

        internal override bool MatchesCurrentEnvironment
        {
            get
            {
                if (!Active)
                {
                    return false;
                }

#if UNITY_IOS
                switch (UnityEngine.iOS.Device.generation)
                {
                    case UnityEngine.iOS.DeviceGeneration.iPad1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPad2Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPad3Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPadMini1Gen:
                    case UnityEngine.iOS.DeviceGeneration.iPhone:
                    case UnityEngine.iOS.DeviceGeneration.iPhone3G:
                    case UnityEngine.iOS.DeviceGeneration.iPhone3GS:
                    case UnityEngine.iOS.DeviceGeneration.iPhone4:
                    case UnityEngine.iOS.DeviceGeneration.iPhone4S:
                        return false;
                    default:
                        return true;
                }
#elif UNITY_ANDROID
                if (SystemInfo.processorCount <= 2)
                {
                    return false;
                }
                else if (SystemInfo.processorCount >= 8)
                {
                    return true;
                }
                else
                {
                    // check the clock rate on quad core systems
                    string freqinfo = "/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq";
                    try
                    {
                        using (System.IO.TextReader reader = new System.IO.StreamReader(freqinfo))
                        {
                            string line = reader.ReadLine();
                            int khz = int.Parse(line) / 1000;
                            if (khz >= 1600)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
#else
                return false;
#endif
            }
        }
    }
}
