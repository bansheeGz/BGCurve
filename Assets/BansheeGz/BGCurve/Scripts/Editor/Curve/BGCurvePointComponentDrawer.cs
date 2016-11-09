using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomPropertyDrawer(typeof(BGCurvePointComponent), true)]
    public class BGCurvePointComponentDrawer : BGPropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // idea.. partially copy/pasted from BGCcChoseDrawer
            // this is a required startup call
            SetUp(position, property, label, () =>
            {
                if (!(property.objectReferenceValue != null)) DrawProperty(property);
                else
                {
                    var point = (BGCurvePointComponent) property.objectReferenceValue;

                    var pointCount = point.Curve.PointsCount;

                    if (pointCount < 2) DrawProperty(property);
                    else
                    {
                        var pointIndex = point.Curve.IndexOf(point);
                        var buttonContent = new GUIContent("" + pointIndex, "Object has multiple components attached. Click to chose.");

                        var buttonWidth = GUI.skin.button.CalcSize(buttonContent).x;

                        Rect.width -= buttonWidth;
                        EditorGUI.PropertyField(Rect, property);


                        if (GUI.Button(new Rect(Rect) {width = buttonWidth, x = Rect.xMax}, buttonContent))
                        {
                            BGCurveChosePointWindow.Open(pointIndex, point.Curve, newPoint =>
                            {
                                property.objectReferenceValue = newPoint;
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