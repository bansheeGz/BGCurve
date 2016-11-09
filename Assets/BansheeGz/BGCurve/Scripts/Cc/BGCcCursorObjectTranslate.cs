using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Moves an object to cursor's position </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursorObjectTranslate")]
    [
        CcDescriptor(
            Description = "Translate an object to the position, the cursor provides.",
            Name = "Translate Object By Cursor",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcCursorObjectTranslate123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcTranslateObject")]
    [ExecuteInEditMode]
    public class BGCcCursorObjectTranslate : BGCcWithCursorObject
    {
        //===============================================================================================
        //                                                    Events
        //===============================================================================================
        /// <summary>object was moved </summary>
        public event EventHandler ObjectTranslated;


        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        // Update is called once per frame
        private void Update()
        {
            var transformToMove = ObjectToManipulate;

            if (transformToMove == null) return;

            var pointsCount = Curve.PointsCount;

            switch (pointsCount)
            {
                case 0:
                    return;
                case 1:
                    transformToMove.position = Curve[0].PositionWorld;
                    break;
                default:

                    transformToMove.position = Cursor.CalculatePosition();

                    if (ObjectTranslated != null) ObjectTranslated(this, null);

                    break;
            }
        }
    }
}