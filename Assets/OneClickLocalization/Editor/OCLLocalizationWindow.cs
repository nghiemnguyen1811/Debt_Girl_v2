using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using OneClickLocalization.Editor.Utils;
using System.Linq;
using OneClickLocalization.Core.Localization;

namespace OneClickLocalization.Editor
{

    public class OCLLocalizationWindow : EditorWindow
    {
        private static readonly string windowTitle = "OCL - Localizations";

        // Separator used to construct controls names
        const string CONTROL_NAME_SEPARATOR = "|;|";

        // Tabs
        private const int STRING_TAB_ID = 0;
        private const int SPRITE_TAB_ID = 1;
        private const int TEXTURE_TAB_ID = 2;
        private const int AUDIOCLIP_TAB_ID = 3;
        private const string FILTER_SEARCH_CONTROL_NAME = "filter_search_textfield";
        private int selectedTab = 0;

        // GUI Styles
        private GUIStyle mainButtonStyle;
        private GUIStyle boxStyle;
        private GUIStyle headerBackgroundStyle;
        private GUIStyle headerLabelStyle;
        
        // Import/Export foldout
        private AnimBool showStringsImportExport;
        // Filters show/hide
        private bool showFiltersBar;

        // Filters
        private bool filterChanged = false;
        private bool filter_untranslatedOnly_applied = false;
        private int filter_languages_applied = -1;
        private string filter_search_applied = "";
        private bool filter_untranslatedOnly_input = false;
        private int filter_languages_input = -1;
        private string filter_search_input = "";

        // List of focus names that must be kept on display even if filter should hide it
        private HashSet<object> filter_tableIdWhiteList = new HashSet<object>();

        // pending list, storing items waiting for layout event
        private List<object> pendingIdsToAdd = new List<object>();
        private List<object> pendingIdsToRemove = new List<object>();
        private Dictionary<object, object> pendingIdsToReplace = new Dictionary<object, object>();
        private bool pendingFilterUnstranslated = false;
        private string pendingFilterSearch = "";
        private int pendingFilterLanguages = -1;

        // Table 
        private List<string> cachedAllStrings = new List<string>();
        private List<object> tableDataProvider = new List<object>();
        private bool dataProviderChanged = false;
        private Vector2 tableScrollPosition = new Vector2();
        private int currentRowHeight = -1;
        private int currentNumItems = -1;
        private int currentMaxNumItems = -1;
        private float currentScrollHeight = -1;
        private float currentLayoutScrollY = -1;

        private static OCLLocalizationWindow window;

        public static OCLLocalizationWindow ShowWindow()
        {
            window = (OCLLocalizationWindow)EditorWindow.GetWindow(typeof(OCLLocalizationWindow), false, windowTitle);
            return window;
        }

        void OnEnable()
        {
            showStringsImportExport = new AnimBool(false);
            showStringsImportExport.valueChanged.AddListener(RefreshWindow);
        }

        void Update()
        {
        }

        void RefreshWindow()
        {
            // Focus keeps old value active
            GUI.FocusControl(null);
            dataProviderChanged = true;
            ShowWindow().Repaint();
        }

        void OnGUI()
        {

            InitGUIStyles();

            ApplyLayoutModifications();

            CheckKeyboardInput();

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            EditorGUILayout.Space();

            CreateTableTabs();

            EditorGUILayout.EndVertical();
        }

        private void CheckKeyboardInput()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().Equals(FILTER_SEARCH_CONTROL_NAME))
            {
                ApplyFilter();
            }
            else if (Event.current.type == EventType.ValidateCommand)
            {
                switch (Event.current.commandName)
                {
                    // Undo Redo performed
                    case "UndoRedoPerformed": RefreshWindow();
                        break;
                }
            }
        }

        private void ApplyLayoutModifications()
        {
            // Modifications of data sources must be made only in layout event
            // If not : risk of desynchronization between layout and repaint
            if (Event.current.type == EventType.Layout)
            {

                // Prepare Undo Redo if needed
                if(pendingIdsToAdd.Count > 0 || pendingIdsToRemove.Count > 0 || pendingIdsToReplace.Count > 0)
                {

                    string undoRedoName = "Edit OCL Localization Ids";
                    if (pendingIdsToAdd.Count > 0)
                        undoRedoName = "OCL Add ids";
                    if (pendingIdsToRemove.Count > 0)
                        undoRedoName = "OCL Remove ids";
                    // Undo record
                    RecordUndoForAllLocalizations(undoRedoName);
                }
                // Add, remove Replace ids
                if (pendingIdsToReplace.Count > 0)
                {
                    foreach (object idToReplace in pendingIdsToReplace.Keys)
                    {
                        DataManager.editorSetup.ReplaceId(idToReplace, pendingIdsToReplace[idToReplace]);
                    }
                    pendingIdsToReplace.Clear();
                    dataProviderChanged = true;
                }
                if (pendingIdsToRemove.Count > 0)
                {
                    foreach (object removeId in pendingIdsToRemove)
                    {
                        DataManager.editorSetup.RemoveId(removeId);
                    }
                    pendingIdsToRemove.Clear();
                    GUI.FocusControl(null);
                    dataProviderChanged = true;
                }

                if (pendingIdsToAdd.Count > 0)
                {
                    foreach (object addId in pendingIdsToAdd)
                    {
                        DataManager.editorSetup.AddId(addId);
                        currentNumItems++;
                        currentMaxNumItems++;
                    }
                    pendingIdsToAdd.Clear();
                    ScrollToBottom();
                    GUI.FocusControl(null);
                    dataProviderChanged = true;
                }

                // Filters modifications
                if (pendingFilterUnstranslated != filter_untranslatedOnly_applied)
                {
                    filter_untranslatedOnly_applied = pendingFilterUnstranslated;
                    filterChanged = true;
                }
                if (pendingFilterSearch != filter_search_applied)
                {
                    filter_search_applied = pendingFilterSearch;
                    filterChanged = true;
                }
                if (pendingFilterLanguages != filter_languages_applied)
                {
                    filter_languages_applied = pendingFilterLanguages;
                    filterChanged = true;
                }
                
                // Filter changed, force refresh of tables by reseting the whiteList
                if (filterChanged)
                {
                    filter_tableIdWhiteList.Clear();
                    tableScrollPosition.y = 0;
                    // keep focus if user is still typing in search field
                    if (!GUI.GetNameOfFocusedControl().Equals(FILTER_SEARCH_CONTROL_NAME))
                        GUI.FocusControl(null);
                }

                if (filterChanged || dataProviderChanged)
                {
                    UpdateDataProvider();
                    dataProviderChanged = false;
                    filterChanged = false;
                }
            }
        }

        private void InitGUIStyles()
        {
            mainButtonStyle = new GUIStyle(GUI.skin.button);
            mainButtonStyle.normal.textColor = Color.white;
            mainButtonStyle.fontStyle = FontStyle.Bold;
            mainButtonStyle.hover.textColor = Color.white;
            mainButtonStyle.active.textColor = Color.white;

            boxStyle = new GUIStyle(GUI.skin.box);

            headerBackgroundStyle = new GUIStyle();
            headerBackgroundStyle.normal.background = GUIUtils.MakeTex(1, 1, DataManager.mainColor);

            headerLabelStyle = new GUIStyle(GUI.skin.label);
            headerLabelStyle.normal.textColor = Color.white;
            headerLabelStyle.fontStyle = FontStyle.Bold;
            headerLabelStyle.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateFiltersBar()
        {
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool isDefaultFilter = !IsFilterModified();
            EditorGUILayout.BeginHorizontal();
            showFiltersBar = GUIUtils.Foldout(showFiltersBar, "Filter ids", true, EditorStyles.foldout);

            if (!showFiltersBar)
            {
                GUI.enabled = !isDefaultFilter;
                if (GUILayout.Button("reset", GUILayout.Width(100)))
                {
                    ResetFilter();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            if (showFiltersBar)
            {
                EditorGUI.indentLevel++;

                // Untranslated
                filter_untranslatedOnly_input = EditorGUILayout.Toggle("Untranslated only", filter_untranslatedOnly_input);

                // Search
                GUI.SetNextControlName(FILTER_SEARCH_CONTROL_NAME);
                filter_search_input = EditorGUILayout.TextField("Search", filter_search_input);

                // Languages
                string[] languagesNames = new string[DataManager.editorSetup.languages.Count];
                for (int i = 0; i < DataManager.editorSetup.languages.Count; i++)
                {
                    languagesNames[i] = DataManager.editorSetup.languages[i].ToString();
                }
                filter_languages_input = EditorGUILayout.MaskField("Languages", filter_languages_input, languagesNames);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(150);
                if (GUILayout.Button("Apply", GUILayout.Width(100)))
                {
                    ApplyFilter();
                }
                GUI.enabled = !isDefaultFilter;
                if (GUILayout.Button("reset", GUILayout.Width(100)))
                {
                    ResetFilter();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();


                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyFilter()
        {
            pendingFilterUnstranslated = filter_untranslatedOnly_input;
            pendingFilterSearch = filter_search_input;
            pendingFilterLanguages = filter_languages_input;
            filterChanged = true;
        }

        private void ResetFilter()
        {
            pendingFilterUnstranslated = false;
            pendingFilterSearch = "";
            pendingFilterLanguages = -1;
            filter_untranslatedOnly_input = false;
            filter_search_input = "";
            filter_languages_input = -1;
            filterChanged = true;
            GUI.FocusControl(null);
        }

        private bool IsFilterModified()
        {
            return filter_untranslatedOnly_applied == true || !filter_search_applied.Equals("") || (filter_languages_applied != -1);
        }

        private void CreateTextActionsBar()
        {
            EditorGUILayout.BeginHorizontal();


            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;
            if (GUILayout.Button("Add String Id", mainButtonStyle, GUILayout.ExpandWidth(false)))
            {
                int newStringIndex = 1;
                string newId = "New String_" + newStringIndex;
                while (DataManager.editorSetup.HasId(newId))
                {
                    ++newStringIndex;
                    newId = "New String_" + newStringIndex;
                }
                pendingIdsToAdd.Add(newId);
            }
            GUI.backgroundColor = defaultBgColor;

            if (GUILayout.Button("Import/Export", GUILayout.ExpandWidth(false)))
            {
                showStringsImportExport.target = !showStringsImportExport.target;
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = DataManager.mainColor;
            if (GUILayout.Button(new GUIContent("Translate all", "Translate all active strings from current filter"), mainButtonStyle, GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Translate all strings", "This will translate all strings of the table, based on the filter. It will erase current values. Are you sure you want to continue?", "Oh yeah!", "Hum... let me check... stop here."))
                {
                    var languagesToTranslate = new List<SystemLanguage>();
                    for (int i = 0; i < DataManager.editorSetup.languages.Count; i++)
                    {
                        SystemLanguage lang = (SystemLanguage) DataManager.editorSetup.languages[i];
                        if (IsLanguageSelected(lang))
                        {
                            languagesToTranslate.Add(lang);
                        }
                    }

                    var idsToTranslate = new List<string>();
                    foreach (var id in tableDataProvider)
                    {
                        if (id is string)
                        {
                            idsToTranslate.Add(id as string);
                        }                        
                    }

                    if (idsToTranslate.Count > 0 && languagesToTranslate.Count > 0)
                    {
                        var translationResults = DataManager.translator.Translate(idsToTranslate.ToArray(), DataManager.editorSetup.defaultLanguage, languagesToTranslate.ToArray());
                        for (int i = 0; i < translationResults.translationResults.Count; i++)
                        {
                            var id = idsToTranslate[i];
                            
                            var translationResult = translationResults.translationResults[i];
                            foreach (var translation in translationResult.translations)
                            {
                                SystemLanguage translatedLanguage = LanguageUtils.GetLanguageFromCode(translation.to);
                                DataManager.editorSetup.SetLocalization(id, translatedLanguage, translation.text);
                            }       
                        }
                    }
                }
            }
            GUI.backgroundColor = defaultBgColor;

            if (GUILayout.Button("Remove all strings", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Remove all strings", "Are you sure you want delete all strings?\nYou'll lose all localizations of all languages (everything in other words...).", "Delete all YES !", "Of course NOT! Go back!"))
                {
                    foreach (object stringId in DataManager.editorSetup.GetIds<string>())
                    {
                        pendingIdsToRemove.Add(stringId);
                    }
                }
            }

            if (GUILayout.Button("Reset all localizations", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Reset all string localizations", "Are you sure you want delete all strings localizations?\nAll localizations will be set to null.", "Reset all YES !", "Of course NOT! Go back!"))
                {
                    RecordUndoForAllLocalizations("Reset OCL strings localizations");
                    foreach (string stringId in DataManager.editorSetup.GetIds<string>())
                    {
                        foreach (SystemLanguage lang in DataManager.editorSetup.languages)
                        {
                            DataManager.editorSetup.SetLocalization(stringId, lang, null);
                        }
                    }
                }
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateSpriteActionsBar()
        {
            EditorGUILayout.BeginHorizontal();


            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;
            if (GUILayout.Button("Add Sprite Id", mainButtonStyle, GUILayout.ExpandWidth(false)))
            {
                if (DataManager.editorSetup.HasId(DataManager.defaultSprite))
                {
                    EditorUtility.DisplayDialog("Duplicate item", "There is already a default sprite entry, duplicates not allowed.\n\nPlease modify the existing one.", "OK");
                }
                else
                {
                    pendingIdsToAdd.Add(DataManager.defaultSprite);
                }
            }
            GUI.backgroundColor = defaultBgColor;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Remove all sprites", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Remove all Sprite ids", "Are you sure you want delete all Sprites?\nYou'll lose all localizations of all languages (everything in other words...).", "Delete all YES !", "Of course NOT! Go back!"))
                {
                    foreach (Sprite spriteId in DataManager.editorSetup.GetIds<Sprite>())
                    {
                        pendingIdsToRemove.Add(spriteId);
                    }
                }
            }

            if (GUILayout.Button("Reset all localizations", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Reset all Sprite localizations", "Are you sure you want delete all Sprite localizations?\nAll localizations will be set to null.", "Reset all YES !", "Of course NOT! Go back!"))
                {
                    RecordUndoForAllLocalizations("Reset OCL Sprite localizations");
                    foreach (Sprite spriteId in DataManager.editorSetup.GetIds<Sprite>())
                    {
                        foreach (SystemLanguage lang in DataManager.editorSetup.languages)
                        {
                            DataManager.editorSetup.SetLocalization(spriteId, lang, null);
                        }
                    }
                }
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateTextureActionsBar()
        {
            EditorGUILayout.BeginHorizontal();


            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;
            if (GUILayout.Button("Add Texture Id", mainButtonStyle, GUILayout.ExpandWidth(false)))
            {
                if (DataManager.editorSetup.HasId(DataManager.defaultTexture))
                {
                    EditorUtility.DisplayDialog("Duplicate item", "There is already a default texture entry, duplicates not allowed.\n\nPlease modify the existing one.", "OK");
                }
                else
                {
                    pendingIdsToAdd.Add(DataManager.defaultTexture);
                }
            }
            GUI.backgroundColor = defaultBgColor;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Remove all textures", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Remove all Texture ids", "Are you sure you want delete all Textures?\nYou'll lose all localizations of all languages (everything in other words...).", "Delete all YES !", "Of course NOT! Go back!"))
                {
                    foreach (Texture textureId in DataManager.editorSetup.GetIds<Texture>())
                    {
                        pendingIdsToRemove.Add(textureId);
                    }
                }
            }

            if (GUILayout.Button("Reset all localizations", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Reset all Texture localizations", "Are you sure you want delete all texture localizations?\nAll localizations will be set to null.", "Reset all YES !", "Of course NOT! Go back!"))
                {
                    RecordUndoForAllLocalizations("Reset OCL Textures localizations");
                    foreach (Texture textureId in DataManager.editorSetup.GetIds<Texture>())
                    {
                        foreach (SystemLanguage lang in DataManager.editorSetup.languages)
                        {
                            DataManager.editorSetup.SetLocalization(textureId, lang, null);
                        }
                    }
                }
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateAudioClipActionsBar()
        {
            EditorGUILayout.BeginHorizontal();


            Color defaultBgColor = GUI.backgroundColor;
            GUI.backgroundColor = DataManager.mainColor;
            if (GUILayout.Button("Add AudioClip Id", mainButtonStyle, GUILayout.ExpandWidth(false)))
            {
                if (DataManager.editorSetup.HasId(DataManager.defaultAudioClip))
                {
                    EditorUtility.DisplayDialog("Duplicate item", "There is already a default AudioClip entry, duplicates not allowed.\n\nPlease modify the existing one.", "OK");
                }
                else
                {
                    pendingIdsToAdd.Add(DataManager.defaultAudioClip);
                }
            }
            GUI.backgroundColor = defaultBgColor;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Remove all AudioClips", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Remove all AudioClip ids", "Are you sure you want delete all AudioClip ids?\nYou'll lose all localizations of all languages (everything in other words...).", "Delete all YES !", "Of course NOT! Go back!"))
                {
                    foreach (AudioClip audioClipId in DataManager.editorSetup.GetIds<AudioClip>())
                    {
                        pendingIdsToRemove.Add(audioClipId);
                    }
                }
            }

            if (GUILayout.Button("Reset all localizations", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Reset all audioClip localizations", "Are you sure you want delete all audioClip localizations?\nAll localizations will be set to null.", "Reset all YES !", "Of course NOT! Go back!"))
                {
                    RecordUndoForAllLocalizations("Reset OCL AudioClip localizations");
                    foreach (AudioClip audioClipId in DataManager.editorSetup.GetIds<AudioClip>())
                    {
                        foreach (SystemLanguage lang in DataManager.editorSetup.languages)
                        {
                            DataManager.editorSetup.SetLocalization(audioClipId, lang, null);
                        }
                    }
                }
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateStringsImportExportBar()
        {
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20), GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            GUILayout.Label("xml", EditorStyles.boldLabel, GUILayout.Width(80));

            if (GUILayout.Button("Import", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
            {
                // Import undo special case : we need to do the add in the import, therefore we can't handle the undo during the pending treatment
                foreach (ILocalization localization in DataManager.editorSetup.allLocalizations)
                {
                    if (localization is StringLocalization)
                        Undo.RecordObject((StringLocalization)localization, "Import xml");
                }
                DataManager.SelectAndImportXml();
            }

            if (GUILayout.Button("Export", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
            {
                DataManager.ExportToXml();
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20), GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            GUILayout.Label("csv", EditorStyles.boldLabel, GUILayout.Width(80));

            if (GUILayout.Button("Import", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
            {
                // Import undo special case : we need to do the add in the import, therefore we can't handle the undo during the pending treatment
                foreach (ILocalization localization in DataManager.editorSetup.allLocalizations)
                {
                    if(localization is StringLocalization)
                        Undo.RecordObject((StringLocalization)localization, "Import csv");
                }
                DataManager.SelectAndImportCSV();
            }

            if (GUILayout.Button("Export", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
            {
                DataManager.ExportToCSV();
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

        }

        private void CreateTableTabs()
        {
            int oldSelectedTab = selectedTab;
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton);
                    tabStyle.fixedHeight = 30;
                    if (GUILayout.Toggle(selectedTab == STRING_TAB_ID, "Texts", tabStyle))
                        selectedTab = STRING_TAB_ID;

                    if (GUILayout.Toggle(selectedTab == SPRITE_TAB_ID, "Images (Sprite)", tabStyle))
                        selectedTab = SPRITE_TAB_ID;

                    if (GUILayout.Toggle(selectedTab == TEXTURE_TAB_ID, "Images (Texture)", tabStyle))
                        selectedTab = TEXTURE_TAB_ID;

                    if (GUILayout.Toggle(selectedTab == AUDIOCLIP_TAB_ID, "Audio (AudioClip)", tabStyle))
                        selectedTab = AUDIOCLIP_TAB_ID;
                }
                GUILayout.EndHorizontal();

                if(oldSelectedTab != selectedTab)
                {
                    // All tables have there own scroll
                    tableScrollPosition.y = 0;
                    // ask for reload on next layout event
                    dataProviderChanged = true;
                    ResetFilter();
                }

                if (selectedTab == STRING_TAB_ID)
                {
                    CreateTextActionsBar();
                    if (EditorGUILayout.BeginFadeGroup(showStringsImportExport.faded))
                    {
                        CreateStringsImportExportBar();
                    }
                    EditorGUILayout.EndFadeGroup();
                    CreateFiltersBar();
                    CreateLocalizationTable(typeof(string));
                }
                else if (selectedTab == SPRITE_TAB_ID)
                {
                    CreateSpriteActionsBar();
                    CreateFiltersBar();
                    CreateLocalizationTable(typeof(Sprite));
                }
                else if (selectedTab == TEXTURE_TAB_ID)
                {
                    CreateTextureActionsBar();
                    CreateFiltersBar();
                    CreateLocalizationTable(typeof(Texture));
                }
                else if (selectedTab == AUDIOCLIP_TAB_ID)
                {
                    CreateAudioClipActionsBar();
                    CreateFiltersBar();
                    CreateLocalizationTable(typeof(AudioClip));
                }
            }
            GUILayout.EndVertical();
        }

        private void CreateLocalizationTable(Type type)
        {
            int objectFieldsPadding = 2;
            int stringTextFieldHeight = 50;
            int imagePreviewHeight = 70;
            int imagePreviewWidth = 70;
            int audioPreviewHeight = 15;
            // Columns width - only the localization column has variable width
            int languageColWidth = 60;
            int optionsColWidth = 35;
            int indexColWidth = 40;

            int rowButtonsSize = 25;

            int rowSplitterThickness = 1;
            int rowSplitterHeight = GUIUtils.splitterMargin * 2 + rowSplitterThickness;

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginHorizontal(headerBackgroundStyle);
            GUILayout.Space(indexColWidth + 5);
            string idColName = type.ToString().Substring(type.ToString().LastIndexOf(".") + 1);

            EditorGUILayout.LabelField(idColName, headerLabelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));
            EditorGUILayout.LabelField("Localization", headerLabelStyle, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            Vector2 newScrollPosition = EditorGUILayout.BeginScrollView(tableScrollPosition);
            if(tableScrollPosition != newScrollPosition)
            {
                tableScrollPosition = newScrollPosition;
                // Reset focus during scroll, unless it's a filter focus
                if(!GUI.GetNameOfFocusedControl().Equals(FILTER_SEARCH_CONTROL_NAME))
                    GUI.FocusControl(null);
            }

            // Selected languages count
            int numSelectedLanguages = 1;
            int numLanguages = DataManager.editorSetup.languages.Count;
            if (filter_languages_applied == -1)
                numSelectedLanguages = numLanguages;
            else
            {
                numSelectedLanguages = 0;
                foreach (SystemLanguage language in DataManager.editorSetup.languages)
                {
                    int layer = 1 << DataManager.editorSetup.languages.IndexOf(language);
                    if ((filter_languages_applied & layer) != 0)
                    {
                        numSelectedLanguages++;
                    }
                }
            }

            // Index of the id in display
            int index = 0;
            object entryToDelete = null;
            object entryToAdd = null;

            // optimization ? cache at class level, hard to maintain clean
            foreach (object id in tableDataProvider)
            {
                // RowHeight
                if (id is string)
                    currentRowHeight = Math.Max((stringTextFieldHeight * numSelectedLanguages), stringTextFieldHeight);
                else if (id is Sprite || id is Texture)
                    currentRowHeight = Math.Max((imagePreviewHeight * numSelectedLanguages), imagePreviewHeight);
                else if (id is AudioClip)
                    currentRowHeight = Math.Max((audioPreviewHeight * numSelectedLanguages), audioPreviewHeight);
                currentRowHeight += rowSplitterHeight;
                currentRowHeight += objectFieldsPadding;

                bool hiddenByScroll = IsIdHiddenByScroll(objectFieldsPadding, stringTextFieldHeight, imagePreviewHeight, audioPreviewHeight, index, id);
                
                if (hiddenByScroll)
                {
                    //GUILayout.Space(rowHeight);
                    GUILayout.Space(currentRowHeight);
                }
                else {

                    //EditorGUILayout.BeginHorizontal(index % 2 == 0 ? lineColor1 : lineColor2, GUILayout.Height(currentRowHeight));
                    // Row horizontal group
                    EditorGUILayout.BeginHorizontal(GUILayout.Height(currentRowHeight - rowSplitterHeight), GUILayout.ExpandWidth(true));

                    // control name format : id + separator [ + language]

                    object modifiedId = null;

                    GUILayout.Label((index + 1).ToString(), GUILayout.Width(indexColWidth));

                    GUI.SetNextControlName(id.ToString());
                    // Id horizontal group - Set the size of the first column using screen
                    EditorGUILayout.BeginHorizontal(GUILayout.MinWidth((EditorGUIUtility.currentViewWidth / 2) - indexColWidth - optionsColWidth - rowButtonsSize - languageColWidth ));
                    if (id is string)
                    {
                        EditorStyles.textField.wordWrap = true;
                        modifiedId = EditorGUILayout.TextArea((string)id, GUILayout.Height(stringTextFieldHeight), GUILayout.ExpandWidth(true), GUILayout.MaxWidth((EditorGUIUtility.currentViewWidth / 2) - indexColWidth - optionsColWidth - rowButtonsSize - languageColWidth));
                    }
                    else if (id is Sprite)
                    {
                        modifiedId = EditorGUILayout.ObjectField((Sprite)id, typeof(Sprite), false, GUILayout.Height(imagePreviewHeight), GUILayout.Width(imagePreviewWidth));
                        GUILayout.FlexibleSpace();
                    }
                    else if (id is Texture)
                    {
                        modifiedId = EditorGUILayout.ObjectField((Texture)id, typeof(Texture), false, GUILayout.Height(imagePreviewHeight), GUILayout.Width(imagePreviewWidth));
                        GUILayout.FlexibleSpace();
                    }
                    else if (id is AudioClip)
                    {
                        modifiedId = EditorGUILayout.ObjectField((AudioClip)id, typeof(AudioClip), false, GUILayout.Height(audioPreviewHeight));
                    }
                    EditorGUILayout.EndHorizontal();

                    if (modifiedId != null && !modifiedId.Equals(id))
                    {
                        if ((modifiedId is Sprite || modifiedId is Texture || modifiedId is AudioClip) && tableDataProvider.Contains(modifiedId))
                        {
                            EditorUtility.DisplayDialog("Duplicates", "Id already exists, please modify the existing entry.", "OK");
                        }
                        else
                        {
                            // delete key to add modified instead
                            entryToDelete = id;
                            entryToAdd = modifiedId;
                        }

                    }

                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));

                    for (int langIndex = 0; langIndex < DataManager.editorSetup.languages.Count; langIndex++)
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        SystemLanguage lang = (SystemLanguage)DataManager.editorSetup.languages[langIndex];

                        bool isSelectedLanguage = IsLanguageSelected(lang);

                        if (isSelectedLanguage)
                        {
                            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                            GUI.skin.label.wordWrap = true;
                            GUILayout.Label(lang.ToString(), GUILayout.Width(languageColWidth));

                            object editValue = null;
                            object currentValue = DataManager.editorSetup.GetLocalization(id, lang);

                            string langFocus = id + CONTROL_NAME_SEPARATOR + lang;
                            GUI.SetNextControlName(langFocus);

                            if (id is string)
                            {
                                EditorStyles.textField.wordWrap = true;
                                editValue = EditorGUILayout.TextArea((string)currentValue, GUILayout.Height(stringTextFieldHeight), GUILayout.ExpandWidth(true));
                            }
                            else if (id is Sprite)
                            {
                                editValue = EditorGUILayout.ObjectField((Sprite)currentValue, typeof(Sprite), false, GUILayout.Height(imagePreviewHeight), GUILayout.Width(imagePreviewWidth));
                                GUILayout.FlexibleSpace();

                            }
                            else if (id is Texture)
                            {
                                editValue = EditorGUILayout.ObjectField((Texture)currentValue, typeof(Texture), false, GUILayout.Height(imagePreviewHeight), GUILayout.Width(imagePreviewWidth));
                                GUILayout.FlexibleSpace();
                            }
                            else if (id is AudioClip)
                            {
                                editValue = EditorGUILayout.ObjectField((AudioClip)currentValue, typeof(AudioClip), false, GUILayout.Height(audioPreviewHeight));
                            }

                            if(id is string)
                            {
                                if (GUILayout.Button(new GUIContent(DataManager.autoTranslateIconTex, "Localize this language using Microsoft Translator"), GUILayout.Width(rowButtonsSize), GUILayout.Height(rowButtonsSize)))
                                {
                                    GUI.FocusControl(null);
                                    string translateValue = DataManager.translator.Translate((string)id, DataManager.editorSetup.defaultLanguage, lang);
                                    if (translateValue != null)
                                        editValue = translateValue;
                                }
                            }
                            
                            if (editValue != currentValue)
                            {
                                RecordUndoForLocalizationLanguage(id, lang, name);

                                DataManager.editorSetup.SetLocalization(id, lang, editValue);
                                if (IsFilterModified())
                                    filter_tableIdWhiteList.Add(id);
                            }

                            EditorGUILayout.BeginHorizontal(GUILayout.Width(optionsColWidth));
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                            GUI.enabled = (currentValue != null);
                            if (GUILayout.Button(new GUIContent(DataManager.nullIconTex, "Set the localization value to null"), GUILayout.Width(rowButtonsSize), GUILayout.Height(rowButtonsSize)))
                            {
                                RecordUndoForLocalizationLanguage(id, lang, name);

                                DataManager.editorSetup.SetLocalization(id, lang, null);
                                GUI.FocusControl(null);
                            }
                            GUI.enabled = true;

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.EndHorizontal();
                        }

                    }

                    EditorGUILayout.EndVertical();


                    if (GUILayout.Button(DataManager.closeIconTex, GUILayout.Width(rowButtonsSize), GUILayout.Height(rowButtonsSize)))
                    {
                        if (EditorUtility.DisplayDialog("Delete item", "Are you sure you want delete this item?", "Yes !", "Not so sure..."))
                        {
                            entryToDelete = id;
                        }
                        GUI.FocusControl(null);
                    }
                    GUILayout.Space(10);

                    EditorGUILayout.EndHorizontal();

                    // Row splitter
                    GUIUtils.Splitter(1);
                }

                index++;

            }

            // No item :(
            if (index == 0 && !IsFilterModified())
            {
                GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                GUILayout.FlexibleSpace();
                if(selectedTab.Equals(STRING_TAB_ID))
                    GUILayout.Label("No string to localize :( Add a new one or make an import !");
                else if (selectedTab.Equals(SPRITE_TAB_ID))
                    GUILayout.Label("No Sprite to localize :( Add a new one !");
                else if(selectedTab.Equals(TEXTURE_TAB_ID))
                    GUILayout.Label("No Texture to localize :( Add a new one !");
                else if (selectedTab.Equals(AUDIOCLIP_TAB_ID))
                    GUILayout.Label("No AudioClip to localize :( Add a new one !");
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            currentNumItems = index;

            // Handle string id modification by deleting old value and adding the new one... not pretty, but works...
            if (entryToDelete != null && entryToAdd != null)
            {
                // Other types don't support hot replace
                if (entryToAdd is string)
                {
                    int uniqueIndex = 0;
                    string uniqueEntryToAdd = (string)entryToAdd;
                    // ensure unicity before replace
                    while (cachedAllStrings.Contains(uniqueEntryToAdd, StringComparer.OrdinalIgnoreCase))
                    {
                        uniqueIndex++;
                        uniqueEntryToAdd = "[id already exists - " + uniqueIndex + "] " + entryToAdd;
                    }
                    entryToAdd = uniqueEntryToAdd;

                }

                pendingIdsToReplace[entryToDelete] = entryToAdd;
                if (IsFilterModified())
                {
                    filter_tableIdWhiteList.Add(entryToAdd);
                    filter_tableIdWhiteList.Remove(entryToDelete);
                }
            }
            else {
                if (entryToDelete != null)
                {
                    pendingIdsToRemove.Add(entryToDelete);
                }
            }

            EditorGUILayout.EndScrollView();
            Rect scrollRect = GUILayoutUtility.GetLastRect();
            if(scrollRect.height != -1 && scrollRect.height != 1 && scrollRect.height != currentScrollHeight)
                currentScrollHeight = scrollRect.height;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
            
            // Footer
            EditorGUILayout.BeginHorizontal(boxStyle);
            GUILayout.Label(currentNumItems + "/" + currentMaxNumItems + " " + idColName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        
        private static void RecordUndoForAllLocalizations(string undoRedoName)
        {
            foreach (ILocalization localization in DataManager.editorSetup.allLocalizations)
            {
                if (localization is StringLocalization)
                {
                    Undo.RecordObject((StringLocalization)localization, undoRedoName);
                }
                else if (localization is SpriteLocalization)
                {
                    Undo.RecordObject((SpriteLocalization)localization, undoRedoName);
                }
                else if (localization is TextureLocalization)
                {
                    Undo.RecordObject((TextureLocalization)localization, undoRedoName);
                }
                else if (localization is AudioClipLocalization)
                {
                    Undo.RecordObject((AudioClipLocalization)localization, undoRedoName);
                }
            }
        }

        private static void RecordUndoForLocalizationLanguage(object id, SystemLanguage lang, string name)
        {
            // Undo redo localization edit
            ILocalization editedLocalization = DataManager.editorSetup.GetLocalizationAsset(id.GetType(), lang);
            if (editedLocalization is StringLocalization)
            {
                Undo.RecordObject((StringLocalization)editedLocalization, name);
            }
            else if (editedLocalization is SpriteLocalization)
            {
                Undo.RecordObject((SpriteLocalization)editedLocalization, name);
            }
            else if (editedLocalization is TextureLocalization)
            {
                Undo.RecordObject((TextureLocalization)editedLocalization, name);
            }
            else if (editedLocalization is AudioClipLocalization)
            {
                Undo.RecordObject((AudioClipLocalization)editedLocalization, name);
            }
        }

        private bool IsIdHiddenByScroll(int objectFieldsPadding, int stringTextFieldHeight, int imagePreviewHeight, int audioPreviewHeight, int index, object id)
        {

            // Security with types, as scroll seems to modify scrollposition asynchronously between layout and repatin, what a bad boy scroll
            float scrollY = tableScrollPosition.y;
            if (Event.current.type != EventType.Layout && currentLayoutScrollY != -1 && currentLayoutScrollY != tableScrollPosition.y)
            {
                scrollY = currentLayoutScrollY;
            }
            else
                currentLayoutScrollY = tableScrollPosition.y;

            // To optimize display of large collections, only displayed cells are created, others are replaced with GUILayout.Space
            // As getting real size of the scrollRect take resources and is complicated, we take the maximum vlaue possible : screen height
            // This way, some cells are created for no use, but performances aren't killed by a few cells anyway ;)
            bool isContentHiddenByScroll = true;
            if ((((index + 1) * currentRowHeight) >= (scrollY - 100)) && (((index + 1) * currentRowHeight) <= (scrollY + Screen.height)))
            {
                isContentHiddenByScroll = false;
            }

            return isContentHiddenByScroll;
        }

        private bool IsIdInWhiteList(object id)
        {
            if (id == null || filter_tableIdWhiteList.Count == 0)
                return false;
            return filter_tableIdWhiteList.Contains(id);
        }

        private bool TestSearchOnIdLocalization(object id, SystemLanguage lang)
        {
            string searchString = null;

            object localization = DataManager.editorSetup.GetLocalization(id, lang);
            if (localization != null)
                searchString = localization.ToString();

            if (searchString == null)
            {
                return false;
            }

            return searchString.IndexOf(filter_search_applied, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TestSearchOnIdLocalizations(object id)
        {
            bool res = false;
            foreach (SystemLanguage lang in DataManager.editorSetup.languages)
            {
                res = TestSearchOnIdLocalization(id, lang);
                if (res)
                    break;
            }

            return res;
        }

        private bool TestSearchOnId(object id)
        {
            bool res = false;
            string searchString = id.ToString();

            if (searchString.IndexOf(filter_search_applied, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                res = true;
            }

            return res;
        }

        private bool IsLanguageSelected(SystemLanguage lang)
        {
            if (DataManager.editorSetup.languages.Contains(lang))
            {
                int layer = 1 << DataManager.editorSetup.languages.IndexOf(lang);
                if ((filter_languages_applied & layer) != 0)
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Update the table dataProvider for the current state of view
        /// </summary>
        /// <returns></returns>
        private void UpdateDataProvider()
        {
            // Cache strings for use without performance impact
            cachedAllStrings = DataManager.editorSetup.GetIds<string>();

            List<object> newDataProvider = new List<object>();
            if(selectedTab == STRING_TAB_ID)
            {
                foreach (string id in cachedAllStrings)
                {
                    newDataProvider.Add(id);
                }
            }
            else if(selectedTab == SPRITE_TAB_ID)
            {
                foreach (Sprite id in DataManager.editorSetup.GetIds<Sprite>())
                {
                    newDataProvider.Add(id);
                }
            }
            else if(selectedTab == TEXTURE_TAB_ID)
            {
                foreach (Texture id in DataManager.editorSetup.GetIds<Texture>())
                {
                    newDataProvider.Add(id);
                }
            }
            else if(selectedTab == AUDIOCLIP_TAB_ID)
            {
                foreach (AudioClip id in DataManager.editorSetup.GetIds<AudioClip>())
                {
                    newDataProvider.Add(id);
                }
            }
            currentMaxNumItems = newDataProvider.Count;

            // feed tableDataProvider with the new provider and apply filters
            tableDataProvider.Clear();
            foreach (object id in newDataProvider)
            {
                // Store if the currend id is valid for the search string
                bool isValidateSearch = false;
                bool hasUnstranslated = false;

                bool isValidateWhiteList = false;
                if (IsIdInWhiteList(id))
                {
                    isValidateWhiteList = true;
                }

                // Search Filter on Id and Localizations : hide the line if no one correspond
                if (!isValidateWhiteList && !filter_search_applied.Equals("") && (TestSearchOnIdLocalizations(id) || TestSearchOnId(id)))
                {
                    isValidateSearch = true;
                }

                // Unused filter : If all values are ok, no need to display line
                if (!isValidateWhiteList && filter_untranslatedOnly_applied)
                {
                    foreach (SystemLanguage lang in DataManager.editorSetup.languages)
                    {
                        if (DataManager.editorSetup.GetLocalization(id, lang) == null)
                        {
                            hasUnstranslated = true;
                            break;
                        }
                    }
                }

                // Filters and whitelist decide if line should be displayed
                if (((filter_untranslatedOnly_applied && !hasUnstranslated) || (!filter_search_applied.Equals("") && !isValidateSearch)) && !isValidateWhiteList)
                {
                    // skip
                }
                else
                {
                    tableDataProvider.Add(id);
                }

            }
        }

        private void ScrollToBottom()
        {
            if (currentNumItems != -1 && currentRowHeight != -1 && currentScrollHeight != -1)
            {
                tableScrollPosition.y = (currentRowHeight * currentNumItems) - currentScrollHeight;
            }
        }

        void OnDestroy()
        {
            DataManager.PersistSetup();
        }

        void OnFocus()
        {
            DataManager.LoadSetup();
            // ask for reload on next layout event
            dataProviderChanged = true;

        }

        void OnLostFocus()
        {
            DataManager.PersistSetup();
        }

    }
}