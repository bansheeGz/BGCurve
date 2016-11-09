using UnityEngine;
using UnityEngine.SceneManagement;

namespace BansheeGz.BGSpline.Example
{
    //for main menu scene
    public class BGTestMainMenu : MonoBehaviour
    {
        //to let know that scenes are loaded via menu
        public static bool Inited;

        // Use this for initialization
        void Start()
        {
            Inited = true;
        }

        //for loading scenes
        public void LoadScene(string scene)
        {
            SceneManager.LoadScene(scene);
        }
    }
}