using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Editor.InspectorWindows
{
    public class OpenUpmSearcher : EditorWindow
    {
        private const string WindowTitle = "OpenUPM Searcher";
        private static readonly Vector2 WindowMinSize = new(350, 200);

        private const string MatchedColorEditorUserSettingsKey = WindowTitle + "_MatchedColorJSON";
        private const string CacheFilePath = ".cache/api_cache.txt";
        private const int DisplayMax = 500;

        private Vector2 _scroll;
        private string _searchText = "";
        private List<string> _packageNameList;

        private Color? _matchedColor;
        private Color MatchedColor
        {
            get
            {
                if (_matchedColor != null) return _matchedColor.Value;
                var json = EditorUserSettings.GetConfigValue(MatchedColorEditorUserSettingsKey);
                if (string.IsNullOrEmpty(json))
                    _matchedColor = new Color32(131, 119, 255, 255);
                else
                    _matchedColor = JsonUtility.FromJson<Color>(json);
                return _matchedColor!.Value;
            }
            set
            {
                _matchedColor = value;
            }
        }

        private enum Message
        {
            None,
            GitHubAPI,
            ManifestJson,
        }

#if UNITY_2017_1_OR_NEWER
        [MenuItem("Window/Custom Tools/", priority = Int32.MaxValue)]
#endif
        [MenuItem("Window/Custom Tools/OpenUPM Searcher")]
        private static void CreateWindow()
        {
            var window = CreateInstance<OpenUpmSearcher>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = WindowMinSize;
            window.Show();
        }

        private void OnGUI()
        {
            // API
            GUILayout.BeginHorizontal();
            {
                var tempBgColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Get PackageList by GitHub API"))
                    if (_packageNameList == null) CreatePackageListCacheFromGitHubAPI();
                    else if (EditorUtility.DisplayDialog("Confirm", GetDialogMessage(Message.GitHubAPI), "Call API",
                                 "Cancel"))
                        CreatePackageListCacheFromGitHubAPI();
                GUI.backgroundColor = tempBgColor;

                if (File.Exists(CacheFilePath)) GUILayout.Label(new GUIContent("API cache file: exists", "Last updated:" + File.GetLastWriteTime(CacheFilePath)));
                else GUILayout.Label("API cache file: not exist");
            }
            GUILayout.EndHorizontal();

            // MenuItem関連
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Open ProjectSettings",
                        "Please check 'Package Manager' for your registered data.")) &&
                    !EditorApplication.ExecuteMenuItem("Edit/Project Settings..."))
                    Debug.Log("Failed Open ProjectSettings");
                if (GUILayout.Button(new GUIContent("Open PackageManager",
                        "Please check 'Packages:My Registries' for your registered data.")) &&
                    !EditorApplication.ExecuteMenuItem("Window/Package Manager"))
                    Debug.Log("Failed Open PackageManager");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Matched Highlight", GUILayout.Width(110));
                var color = EditorGUILayout.ColorField(MatchedColor);
                if (MatchedColor != color)
                {
                    //Debug.Log(color.ToString() + " :" + Event.current.type.ToString());
                    MatchedColor = color;
                    EditorUserSettings.SetConfigValue(MatchedColorEditorUserSettingsKey, JsonUtility.ToJson(color));
                }

                if (GUILayout.Button("Open manifest.json"))
                {
                    var path = Path.Combine(Application.dataPath.Replace("/Assets", ""), "Packages/manifest.json");
                    Process.Start(path);
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Filter", GUILayout.Width(35));
                _searchText = GUILayout.TextField(_searchText);
                if (GUILayout.Button("Clear", GUILayout.Width(45))) _searchText = "";
            }
            EditorGUILayout.EndScrollView();

            DrawPackageList();
        }

        private void DrawPackageList()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            if (!File.Exists(CacheFilePath) && _packageNameList == null)
            {
                GUILayout.Label("Press 'Get PackageList by GitHub API' button to get the package list.");
                return;
            }

            if (_packageNameList == null || _packageNameList.Count == 0)
                if (File.Exists(CacheFilePath))
                {
                    _packageNameList = new List<string>();
                    var packageNames = File.ReadAllLines(CacheFilePath);
                    foreach (var packageName in packageNames)
                        if (!string.IsNullOrWhiteSpace(packageName))
                            _packageNameList.Add(packageName);
                }

            var isOpenManifestJson = false;
            var count = 0;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_packageNameList != null)
                if (_searchText.Length > 0)
                    foreach (var packageName in _packageNameList
                                 .Where(packageName => packageName.Contains(_searchText))
                                 .TakeWhile(_ => count++ < DisplayMax))
                        DrawPackage(packageName, ref isOpenManifestJson);
                else
                    foreach (var packageName in _packageNameList)
                    {
                        DrawPackage(packageName, ref isOpenManifestJson);

                        if (count++ >= DisplayMax) break;
                    }

            EditorGUILayout.EndScrollView();

            if (count > DisplayMax) GUILayout.Label(count - 1 + " packages shown.");
            else GUILayout.Label($"{count} package{(count == 1 ? "" : "s")} shown.");


            if (!GUILayout.Button(new GUIContent("Register All", "Register all filtered packages to manifest.json.\n"),
                    GUILayout.Width(100))) return;
            var registeredPackages = _packageNameList.Where(packageName => packageName.Contains(_searchText))
                .Select(RegisterScope).Count(result => result);
            EditorUtility.DisplayDialog("Result",
                $"Registered {registeredPackages} package{(registeredPackages == 1 ? "" : "s")}", "OK");
        }

        private void DrawPackage(string packageName, ref bool isOpenManifestJson)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Web", "Open in Web Browser\n" + "https://openupm.com/packages/" + packageName + "/"), GUILayout.Width(36)))
                {
                    Application.OpenURL("https://openupm.com/packages/" + packageName + "/");
                }

                if (GUILayout.Button(new GUIContent("Register", "Register to manifest.json.\n'" + packageName + "'"), GUILayout.Width(60)))
                {
                    var result = RegisterScope(packageName);
                    if (result)
                    {
                        if (EditorUtility.DisplayDialog("Result", GetDialogMessage(Message.ManifestJson), "OK"))
                        {
                            isOpenManifestJson = true;
                        }
                    }
                }

                var style = new GUIStyle(EditorStyles.wordWrappedLabel);
                style.richText = true;
                var displayName = _searchText != "" ? packageName.Replace(_searchText, $"<color=#{ColorUtility.ToHtmlStringRGB(MatchedColor)}>" + _searchText + "</color>") : packageName;
                EditorGUILayout.LabelField(displayName, style);
            }
            EditorGUILayout.EndHorizontal();
        }

        public static bool RegisterScope(string scope)
        {
            const string registryName = "OpenUPM";
            const string registryURL = "https://package.openupm.com/";

            var projectPath = Application.dataPath.Replace("/Assets", "");
            var manifestPath = Path.Combine(projectPath, "Packages/manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("manifest.json not found in the Packages folder.");
                return false;
            }

            var manifestContent = File.ReadAllText(manifestPath);
            var manifestJson = JObject.Parse(manifestContent);

            var scopedRegistries = manifestJson["scopedRegistries"] as JArray;
            if (scopedRegistries == null)
            {
                scopedRegistries = new JArray();
                manifestJson["scopedRegistries"] = scopedRegistries;
            }

            JObject openUpmRegistry = null;
            foreach (var jToken in scopedRegistries)
            {
                var registry = (JObject)jToken;
                if (registry["name"] != null && registry["name"].ToString() == registryName)
                {
                    openUpmRegistry = registry;
                    break;
                }
            }

            var existingScopeList = new List<string>();
            foreach (JObject registry in scopedRegistries)
            {
                var scopes = registry["scopes"] as JArray;
                foreach (string s in scopes!)
                {
                    existingScopeList.Add(s);
                }
            }

            foreach (var existingScope in existingScopeList)
            {
                if (existingScope != scope) continue;
                Debug.LogError("'" + scope + "' is already registered scope.");
                return false;
            }

            if (openUpmRegistry == null)
            {
                openUpmRegistry = new JObject
                {
                    ["name"] = registryName,
                    ["url"] = registryURL,
                    ["scopes"] = new JArray(scope)
                };
                scopedRegistries.Add(openUpmRegistry);
            }
            else
            {
                var scopes = openUpmRegistry["scopes"] as JArray;
                scopes?.Add(scope);
            }

            File.WriteAllText(manifestPath, manifestJson.ToString());
            AssetDatabase.Refresh();
            return true;
        }

        private async void CreatePackageListCacheFromGitHubAPI()
        {
            var owner = "openupm";
            var repo = "openupm";
            var branch = "master";

            var dirSha = await GetGitHubDirectorySha(owner, repo, branch);

            if (!string.IsNullOrEmpty(dirSha))
            {
                await GetPackageFileListFromTree(owner, repo, dirSha);
            }
        }

        private async Task<string> GetGitHubDirectorySha(string owner, string repo, string branch)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/branches/{branch}";
            using (var www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("User-Agent", "Unity");
                var asyncOperation = www.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Delay(5);
                }

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + www.error);
                    return null;
                }

                var jsonObject = JObject.Parse(www.downloadHandler.text);
                var treeSha = jsonObject["commit"]?["commit"]?["tree"]?["sha"]?.ToString();
                return treeSha;
            }
        }

        private async Task GetPackageFileListFromTree(string owner, string repo, string treeSha)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{treeSha}?recursive=0";
            using (var www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("User-Agent", "Unity");
                var asyncOperation = www.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    await Task.Delay(5);
                }

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + www.error);
                }
                else
                {
                    var jsonObject = JObject.Parse(www.downloadHandler.text);
                    var treeArray = jsonObject["tree"] as JArray;
                    _packageNameList = new List<string>();

                    foreach (JObject item in treeArray)
                    {
                        var path = item["path"].ToString();
                        if (path.StartsWith("data/packages/") && path.EndsWith(".yml"))
                        {
                            var packageName = Path.GetFileNameWithoutExtension(path);
                            _packageNameList.Add(packageName);
                        }
                    }


                    Debug.Log("Found " + _packageNameList.Count + " packages.");

                    var outputText = "";
                    foreach (var packageName in _packageNameList)
                    {
                        outputText += packageName + Environment.NewLine;
                    }

                    var dirPath = Path.GetDirectoryName(CacheFilePath);
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    File.WriteAllText(CacheFilePath, outputText);

                    Debug.Log("Create cache file. path:" + CacheFilePath);
                }
            }
        }

        private string GetDialogMessage(Message msg)
        {
            var str = "";
            if (msg == Message.None) { }
            else if (msg == Message.GitHubAPI)
            {
                str = @"
This process calls the GitHub API to retrieve the list of package files in the master branch of the OpenUPM repository and create a cache file.
The GitHub API has a limit on the number of requests per minute and per hour.
Once the cache file is created at the first startup, there is no need to regenerate it for the time being.
We recommend that you do not run it more than necessary.
";
            }
            else if (msg == Message.ManifestJson)
            {
                str = @"
Registration succeeded.
Open manifest.json to reflect this in the UnityEditor.
To be precise, deactivate Unity once and it will be reflected.
If it is reflected in ProjectSettings, you can install it from PackageManager (Packages:Unity Registry).
";
            }

            return str;
        }

    }
}