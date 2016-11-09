using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{

	public class BGCcCursorObjectEditor : BGCcEditor
	{
	    protected override void InternalOnInspectorGUI()
	    {
	        EditorGUILayout.PropertyField(serializedObject.FindProperty("objectToManipulate"));
	    }
	}
}