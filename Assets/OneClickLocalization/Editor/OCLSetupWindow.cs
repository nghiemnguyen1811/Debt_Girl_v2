using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OneClickLocalization.Components;
using OneClickLocalization.Editor.Utils;
using OneClickLocalization.Editor.UtilityWindows;

namespace OneClickLocalization.Editor
{
    /// <summary>
    /// Custom Editor window displaying OCL Setup
    /// </summary>
    public class OCLSetupWindow : EditorWindow
    {
        private const string windowTitle = "OCL - Setup";

        Vector2 windowScrollPosition = new Vector2();

        private bool showSetupSettings = false;

        private SystemLanguage addLanguageValue;

        private GUIStyle mainButtonStyle;

        private Dictionary<SystemLanguage, int> idsTranslatedCount = new Dictionary<SystemLanguage, int>();
        private int idsCount = -1;

        public static OCLSetupWindow ShowWindow()
        {
            OCLSetupWindow window = (OCLSetupWindow) EditorWindow.GetWindow(typeof(OCLSetupWindow), false, windowTitle);
            return window;
        }

        void OnEnable()
        {
        }

        void OnGUI()
        {
            mainButtonStyle = new GUIStyle(GUI.skin.button);
            mainButtonStyle.normal.textColor = Color.white;
            mainButtonStyle.fontStyle = FontStyle.Bold;
            mainButtonStyle.hover.textColor = Color.white;
            mainButtonStyle.active.textColor = Color.white;

            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);

            EditorGUILayout.Separator();

            CreateSaveLoad();

            EditorGUILayout.Separator();

            CreateParameters();

            EditorGUILayout.Separator();

            CreateAutomaticSetup();

            EditorGUILayout.Separator();

            CreateLanguages();

            EditorGUILayout.Separator();

            CreateTranslatorConfig();

            EditorGUILayout.Separator();

            CreateReset();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void CreateSaveLoad()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Setup"))
            {
                if (DataUtils.SaveSetup())
                {
                    EditorUtility.DisplayDialog("Save success", "Setup saved", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Save failed", "An error occured during save process", "OK");
                }
            }

            if (GUILayout.Button("Load Setup"))
            {
                if (DataUtils.LoadSetup())
                {
                    EditorUtility.DisplayDialog("Load success", "New setup loaded", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Load failed", "An error occured during load process", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateParameters()
        {
            // Language selection
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);


            DataManager.editorSetup.active = EditorGUILayout.Toggle(
                new GUIContent("Activate Localization",
                    "If unchecked, One Click Localization will have no effect on the project"),
                DataManager.editorSetup.active);

            EditorGUILayout.Separator();

            DataManager.editorSetup.forceLanguage =
                !(EditorGUILayout.Toggle("Use system's language", !DataManager.editorSetup.forceLanguage));

            GUI.enabled = DataManager.editorSetup.forceLanguage;
            DataManager.editorSetup.forcedLanguage =
                (SystemLanguage) EditorGUILayout.EnumPopup("Force language", DataManager.editorSetup.forcedLanguage);
            GUI.enabled = true;

            DataManager.editorSetup.useDefaultLanguageForNullValues = EditorGUILayout.Toggle(
                new GUIContent("Default language for nulls",
                    "If a translation has a null value, default language value will be returned by API"),
                DataManager.editorSetup.useDefaultLanguageForNullValues);

            EditorGUILayout.EndVertical();
        }

        private void CreateSetupSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

            // Add OCLComponentAdapter 
            DataManager.addAdapters = EditorGUILayout.Toggle(
                new GUIContent("Add OCL component",
                    "If checked, OCLComponentAdapter component will be added to objects with supported comps, making the object localization ready."),
                DataManager.addAdapters);

            // Extract comp data
            DataManager.extractLocalizationData = EditorGUILayout.Toggle(
                new GUIContent("Extract data",
                    "If checked, supported types are extracted from object components to the localization list."),
                DataManager.extractLocalizationData);

            // Managed types 
            string[] supportedTypesNames = new string[DataManager.supportedCompsList.Length];
            for (int i = 0; i < supportedTypesNames.Length; i++)
            {
                supportedTypesNames[i] = DataManager.supportedCompsList[i].ToString();
            }

            DataManager.selectedSupportedTypes = EditorGUILayout.MaskField(
                new GUIContent("Supported types", "Select types to be handled by the setup process."),
                DataManager.selectedSupportedTypes, supportedTypesNames);

            EditorGUILayout.Separator();

            // Include / Exclude Components
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            // manual indentation for Button... kinda sucks but no other solution found...
            GUILayout.Space(150);
            if (GUILayout.Button("Configure Includes/Excludes", GUILayout.ExpandWidth(false)))
            {
                OCLSetupCompsIncludeExcludeWindow.ShowWindow();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            // Scenes 
            DataManager.parseScenes = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Parse Scene objects",
                    "If checked, selected scenes will be parsed during setup process."), DataManager.parseScenes);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Only scenes from the build settings 'Scenes in build' list are available.",
                EditorStyles.helpBox);

            var allScenes = EditorBuildSettings.scenes;
            var scenesNames = new List<string>();
            if (allScenes.Length > 0)
            {
                foreach (var scene in allScenes)
                {
                    if (!scene.enabled)
                        continue;
                    scenesNames.Add(scene.path.Substring(scene.path.LastIndexOf("/") + 1));
                }
            }

            if (scenesNames.Count > 0)
            {
                DataManager.selectedScenes =
                    EditorGUILayout.MaskField("Selected scenes", DataManager.selectedScenes, scenesNames.ToArray());
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "There are no scene configured to be parsed (or the scenes are disabled). Go to 'Build Settings' and add the scenes you want to parse in 'Scenes In Build'",
                    MessageType.Warning);
            }

            GUIContent inactiveGUIContent = new GUIContent("Parse inactives",
                "If checked, inactive objects in scenes will be used for the setup process");
            DataManager.parseInactivesInScenes =
                EditorGUILayout.Toggle(inactiveGUIContent, DataManager.parseInactivesInScenes);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Separator();

            // Prefabs 
            DataManager.parsePrefabs = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Parse Prefab assets", "If checked, prefab assets will be parsed during setup process."),
                DataManager.parsePrefabs);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Prefabs are parsed from the Assets folder.", EditorStyles.helpBox);

            DataManager.prefabsPath = EditorGUILayout.TextField("Assets subpath : ", DataManager.prefabsPath);
            bool prefabsPathExist = Directory.Exists(Application.dataPath + DataManager.prefabsPath);
            if (!prefabsPathExist)
            {
                EditorGUILayout.LabelField("WARNING : The path does not exist.", EditorStyles.helpBox);
            }

            inactiveGUIContent = new GUIContent("Parse inactives",
                "If checked, inactive objects in prefabs will be used for the setup process");
            DataManager.parseInactivesInPrefabs =
                EditorGUILayout.Toggle(inactiveGUIContent, DataManager.parseInactivesInPrefabs);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Separator();

            // ScriptableObjects 
            DataManager.parseScriptableObjects = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Parse ScriptableObject assets",
                    "If checked, scriptableObjects fields and properties will be parsed for extraction only during process."),
                DataManager.parseScriptableObjects);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                "ScriptableObjects are only parsed for data extraction from their fields and properties.",
                EditorStyles.helpBox);

            DataManager.scriptableObjectPath =
                EditorGUILayout.TextField("Assets subpath : ", DataManager.scriptableObjectPath);
            bool scriptableObjectPathExist = Directory.Exists(Application.dataPath + DataManager.scriptableObjectPath);
            if (!scriptableObjectPathExist)
            {
                EditorGUILayout.LabelField("WARNING : The path does not exist.", EditorStyles.helpBox);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Separator();


            EditorGUILayout.EndVertical();
        }

        private void CreateLanguages()
        {
            GUILayout.Label("Languages", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            SystemLanguage modifiedDefaultLanguage = (SystemLanguage) EditorGUILayout.EnumPopup(
                new GUIContent("Default", "The language used before applying any localization"),
                DataManager.editorSetup.defaultLanguage);

            if (modifiedDefaultLanguage != DataManager.editorSetup.defaultLanguage)
            {
                if (DataManager.editorSetup.languages.Contains(modifiedDefaultLanguage))
                {
                    EditorUtility.DisplayDialog("Change default language",
                        "You can't specify a default language that is used.\n\nRemove " + modifiedDefaultLanguage +
                        " to use it as default language.", "OK");
                }
                else
                {
                    // Valid modification
                    DataManager.editorSetup.SetDefaultLanguage(modifiedDefaultLanguage);
                }
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (DataManager.editorSetup.languages.Count == 0)
            {
                EditorGUILayout.LabelField("No language selected yet. Please add one.");
            }
            else
            {
                // Language line
                for (int i = DataManager.editorSetup.languages.Count - 1; i >= 0; i--)
                {
                    SystemLanguage lang = (SystemLanguage) DataManager.editorSetup.languages[i];
                    EditorGUILayout.BeginHorizontal();

                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(lang.ToString(), GUILayout.Width(120), GUILayout.Height(25));

                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                    string idsTranslatedForLang = "-";
                    if (idsTranslatedCount.ContainsKey(lang))
                    {
                        idsTranslatedForLang = idsTranslatedCount[lang].ToString();
                    }
                    else
                    {
                        idsTranslatedForLang = "0";
                    }

                    GUILayout.Label(idsTranslatedForLang + "/" + idsCount + " translated", GUILayout.MinWidth(120),
                        GUILayout.Height(25));
                    if (GUILayout.Button(DataManager.closeIconTex, GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog("Remove language",
                            "Are you sure you want remove " + lang.ToString() +
                            " language?\nYou'll lose all localizations for this language!", "Do it! YES!",
                            "Wow NO! Stop!"))
                        {
                            DataManager.editorSetup.RemoveLanguageAndPersist(lang);
                            UpdateTranslatedStats();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            addLanguageValue =
                (SystemLanguage) EditorGUILayout.EnumPopup(addLanguageValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Add", GUILayout.Width(100)))
            {
                if (DataManager.editorSetup.languages.Contains(addLanguageValue))
                {
                    EditorUtility.DisplayDialog("Add language", "It's already there!", "OK");
                }
                else if (DataManager.editorSetup.defaultLanguage.Equals(addLanguageValue))
                {
                    EditorUtility.DisplayDialog("Add language", "You can't add your default language.", "OK");
                }
                else
                {
                    // Valid submission
                    DataManager.editorSetup.AddLanguageAndPersist(addLanguageValue);
                    UpdateTranslatedStats();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();


            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;

            if (GUILayout.Button(new GUIContent(" Edit Localizations", DataManager.editLocalizationsIconTex),
                mainButtonStyle, GUILayout.Height(32), GUILayout.Width(300)))
            {
                OCLMenu.ShowLocalizationWindow();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = defaultBgColor;

            EditorGUILayout.EndVertical();
        }

        private void CreateAutomaticSetup()
        {
            GUILayout.Label("Automatic Setup", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;

            GUI.enabled = ((DataManager.addAdapters || DataManager.extractLocalizationData) &&
                           (DataManager.parsePrefabs || DataManager.parseScenes))
                          || (DataManager.extractLocalizationData && DataManager.parseScriptableObjects);

            GUIContent setupGUIContent = new GUIContent();
            // make sure interface is readable even without images
            setupGUIContent.text = " Start automatic setup";
            setupGUIContent.image = DataManager.setupIconTex;
            setupGUIContent.tooltip = "Automatically setup your project, use setting below to configure the process";

            if (GUILayout.Button(setupGUIContent, mainButtonStyle, GUILayout.Height(50), GUILayout.Width(300),
                GUILayout.MinWidth(100)))
            {
                ParseResult res = Parse(DataManager.parseScenes, DataManager.parsePrefabs,
                    DataManager.parseScriptableObjects,
                    DataManager.extractLocalizationData, DataManager.addAdapters, false);
                if (res != null)
                {
                    string comps = (res.componentsAdded + res.componentsAlreadyExist) + " supported components found.";
                    string extractedStr = res.stringsExtracted + " strings extracted (" + res.stringsDuplicates +
                                          " duplicates skipped)";
                    string extractedSprites = res.spritesExtracted + " Images(Sprite) extracted (" +
                                              res.spritesDuplicates + " duplicates skipped)";
                    string extractedTextures = res.texturesExtracted + " Images(Texture) extracted (" +
                                               res.texturesDuplicates + " duplicates skipped)";
                    string extractedAudioClips = res.audioClipExtracted + " Audio(AudioClip) extracted (" +
                                                 res.audioClipDuplicates + " duplicates skipped)";
                    string setupComps = res.componentsAdded + " OCLComponentAdapter added (" +
                                        res.componentsAlreadyExist + " already present)";
                    EditorUtility.DisplayDialog("Setup Project Result",
                        res.scenesParsed + " scenes parsed\n" + res.prefabsParsed + " prefabs parsed" + "\n"
                        + res.scriptableObjectsParsed + " ScriptableObject parsed\n" + "\n" +
                        comps + "\n\n" + extractedStr + "\n" + extractedSprites + "\n" + extractedTextures + "\n" +
                        extractedAudioClips + "\n\n" + setupComps, "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Setup Project Result", "Operation cancelled", "OK");
                }
            }

            GUI.enabled = true;

            GUI.backgroundColor = defaultBgColor;

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (EditorBuildSettings.scenes.Length == 0 && DataManager.parseScenes)
            {
                EditorGUILayout.HelpBox(
                    "There are no scene configured to be parsed. Go to 'Build Settings' and add the scenes you want to parse in 'Scenes In Build'",
                    MessageType.Warning);
            }

            showSetupSettings = GUIUtils.Foldout(showSetupSettings, "Setup Settings", true, EditorStyles.foldout);
            if (showSetupSettings)
            {
                EditorGUI.indentLevel++;
                CreateSetupSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateTranslatorConfig()
        {
            GUILayout.Label("Microsoft Translator", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            EditorGUILayout.SelectableLabel("Uses Azure API accounts. Documentations :");
            EditorGUILayout.SelectableLabel(
                "https://docs.microsoft.com/en-us/azure/cognitive-services/translator/translator-text-how-to-signup");

            string newKey = EditorGUILayout.TextField("Client key", DataManager.translatorKey);

            /* No more free infinite account so no more default account
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(150);
            if (GUILayout.Button(new GUIContent("Set default credentials", "Default OCL account for Azure API Translator is public, so you may encounter \"Quota reached error\", use your own."), GUILayout.ExpandWidth(false)))
            {
                GUI.FocusControl(null);
                newKey = DataManager.defaultTranslatorKey;
            }
            EditorGUILayout.EndHorizontal();
            */

            DataManager.translatorKey = newKey;

            EditorGUILayout.EndVertical();

            if (DataManager.translatorKey != newKey)
            {
                DataManager.translator.SetCredentials(newKey);
            }
        }

        private void CreateReset()
        {
            GUILayout.Label("Reset", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            if (GUILayout.Button(
                new GUIContent("Reset OCL",
                    "Removes all OCLComponentAdapters from scenes and prefabs, reset localizations and languages"),
                GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Reset OCL",
                    "OneClickLocalization will reset to its default configuration.\n   - All OCLComponentAdapter will be searched and removed from all scenes and prefabs objects.\n   - All localizations will be deleted\n\n Are you sure?",
                    "Yeah !", "Are you crazy?! Stop, you fool!"))
                {
                    if (EditorUtility.DisplayDialog("Reset OCL - Miss click security", "100% sure? No regrets?",
                        "Yes, reset all", "No, don't do anything"))
                    {
                        GUI.FocusControl(null);
                        ParseResult result = Parse(true, true, false, false, false, true);
                        if (result != null)
                        {
                            DestroyImmediate(DataManager.editorSetup, true);
                            DataManager.LoadSetup();

                            DataManager.ResetEditorPrefs();

                            EditorUtility.DisplayDialog("Reset Result",
                                result.componentsRemoved + " OCLComponentAdapter removed\n\nSetup reseted to default.",
                                "OK");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Reset Result", "Operation cancelled", "OK");
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private ParseResult Parse(bool parseScenes, bool parsePrefabs, bool parseScriptableObjects, bool extractData,
            bool addAdapters, bool reset)
        {
            ParseResult res = new ParseResult();

            EditorUtility.DisplayProgressBar("Action progress", "Start parsing...", 0.0f);

            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return null;
                }

                // define the list of scenes to parse
                EditorBuildSettingsScene[] allScenes = parseScenes ? EditorBuildSettings.scenes : null;
                List<EditorBuildSettingsScene> scenesToParse = new List<EditorBuildSettingsScene>();
                if (parseScenes)
                {
                    if (DataManager.selectedScenes == -1)
                    {
                        for (int i = 0; i < allScenes.Length; i++)
                        {
                            scenesToParse.Add(allScenes[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < allScenes.Length; i++)
                        {
                            EditorBuildSettingsScene scene = allScenes[i];
                            int layer = 1 << i;
                            if ((DataManager.selectedScenes & layer) != 0)
                            {
                                scenesToParse.Add(scene);
                            }
                        }
                    }
                }

                // List<GameObject> allPrefabs = new List<GameObject>();
                List<string> allPrefabsPath = new List<string>();
                if (parsePrefabs)
                {
                    // DataUtils.GetAllPrefabsFromPath(Application.dataPath + DataManager.prefabsPath, allPrefabs);
                    DataUtils.GetAllPrefabsPathFromPath(Application.dataPath + DataManager.prefabsPath, allPrefabsPath);
                }

                // define the list of types to handle
                List<Type> supportedSelectedTypes = new List<Type>();
                for (int i = 0; i < DataManager.supportedCompsList.Length; i++)
                {
                    Type type = DataManager.supportedCompsList[i];
                    int layer = 1 << i;
                    // If Reset (removeAdapters), then select all types available
                    if ((DataManager.selectedSupportedTypes & layer) != 0)
                    {
                        supportedSelectedTypes.Add(type);
                    }
                }

                int progressStep = 1;
                // if (allPrefabs.Count > 0 || scenesToParse.Count > 0)
                // {
                //     progressStep = 1 / ((parseScenes ? allScenes.Length : 0) + (parsePrefabs ? allPrefabs.Count : 0));
                // }

                if (allPrefabsPath.Count > 0 || scenesToParse.Count > 0)
                {
                    progressStep =
                        1 / ((parseScenes ? allScenes.Length : 0) + (parsePrefabs ? allPrefabsPath.Count : 0));
                }


                float progressStatus = 0.0f;

                if (parsePrefabs)
                {
                    //foreach (GameObject prefab in allPrefabs)
                    foreach (string prefabPath in allPrefabsPath)
                    {
                        GameObject prefab = DataUtils.GetPrefabFromPath(prefabPath);
                        if (prefab == null)
                            continue;

                        // Debug.Log("Prefab path searched : " + prefabPath);

                        EditorUtility.DisplayProgressBar("Setup progress", "Parse prefab : " + prefab.name,
                            progressStatus + progressStep);
                        res.prefabsParsed++;

                        Debug.Log("Prefab parsed : " + prefab.ToString());

                        Component[] components = prefab.GetComponentsInChildren<Component>(true);
                        foreach (Component comp in components)
                        {
                            if (comp == null)
                                continue;

                            GameObject compGameObject = comp.gameObject;
                            if (compGameObject == null || compGameObject.hideFlags == HideFlags.NotEditable ||
                                compGameObject.hideFlags == HideFlags.HideAndDontSave)
                                continue;

                            // Reset option, just remove OCLComponentAdapter whatever the host is
                            if (reset)
                            {
                                RemoveObjectAdapter(compGameObject, res);
                                continue;
                            }

                            // Ignore inactive if needed
                            if (!DataManager.parseInactivesInPrefabs && !compGameObject.activeSelf)
                                continue;

                            if (!IsCompSupported(comp, supportedSelectedTypes))
                            {
                                continue;
                            }

                            // Add new component
                            if (addAdapters)
                            {
                                AddObjectAdapter(compGameObject, res);
                            }

                            // Extract strings
                            if (extractData)
                            {
                                ExtractCompData(comp, res);
                            }
                        }
                    }
                }

                // Scriptable object are valid option only for data extraction
                if (parseScriptableObjects && extractData && !reset)
                {
                    // Get all scriptable objects filtering with the path
                    foreach (var so in
                        DataUtils.GetAllInstances<ScriptableObject>(Application.dataPath +
                                                                    DataManager.scriptableObjectPath))
                    {
                        if (so == null)
                            continue;

                        res.scriptableObjectsParsed++;

                        ExtractScriptableObjectData(so, res);
                    }
                }

                if (parseScenes)
                {
                    string currentScenePath = EditorSceneManager.GetActiveScene().path;
                    foreach (EditorBuildSettingsScene scene in scenesToParse)
                    {
                        EditorUtility.DisplayProgressBar("Setup progress", "Parse scene : " + scene.path,
                            progressStatus + progressStep);

                        res.scenesParsed++;

                        Debug.Log("Scene parsed : " + scene.path);

                        EditorSceneManager.OpenScene(scene.path);

                        // Get all components everywhere, including inactives
                        foreach (Component comp in Resources.FindObjectsOfTypeAll(typeof(Component)) as Component[])
                        {
                            if (comp == null)
                                continue;

                            GameObject compGameObject = comp.gameObject;
                            if (compGameObject == null || compGameObject.hideFlags == HideFlags.NotEditable ||
                                compGameObject.hideFlags == HideFlags.HideAndDontSave)
                                continue;

                            // Ignore inactive if needed
                            if (!DataManager.parseInactivesInScenes && !compGameObject.activeSelf)
                                continue;

                            // Check if object holding component is a prefab
                            if (EditorUtility.IsPersistent(compGameObject.transform.root.gameObject))
                                continue;


                            // Reset option, just remove OCLComponentAdapter whatever the host is
                            if (reset)
                            {
                                RemoveObjectAdapter(compGameObject, res);
                                continue;
                            }

                            if (!IsCompSupported(comp, supportedSelectedTypes))
                                continue;

                            // Add new component
                            if (addAdapters)
                            {
                                AddObjectAdapter(compGameObject, res);
                            }

                            // Extract strings
                            if (extractData)
                            {
                                ExtractCompData(comp, res);
                            }
                        }

                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        EditorSceneManager.SaveOpenScenes();
                    }

                    // Reopen the initial scene... like nothing happened ! Wouhou ! magic boy, just magic ! impressed ? ;)
                    EditorSceneManager.OpenScene(currentScenePath);
                }

                EditorUtility.DisplayProgressBar("Setup progress", "Save data", 1.0f);

                DataManager.PersistSetup();
            }
            catch (Exception e)
            {
                Debug.LogError("Exception during parsing process : " + e.Message);
                throw;
            }
            finally
            {
                // Let's make sure we don't let user with a crashed progressBar on screen
                EditorUtility.ClearProgressBar();
            }

            return res;
        }


        private bool IsCompSupported(Component comp, List<Type> selectedSupportedTypes)
        {
            if (comp == null || selectedSupportedTypes == null || selectedSupportedTypes.Count == 0)
                return false;

            string[] includeTypes =
                DataManager.parseIncludeComps.Split(DataManager.includeExcludeCompsSeparator.ToCharArray());
            string[] excludeTypes =
                DataManager.parseExcludeComps.Split(DataManager.includeExcludeCompsSeparator.ToCharArray());


            // Exclude / Include types, no regex here, just string contains test for simplicity
            Type compType = comp.GetType();

            bool foundInIncludes = true;
            if (includeTypes != null && includeTypes.Length > 0)
            {
                foundInIncludes = false;
                foreach (string includeType in includeTypes)
                {
                    if (includeType != null && !includeType.Equals("") && comp.gameObject != null)
                    {
                        // If the component type is NOT in the include list, it should be skipped
                        if ((compType.FullName.Contains(includeType)))
                        {
                            foundInIncludes = true;

                            break;
                        }
                    }
                }
            }

            if (!foundInIncludes)
            {
                return false;
            }

            Type loopObjectCompType = null;
            if (excludeTypes != null && excludeTypes.Length > 0)
            {
                // Adpater is added on object, so if only one of the components is not supported, we should not add the adapter
                foreach (Component objectComp in comp.gameObject.GetComponents<Component>())
                {
                    foreach (string excludeType in excludeTypes)
                    {
                        if (excludeType != null && !excludeType.Equals("") && comp.gameObject != null)
                        {
                            if (objectComp != null)
                            {
                                loopObjectCompType = objectComp.GetType();
                                // If the component type IS in the exclude list, it should be skipped
                                if (loopObjectCompType != null && (loopObjectCompType.FullName.Contains(excludeType)))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            PropertyInfo prop = null;
            // Search for the right property
            if (selectedSupportedTypes.Contains(typeof(string)))
            {
                prop = compType.GetProperty("text");
                if (prop != null && prop.PropertyType.Equals(typeof(string)))
                {
                    return true;
                }
            }

            if (selectedSupportedTypes.Contains(typeof(Texture)))
            {
                prop = compType.GetProperty("texture");
                if (prop != null && prop.PropertyType.Equals(typeof(Texture)))
                {
                    return true;
                }
            }

            if (selectedSupportedTypes.Contains(typeof(Sprite)))
            {
                prop = compType.GetProperty("sprite");
                if (prop != null && prop.PropertyType.Equals(typeof(Sprite)))
                {
                    return true;
                }
            }

            if (selectedSupportedTypes.Contains(typeof(AudioClip)))
            {
                prop = compType.GetProperty("clip");
                if (prop != null && prop.PropertyType.Equals(typeof(AudioClip)))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddObjectAdapter(GameObject go, ParseResult parseResult)
        {
            if (go.GetComponent<OCLComponentAdapter>() == null)
            {
                parseResult.componentsAdded++;
                go.AddComponent<OCLComponentAdapter>();
            }
            else
            {
                parseResult.componentsAlreadyExist++;
            }
        }

        private void RemoveObjectAdapter(GameObject go, ParseResult parseResult)
        {
            OCLComponentAdapter adapter = go.GetComponent<OCLComponentAdapter>();
            if (adapter != null)
            {
                parseResult.componentsRemoved++;
                DestroyImmediate(adapter, true);
            }
        }

        private void ExtractScriptableObjectData(ScriptableObject so, ParseResult parseResult)
        {
            if (so == null)
                return;

            var objectType = so.GetType();

            Debug.Log("Parsing ScriptableObject : " + objectType);

            // Parse fields of the type
            RecursiveParseFields(so, parseResult);

            // Parse properties, but only the final type properties, ignore parents as we are getting 
            // many scriptableObject properties we don't want
            var properties = objectType.GetProperties(System.Reflection.BindingFlags.Public
                                                      | System.Reflection.BindingFlags.Instance
                                                      | System.Reflection.BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string)) continue;
                var newId = (string) prop.GetValue(so);
                // Debug.Log("Property : " + prop.Name + " = " + newId);
                AddIdToDataManager(newId, parseResult);
            }
        }

        [SerializeField]
        private HashSet<Type> nonRecursiveTypes = new HashSet<Type> {typeof(int), typeof(float), typeof(bool)};

        private void RecursiveParseFields<T>(T obj, ParseResult parseResult)
        {
            foreach (var field in obj.GetType().GetFields())
            {
                if (nonRecursiveTypes.Contains(field.FieldType)) continue;

                if (field.FieldType == typeof(string))
                {
                    AddIdToDataManager((string) field.GetValue(obj), parseResult);
                }
                else if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    foreach (var item in (IList) field.GetValue(obj))
                    {
                        RecursiveParseFields(item, parseResult);
                    }
                }
                else
                {
                    RecursiveParseFields(field, parseResult);
                }
            }
        }

        private void ExtractCompData(Component comp, ParseResult parseResult)
        {
            object newId = null;

            PropertyInfo prop = comp.GetType().GetProperty("text");
            if (prop != null && prop.PropertyType.Equals(typeof(string)))
            {
                newId = prop.GetValue(comp, null);
            }

            prop = comp.GetType().GetProperty("texture");
            if (prop != null && prop.PropertyType.Equals(typeof(Texture)))
            {
                newId = prop.GetValue(comp, null);
                if ((Texture) newId == null)
                    newId = null;
            }

            prop = comp.GetType().GetProperty("sprite");
            if (prop != null && prop.PropertyType.Equals(typeof(Sprite)))
            {
                newId = prop.GetValue(comp, null);
                if ((Sprite) newId == null)
                    newId = null;
            }

            prop = comp.GetType().GetProperty("clip");
            if (prop != null && prop.PropertyType.Equals(typeof(AudioClip)))
            {
                newId = prop.GetValue(comp, null);
                if ((AudioClip) newId == null)
                    newId = null;
            }

            AddIdToDataManager(newId, parseResult);
        }

        private void AddIdToDataManager(object newId, ParseResult parseResult)
        {
            if (newId == null || newId.Equals("")) return;

            if (!DataManager.editorSetup.HasId(newId))
            {
                switch (newId)
                {
                    case string _:
                        parseResult.stringsExtracted++;
                        break;
                    case Sprite _:
                        parseResult.spritesExtracted++;
                        break;
                    case Texture _:
                        parseResult.texturesExtracted++;
                        break;
                    case AudioClip _:
                        parseResult.audioClipExtracted++;
                        break;
                }

                // Finally : add the new extracted id to localizations list
                DataManager.editorSetup.AddId(newId);
            }
            else
            {
                switch (newId)
                {
                    case string _:
                        parseResult.stringsDuplicates++;
                        break;
                    case Sprite _:
                        parseResult.spritesDuplicates++;
                        break;
                    case Texture _:
                        parseResult.texturesDuplicates++;
                        break;
                    case AudioClip _:
                        parseResult.audioClipDuplicates++;
                        break;
                }
            }
        }

        private void UpdateTranslatedStats()
        {
            idsTranslatedCount.Clear();

            List<object> allIds = DataManager.editorSetup.GetAllIds();
            idsCount = allIds.Count;

            foreach (object id in allIds)
            {
                foreach (SystemLanguage language in DataManager.editorSetup.languages)
                {
                    object localization = DataManager.editorSetup.GetLocalization(id, language);
                    if (localization != null)
                    {
                        if (idsTranslatedCount.ContainsKey(language))
                            idsTranslatedCount[language]++;
                        else
                            idsTranslatedCount[language] = 1;
                    }
                }
            }
        }

        void OnDestroy()
        {
            DataManager.PersistSetup();
            DataManager.SaveEditorPrefs();
        }

        void OnFocus()
        {
            DataManager.LoadSetup();
            DataManager.LoadEditorPrefs();

            UpdateTranslatedStats();
        }

        void OnLostFocus()
        {
            DataManager.PersistSetup();
            DataManager.SaveEditorPrefs();
        }
    }

    /// <summary>
    /// Class used to store parsing result during setup process
    /// </summary>
    class ParseResult
    {
        public int stringsExtracted = 0;
        public int spritesExtracted = 0;
        public int texturesExtracted = 0;
        public int audioClipExtracted = 0;
        public int componentsAdded = 0;
        public int componentsRemoved = 0;
        public int stringsTranslated = 0;
        public int stringsDuplicates = 0;
        public int spritesDuplicates = 0;
        public int texturesDuplicates = 0;
        public int audioClipDuplicates = 0;
        public int componentsAlreadyExist = 0;

        public int scenesParsed = 0;
        public int prefabsParsed = 0;
        public int scriptableObjectsParsed = 0;

        public ParseResult()
        {
        }
    }
}