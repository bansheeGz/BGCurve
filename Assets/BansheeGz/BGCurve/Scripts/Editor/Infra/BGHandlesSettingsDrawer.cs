using BansheeGz.BGSpline.Curve;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    // Currently this file is not used
    // custom drawer for handles settings
    // idea.. code is still messy after refactoring
    [CustomPropertyDrawer(typeof (BGCurveSettings.SettingsForHandles))]
    public class BGHandlesSettingsDrawer : BGPropertyDrawer
    {
        //do not remove. It indicates 5 lines are used
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label)*5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // this is required startup call
            SetUp(position, property, label, () =>
            {
                //occupy 2 lines
                SetHeight(Height*2);

                //this call will set controlRect=space for control itself (without label)
                PrefixLabel("Remove");

                //set cursor to the start of just allocated space for control
                SetCursor(ControlRect.x, ControlRect.y);

                //this block will draw checkboxes for Remove options
                Indent(0, () =>
                {
                    //------------------------------------------- first line
                    LineWithRemoveControls(property, Height, 15, new[] {"X", "Y", "Z"}, new[] {"RemoveX", "RemoveY", "RemoveZ"});

                    //------------------------------------------- second line
                    SetCursor(ControlRect.x, CursorY + Height);
                    LineWithRemoveControls(property, Height, 20, new[] {"XZ", "XY", "YZ"}, new[] {"RemoveXZ", "RemoveXY", "RemoveYZ"});
                });

                //------------------------------------------- third line (Scale Axis)
                //reset height to standard 1 line
                SetHeight(Height);
                //skip 2 lines, used for checkboxes
                NextLine(2);
                DrawRelativeProperty(property, "Scale Axis", "AxisScale");

                //------------------------------------------- forth line (Scale Planes)
                NextLine();
                DrawRelativeProperty(property, "Scale Planes", "PlanesScale");

                //------------------------------------------- fifth line (Alpha)
                NextLine();
                DrawRelativeProperty(property, "Alpha", "Alpha");
            });
        }

        private void LineWithRemoveControls(SerializedProperty property, float height, int labelWidth, string[] labels, string[] fields)
        {
            for (var i = 0; i < labels.Length; i++)
            {
                RelativePropertyByCursor(10, height, property, fields[i]);
                LabelByCursor(labelWidth, height, labels[i]);
            }
        }
    }
}