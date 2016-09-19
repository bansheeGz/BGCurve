using BansheeGz.BGSpline.Components;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //we need it cause a curve can have multiple cursors and we have to be able to chose between them
    [CustomPropertyDrawer(typeof (BGCcCursor), true)]
    public class BGCcCursorDrawer : BGCcChoseDrawer<BGCcCursor>
    {
    }
}