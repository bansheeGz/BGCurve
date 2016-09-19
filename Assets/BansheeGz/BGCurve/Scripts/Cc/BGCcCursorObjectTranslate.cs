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
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcTranslateObject", 2)]
    public class BGCcCursorObjectTranslate : BGCcWithCursorObject
    {
        /// <summary>object was moved </summary>
        public event EventHandler ObjectTranslated;


        // Update is called once per frame
        private void Update()
        {
            if (ObjectToManipulate == null) return;
            ObjectToManipulate.position = Cursor.CalculatePosition();

            if (ObjectTranslated != null) ObjectTranslated(this, null);
        }
    }
}