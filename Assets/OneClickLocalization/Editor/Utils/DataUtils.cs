using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using OneClickLocalization.Core;
using Object = UnityEngine.Object;

namespace OneClickLocalization.Editor.Utils
{
    static class DataUtils
    {
        /// <summary>
        /// Load a Setup Asset
        /// </summary>
        /// <returns></returns>
        public static bool LoadSetup()
        {
            string openPath = EditorUtility.OpenFilePanel("Load Setup Asset", Application.dataPath, "asset");

            if (openPath != null && !openPath.Equals(""))
            {
                string[] setupAssets =
                    AssetDatabase.FindAssets("OCLSetup", new string[] {LocalizationSetup.setupPersistPath});
                if (setupAssets == null || setupAssets.Length > 0)
                {
                    if (!EditorUtility.DisplayDialog("Setup exists",
                        "OCL already has a setup, loading a new one will replace it, are you sure you want lose current setup data?",
                        "Yes, load and replace", "Nope!"))
                    {
                        return false;
                    }
                }

                string fromPath = "Assets" + openPath.Substring(Application.dataPath.Length);
                string toPath = LocalizationSetup.setupPersistPath + Path.DirectorySeparatorChar +
                                LocalizationSetup.setupPersistName;
                bool copySuccess = AssetDatabase.CopyAsset(fromPath, toPath);
                if (!copySuccess)
                {
                    Debug.LogError("[Load Setup] Copy Setup Asset from <" + fromPath + "> to <" + toPath + "> failed");
                    return false;
                }
                else
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Save Current Setup Asset
        /// </summary>
        /// <returns></returns>
        public static bool SaveSetup()
        {
            string savePath = EditorUtility.SaveFilePanel("Save Setup Asset", Application.dataPath, "MySetup", "asset");

            if (savePath != null && !savePath.Equals(""))
            {
                if (savePath.IndexOf(Application.dataPath) != -1)
                {
                    string fromPath = LocalizationSetup.setupPersistPath + Path.DirectorySeparatorChar +
                                      LocalizationSetup.setupPersistName;
                    string toPath = "Assets" + savePath.Substring(Application.dataPath.Length);
                    bool copySuccess = AssetDatabase.CopyAsset(fromPath, toPath);
                    if (!copySuccess)
                    {
                        Debug.LogError("[Save Setup] Copy Setup Asset from <" + fromPath + "> to <" + toPath +
                                       "> failed");
                        return false;
                    }
                    else
                        return true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Save error",
                        "You must save your setup in the Assets Folder of your project", "OK");
                }
            }

            return false;
        }

        /// <summary>
        /// Create or replace an asset, depending on its existence.
        /// Code from http://answers.unity3d.com/questions/24929/assetdatabase-replacing-an-asset-but-leaving-refer.html
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T CreateOrReplaceAsset<T>(T asset, string path) where T : Object
        {
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                existingAsset = asset;
            }
            else
            {
                EditorUtility.CopySerialized(asset, existingAsset);
            }

            return existingAsset;
        }


        public static void GetAllPrefabsPathFromPath(string path, List<string> allPrefabsPaths)
        {
            string[] newPrefabsPaths = DataUtils.GetPrefabsPathInPath(path);
            if (newPrefabsPaths != null && newPrefabsPaths.Length > 0)
            {
                foreach (string prefabPath in newPrefabsPaths)
                {
                    allPrefabsPaths.Add(prefabPath);
                }
            }

            string[] dirEntries = Directory.GetDirectories(path);

            if (dirEntries == null || dirEntries.Length == 0)
            {
                return;
            }
            else
            {
                foreach (string dir in dirEntries)
                {
                    GetAllPrefabsPathFromPath(dir, allPrefabsPaths);
                }
            }
        }

        public static string[] GetPrefabsPathInPath(string path)
        {
            ArrayList al = new ArrayList();
            string[] fileEntries = Directory.GetFiles(path);
            foreach (string fileName in fileEntries)
            {
                int index = fileName.LastIndexOfAny(new char[] {'/', '\\'});
                string localPath = path;
                localPath = "Assets" + localPath.Substring(Application.dataPath.Length);
                if (index > 0)
                {
                    string fileAssetName = fileName.Substring(index);
                    localPath += fileAssetName;
                }

                localPath = localPath.Replace("//", "/");
                al.Add(localPath);
            }

            string[] result = new string[al.Count];
            for (int i = 0; i < al.Count; i++)
                result[i] = (string) al[i];

            return result;
        }

        public static GameObject GetPrefabFromPath(string prefabPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            return (GameObject) obj;
        }

        /// <summary>
        /// Get all prefabs in given directory path recursively
        /// Result is updated in allPrefabs param.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="allPrefabs"></param>
        public static void GetAllPrefabsFromPath(string path, List<GameObject> allPrefabs)
        {
            GameObject[] newPrefabs = DataUtils.GetPrefabsInPath(path);
            if (newPrefabs != null && newPrefabs.Length > 0)
            {
                foreach (GameObject prefab in newPrefabs)
                {
                    allPrefabs.Add(prefab);
                }
            }

            string[] dirEntries = Directory.GetDirectories(path);

            if (dirEntries == null || dirEntries.Length == 0)
            {
                return;
            }
            else
            {
                foreach (string dir in dirEntries)
                {
                    GetAllPrefabsFromPath(dir, allPrefabs);
                }
            }
        }

        /// <summary>
        /// Returns Prefabs in the given directory path only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GameObject[] GetPrefabsInPath(string path)
        {
            ArrayList al = new ArrayList();
            string[] fileEntries = Directory.GetFiles(path);
            foreach (string fileName in fileEntries)
            {
                int index = fileName.LastIndexOfAny(new char[] {'/', '\\'});
                string localPath = path;
                localPath = "Assets" + localPath.Substring(Application.dataPath.Length);
                if (index > 0)
                {
                    string fileAssetName = fileName.Substring(index);
                    localPath += fileAssetName;
                }

                localPath = localPath.Replace("//", "/");
                Object obj = AssetDatabase.LoadAssetAtPath(localPath, typeof(GameObject));

                if (obj != null)
                    al.Add(obj);
            }

            GameObject[] result = new GameObject[al.Count];
            for (int i = 0; i < al.Count; i++)
                result[i] = (GameObject) al[i];

            return result;
        }

        /// <summary>
        /// Utility method from : https://answers.unity.com/questions/1425758/how-can-i-find-all-instances-of-a-scriptable-objec.html
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAllInstances<T>(string pathFilter) where T : ScriptableObject
        {
            string[]
                guids = AssetDatabase.FindAssets("t:" + typeof(T)
                    .Name); //FindAssets uses tags check documentation for more info
            T[] a = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++) //probably could get optimized 
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string localPathFilter = "Assets" + pathFilter.Substring(Application.dataPath.Length);
                if (path.StartsWith(localPathFilter))
                    a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return a;
        }

        /// <summary>
        /// splits a CSV row 
        /// Code from http://answers.unity3d.com/questions/144200/are-there-any-csv-reader-for-unity3d-without-needi.html
        /// </summary>
        /// <param name="line">The line to split</param>
        /// <param name="useSemiColonSeparator">Use a semi-colon ";" as column separator instead of colon ","</param>
        /// <returns></returns>
        public static string[] SplitCsvLine(string line, bool useSemiColonSeparator = false)
        {
            string pattern;
            if (useSemiColonSeparator)
            {
                pattern = @"(?:^""|;"")(""""|[\w\W]*?)(?="";|""$)|(?:^(?!"")|;(?!""))([^;]*?)(?=$|;)|(\r\n|\n)";
            }
            else
            {
                pattern = @"(?:^""|,"")(""""|[\w\W]*?)(?="",|""$)|(?:^(?!"")|,(?!""))([^,]*?)(?=$|,)|(\r\n|\n)";
            }

            // MatchCollection matches = pattern.Matches(line);
            MatchCollection matches = Regex.Matches(line, pattern,
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            List<string> values = new List<string>();
            for (var i=0; i<matches.Count; i++)
            {
                var m = matches[i];
                var selectedValue = "";
                for (var j = 0; j < m.Groups.Count; j++)
                {
                    if (m.Groups[j].Value != "")
                    {
                        selectedValue = m.Groups[j].Value;
                    }
                }
                values.Add(selectedValue);
            }
            return values.ToArray();
        }
    }
}