using UnityEngine;

namespace BansheeGz.BGSpline.Example
{
    public class BGTestCamera : MonoBehaviour
    {
        private const int Speed = 100;

        private GUIStyle style;

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKey(KeyCode.A)) transform.RotateAround(Vector3.zero, Vector3.up, Speed*Time.deltaTime);
            else if (Input.GetKey(KeyCode.D)) transform.RotateAround(Vector3.zero, Vector3.up, -Speed*Time.deltaTime);
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) {fontSize = 24};
            GUILayout.Label("Use A and D to rotate camera", style);
        }
    }
}