using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGCcChoseDrawer<T> : BGPropertyDrawer where T : BGCc
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // this is a required startup call
            SetUp(position, property, label, () =>
            {
                if (!(property.objectReferenceValue != null))
                {
                    DrawProperty(property);
                }
                else
                {
                    var cc = (T) property.objectReferenceValue;

                    var allPossibleCcList = cc.GetComponents(cc.GetType());

                    if (allPossibleCcList.Length < 2)
                    {
                        DrawProperty(property);
                    }
                    else
                    {
                        var buttonContent = new GUIContent(BGEditorUtility.Trim(cc.CcName, 16), "Object has multiple components attached. Click to chose.");

                        var buttonWidth = GUI.skin.button.CalcSize(buttonContent).x;

                        Rect.width -= buttonWidth;
                        EditorGUI.PropertyField(Rect, property);


                        if (GUI.Button(new Rect(Rect) {width = buttonWidth, x = Rect.xMax}, buttonContent))
                        {
                            BGCcChoseWindow.Open(cc, allPossibleCcList, newCc =>
                            {
                                Undo.RecordObject(property.serializedObject.targetObject, "Cc changed");
                                property.objectReferenceValue = newCc;
                                property.serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(property.serializedObject.targetObject);
                            });
                        }
                    }
                }
            });
        }
    }
}