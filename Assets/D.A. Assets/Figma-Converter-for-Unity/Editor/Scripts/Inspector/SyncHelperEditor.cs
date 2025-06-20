using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.FCU
{
    [CustomEditor(typeof(SyncHelper)), CanEditMultipleObjects]
    internal class SyncHelperEditor : Editor
    {
        [SerializeField] DAInspector gui;
        private FigmaConverterUnity monoBeh;
        private SyncHelper syncObject;

        private void OnEnable()
        {
            syncObject = (SyncHelper)target;
            monoBeh = syncObject.Data.FigmaConverterUnity as FigmaConverterUnity;
        }

        public override void OnInspectorGUI()
        {
            gui.DrawGroup(new Group
            {
                GroupType = GroupType.Vertical,
                Style = gui.ColoredStyle.Background,
                Body = () =>
                {

                    if (monoBeh == null)
                    {
                        gui.Label12px(FcuLocKey.label_dont_remove_fcu_meta.Localize(), null, GUILayout.ExpandWidth(true));
                        gui.Label10px(FcuLocKey.label_more_about_layout_updating.Localize(), null, GUILayout.ExpandWidth(true));

                        gui.Space10();

                        gui.Label10px(FcuLocKey.label_fcu_is_null.Localize(nameof(FigmaConverterUnity), FcuConfig.CreatePrefabs, FcuConfig.SetFcuToSyncHelpers), null, GUILayout.ExpandWidth(true));
                        return;
                    }

                    gui.Colorize(() =>
                    {
                        if (monoBeh.IsUITK())
                        {
                            GUILayout.TextArea(syncObject.Data.NameHierarchy);
                        }

                        if (monoBeh.IsDebug())
                        {
                            gui.Space10();
                            EditorGUILayout.Vector3Field("World Position", syncObject.transform.position);
                            EditorGUILayout.Vector3Field("Local Position", syncObject.transform.localPosition);
                            gui.Space10();
                            base.OnInspectorGUI();
                        }
                    });

                    if (monoBeh.IsUITK() || monoBeh.IsDebug())
                        gui.Space10();

                    gui.Label12px(FcuLocKey.label_dont_remove_fcu_meta.Localize(), null, GUILayout.ExpandWidth(true));
                    gui.Label10px(FcuLocKey.label_more_about_layout_updating.Localize(), null, GUILayout.ExpandWidth(true));
                }
            });
        }
    }
}