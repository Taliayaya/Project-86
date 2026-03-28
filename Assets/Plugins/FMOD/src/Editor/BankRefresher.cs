using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    public class BankRefresher
    {
        private static string currentWatchPath;
        private static FileSystemWatcher sourceFileWatcher;
        private static bool sourceFilesChanged = false;
        private static float lastSourceFileChange = float.MaxValue;
        private static bool autoRefresh = true;
        private static float nextFilePollTime = float.MinValue;

        private const int FilePollPeriod = 5;

        public static void DisableAutoRefresh()
        {
            autoRefresh = false;
        }

        public static void Startup()
        {
            sourceFileWatcher = new FileSystemWatcher();
            sourceFileWatcher.IncludeSubdirectories = true;
            sourceFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

            sourceFileWatcher.Changed += OnSourceFileChanged;
            sourceFileWatcher.Created += OnSourceFileChanged;
            sourceFileWatcher.Deleted += OnSourceFileChanged;

            EditorApplication.update += Update;
        }

        private static void OnSourceFileChanged(object source, FileSystemEventArgs e)
        {
            sourceFilesChanged = true;
        }

        private static void Update()
        {
            UpdateFileWatcherPath();
            CheckSourceFilesChanged();
            CheckCacheFileExists();
            RefreshBanksIfReady();
        }

        private static void UpdateFileWatcherPath()
        {
            string sourceBankPath = Settings.Instance.SourceBankPath;

            string pathToWatch;

            if (Path.IsPathRooted(sourceBankPath))
            {
                pathToWatch = Path.GetFullPath(sourceBankPath);
            }
            else
            {
                pathToWatch = Path.GetFullPath(Environment.CurrentDirectory + "/" + sourceBankPath);
            }

            if (currentWatchPath != pathToWatch)
            {
                currentWatchPath = pathToWatch;

                try {
                    sourceFileWatcher.EnableRaisingEvents = false;
                    sourceFilesChanged = false;

                    if (!string.IsNullOrEmpty(sourceBankPath))
                    {
                        sourceFileWatcher.Path = pathToWatch;
                        sourceFileWatcher.EnableRaisingEvents = true;
                    }
                }
                catch (ArgumentException e)
                {
                    RuntimeUtils.DebugLogWarningFormat("Error watching {0}: {1}", pathToWatch, e.Message);
                }
            }
        }

        private static void CheckSourceFilesChanged()
        {
            if (sourceFilesChanged)
            {
                lastSourceFileChange = Time.realtimeSinceStartup;
                sourceFilesChanged = false;

                if (!BankRefreshWindow.IsVisible)
                {
                    autoRefresh = true;
                }

                if (IsWindowEnabled())
                {
                    BankRefreshWindow.ShowWindow();
                }
            }
        }

        private static void CheckCacheFileExists()
        {
            if (Time.realtimeSinceStartup >= nextFilePollTime)
            {
                if (!File.Exists(EventManager.CacheAssetFullName))
                {
                    EventManager.RefreshBanks();
                }

                nextFilePollTime = Time.realtimeSinceStartup + FilePollPeriod;
            }
        }

        private static void RefreshBanksIfReady()
        {
            if (TimeUntilBankRefresh() == 0 && BankRefreshWindow.ReadyToRefreshBanks)
            {
                EventManager.RefreshBanks();
            }
        }

        public static void HandleBankRefresh(string result)
        {
            lastSourceFileChange = float.MaxValue;
            BankRefreshWindow.HandleBankRefresh(result);
        }

        private static bool IsWindowEnabled()
        {
            Settings settings = Settings.Instance;

            return settings.BankRefreshCooldown == Settings.BankRefreshPrompt
                || (settings.BankRefreshCooldown >= 0 && settings.ShowBankRefreshWindow);
        }

        public static float TimeSinceSourceFileChange()
        {
            if (lastSourceFileChange == float.MaxValue)
            {
                return float.MaxValue;
            }
            else
            {
                return Mathf.Max(0, Time.realtimeSinceStartup - lastSourceFileChange);
            }
        }

        public static float TimeUntilBankRefresh()
        {
            if (!autoRefresh
                || lastSourceFileChange == float.MaxValue
                || Settings.Instance.BankRefreshCooldown < 0)
            {
                return float.MaxValue;
            }
            else
            {
                float nextRefreshTime = lastSourceFileChange + Settings.Instance.BankRefreshCooldown;
                return Mathf.Max(0, nextRefreshTime - Time.realtimeSinceStartup);
            }
        }
    }
}
