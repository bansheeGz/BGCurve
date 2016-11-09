using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcCursorObjectScale))]
    public class BGCcCursorObjectScaleEditor : BGCcCursorObjectEditor
    {
        private GUIContent scaleFieldContent;

        private BGCcCursorObjectScale ObjectScale
        {
            get { return (BGCcCursorObjectScale)cc; }
        }

        protected override void InternalOnInspectorGUI()
        {
            base.InternalOnInspectorGUI();

            BGEditorUtility.Assign(ref scaleFieldContent, () => new GUIContent("Scale Field", "Scale field to take a scale from. Each point will have it's own scale. Should be Vector3 field."));

            BGEditorUtility.CustomField(scaleFieldContent, cc.Curve, ObjectScale.ScaleField, BGCurvePointField.TypeEnum.Vector3, field =>
            {
                ObjectScale.ScaleField = field;
            });
        }

    }
}