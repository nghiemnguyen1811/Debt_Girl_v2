using UnityEngine;
using System.Collections;
using UnityEditor;


namespace OneClickLocalization.Editor.UtilityWindows
{
    /// <summary>
    /// Utility window used to configure Components includes and excludes used during an automatic setup process.
    /// </summary>
    public class OCLSetupCompsIncludeExcludeWindow : EditorWindow
    {
        private static string windowTitle = "Include / Exclude Compoents";

        public static OCLSetupCompsIncludeExcludeWindow ShowWindow()
        {
            OCLSetupCompsIncludeExcludeWindow window = (OCLSetupCompsIncludeExcludeWindow)EditorWindow.GetWindow(typeof(OCLSetupCompsIncludeExcludeWindow), true, windowTitle);
            window.minSize = new Vector2(400, 250);
            return window;
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Configure components to include or exclude during automatic setup process", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("By default, only components supported by the OCLComponentAdapter are in the includeList.", EditorStyles.helpBox);

            GUILayout.Label("List values are separated by \",\" and you can insert full names (with namespace) or part of names.", EditorStyles.helpBox);

            GUILayout.Label("Includes", EditorStyles.boldLabel);
            DataManager.parseIncludeComps = EditorGUILayout.TextField(DataManager.parseIncludeComps).Replace(" ", "");
            GUILayout.Label("Excludes", EditorStyles.boldLabel);
            DataManager.parseExcludeComps = EditorGUILayout.TextField(DataManager.parseExcludeComps).Replace(" ", "");

            EditorGUILayout.EndVertical();
        }

        void OnDestroy()
        {
            DataManager.SaveEditorPrefs();
        }

        void OnFocus()
        {
            DataManager.LoadEditorPrefs();
        }

        void OnLostFocus()
        {
            DataManager.SaveEditorPrefs();
        }
    }
}