using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Scale an object according to the cursor's position. Scale values are taken from a curve's Vector3 field</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursorObjectScale")]
    [
        CcDescriptor(
            Description = "Scale the object, according to cursor position. Scale values are taken from curve's field values.",
            Name = "Scale Object By Cursor",
            Icon = "BGCcCursorObjectScale123")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcScaleObject")]
    [ExecuteInEditMode]
    public class BGCcCursorObjectScale : BGCcWithCursorObject
    {
        //===============================================================================================
        //                                                    Events
        //===============================================================================================
        /// <summary>object was scaled </summary>
        public event EventHandler ObjectScaled;

        //===============================================================================================
        //                                                    Fields(Persistent)
        //===============================================================================================

        [SerializeField] [Tooltip("Field to store the scale value at points. It should be a Vector3 field.")] private BGCurvePointField scaleField;

        public BGCurvePointField ScaleField
        {
            get { return scaleField; }
            set { ParamChanged(ref scaleField, value); }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get { return ChoseMessage(base.Error, () => scaleField == null ? "Scale field is not defined." : null); }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================

        // Update is called once per frame
        private void Update()
        {
            if (ObjectToManipulate == null || scaleField == null) return;

            var pointsCount = Curve.PointsCount;

            switch (pointsCount)
            {
                case 0:
                    return;
                case 1:
                    ObjectToManipulate.localScale = Curve[0].GetVector3(scaleField.FieldName);
                    break;
                default:
                    var result = LerpVector(scaleField.FieldName);
                    if (float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z)) return;

                    ObjectToManipulate.localScale = result;

                    if (ObjectScaled != null) ObjectScaled(this, null);
                    break;
            }
        }
    }
}