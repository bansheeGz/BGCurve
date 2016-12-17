using UnityEngine;
using System.Collections;

namespace BansheeGz.BGSpline.Example
{
    //for 1.2 version demo scene
    public class BGTestDirector : MonoBehaviour
    {
        private static readonly Color NightColor = Color.black;
        private static readonly Color DayColor = new Color32(176, 224, 240, 255);

        //Day
        public Light SunLight;
        public Light DirectionalLight;
        public ParticleSystem SunParticles;

        //Night
        public Animator MoonAnimator;
        public Light MoonLight;
        public ParticleSystem StarsParticles;

        //Stars
        public GameObject Stars;

        //callback for sun, reaching particular point
        public void Sun(int point)
        {
            switch (point)
            {
                case 0:
                    StartCoroutine(ChangeBackColor(NightColor, DayColor));
                    StartCoroutine(ChangeDirectLightIntensity(0, .8f));
                    SunParticles.Play();
                    break;
                case 1:
                    SunLight.intensity = 1;
                    Stars.transform.localPosition += new Vector3(0, -20);
                    break;
                case 3:
                    Stars.transform.localPosition -= new Vector3(0, -20);
                    SunLight.intensity = 0;
                    SunParticles.Stop();
                    break;
            }
        }

        //callback for moon, reaching particular point
        public void Moon(int point)
        {
            switch (point)
            {
                case 0:
                    StartCoroutine(ChangeBackColor(DayColor, NightColor));
                    StartCoroutine(ChangeDirectLightIntensity(.8f, 0));
                    StarsParticles.Play();
                    break;
                case 1:
                    MoonAnimator.SetBool("play", true);
                    MoonLight.intensity = 1;
                    break;
                case 2:
                    StarsParticles.Stop();
                    break;
                case 3:
                    MoonAnimator.SetBool("play", false);
                    MoonLight.intensity = 0;
                    break;
            }
        }


        private IEnumerator ChangeBackColor(Color from, Color to)
        {
            var started = Time.time;
            const float changeTime = 1;

            while (Time.time - started < changeTime)
            {
                Camera.main.backgroundColor = Color.Lerp(from, to, (Time.time - started)/changeTime);
                yield return null;
            }
        }

        private IEnumerator ChangeDirectLightIntensity(float from, float to)
        {
            var started = Time.time;
            const float changeTime = 1;

            while (Time.time - started < changeTime)
            {
                DirectionalLight.intensity = Mathf.Lerp(from, to, (Time.time - started)/changeTime);
                yield return null;
            }
        }
    }
}