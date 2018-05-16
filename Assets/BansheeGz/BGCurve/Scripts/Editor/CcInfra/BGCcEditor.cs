using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
namespace BansheeGz.BGSpline.Editor
{
    public class BGCcEditor : UnityEditor.Editor
    {
        public event EventHandler ChangedParent;


        protected BGCc cc;
        private Type parentClass;

        //=================================================================  Unity callbacks
        public virtual void OnEnable()
        {
            cc = (BGCc) target;

            if (cc == null) return;

            //get all required components
            parentClass = cc.GetParentClass();


            InternalOnEnable();

            cc.ChangedParams -= ChangedParams;
            cc.ChangedParams += ChangedParams;

            Undo.undoRedoPerformed -= InternalOnUndoRedo;
            Undo.undoRedoPerformed += InternalOnUndoRedo;
        }


        public virtual void OnDestroy()
        {
            Undo.undoRedoPerformed -= InternalOnUndoRedo;
            cc.ChangedParams -= ChangedParams;
            InternalOnDestroy();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            var componentChanged = BGEditorUtility.ChangeCheck(() =>
            {
                //custom fields
                InternalOnInspectorGUI();

                // -------------  parents
                if (parentClass != null)
                {
                    var possibleParents = cc.GetComponents(parentClass);
                    if (possibleParents.Length > 1)
                    {
                        BGEditorUtility.Horizontal(() =>
                        {
                            GUILayout.Space(10);
                            BGEditorUtility.VerticalBox(() =>
                            {
                                var myParent = cc.GetParent(parentClass);
                                var options = new string[possibleParents.Length];
                                var index = 0;
                                for (var i = 0; i < possibleParents.Length; i++)
                                {
                                    var possibleParent = possibleParents[i];
                                    if (possibleParent == myParent) index = i;
                                    options[i] = ((BGCc)possibleParent).CcName;
                                }

                                //show popup
                                var label = BGCc.GetDescriptor(parentClass).Name ?? parentClass.Name;
                                var newIndex = EditorGUILayout.Popup(label, index, options);
                                if (newIndex != index)
                                {
                                    Undo.RecordObject(cc, "parent change");
                                    cc.SetParent((BGCc) possibleParents[newIndex]);
                                    if (ChangedParent != null) ChangedParent(this, null);
                                }
                            });
                        });
                    }
                }
            });


            //--------------  handles
            if (cc.SupportHandles && !BGCurveSettingsForEditor.CcInspectorHandlesOff && !cc.HideHandlesInInspector)
            {
                BGEditorUtility.Horizontal(() =>
                {
                    GUILayout.Space(10);
                    BGEditorUtility.VerticalBox(() =>
                    {
                        var showHandlesProperty = serializedObject.FindProperty("showHandles");
                        EditorGUILayout.PropertyField(showHandlesProperty);
                        if (cc.SupportHandlesSettings && showHandlesProperty.boolValue) BGEditorUtility.Indent(1, ShowHandlesSettings);
                    });
                });
            }

            //--------------  status
            var info = cc.Info;
            BGEditorUtility.HelpBox(info, MessageType.Info, !string.IsNullOrEmpty(info));

            //--------------  warning
            var warning = cc.Warning;
            BGEditorUtility.HelpBox(warning, MessageType.Warning, !string.IsNullOrEmpty(warning));

            //--------------  error
            var error = cc.Error;
            BGEditorUtility.HelpBox(error, MessageType.Error, !string.IsNullOrEmpty(error));

            if (!GUI.changed) return;

            Undo.RecordObject(cc, "fields change");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(cc);

            if (componentChanged) cc.FireChangedParams();

            InternalOnInspectorGUIPost();
        }

        protected virtual void ShowHandlesSettings()
        {
        }


        protected virtual void ChangedParams(object sender, EventArgs e)
        {
            Repaint();
        }


        public virtual void OnSceneGUI()
        {
            if (!cc.ShowHandles || !cc.SupportHandles) return;
            InternalOnSceneGUI();
        }

        public virtual void OnDelete()
        {
        }

        //=================================================================  Internal to override

        protected virtual void InternalOnEnable()
        {
        }

        protected virtual void InternalOnDestroy()
        {
        }

        protected virtual void InternalOnInspectorGUI()
        {
        }

        protected virtual void InternalOnSceneGUI()
        {
        }

        protected virtual void InternalOnInspectorGUIPost()
        {
        }

        protected virtual void InternalOnUndoRedo()
        {
        }

    }
}