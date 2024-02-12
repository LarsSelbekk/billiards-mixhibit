using UnityEngine;
using NaughtyAttributes;

using System.Collections;

namespace Exact.Example
{
    [RequireComponent(typeof(ScoreKeeper))]
    public class FollowTheRedDot : MonoBehaviour
    {
        [SerializeField, Required]
        ExactManager exactManager;

        [SerializeField]
        bool waitForAllConnected = false;

        [SerializeField] Color color = Color.red;
        [SerializeField] float intensity = 0.5f;
        [SerializeField, MinMaxSlider(0, 1)] Vector2 fadeIntensity = new Vector2(0, 1);
        [SerializeField] float time = 0;

        Device active = null;

        ScoreKeeper scoreKeeper;

        void Start()
        {
            scoreKeeper = GetComponent<ScoreKeeper>();
            StartCoroutine(Startup());
        }

        IEnumerator Startup()
        {
            if(waitForAllConnected)
            {
                Debug.Log("Waiting for devices");
                while (!exactManager.AllDevicesConnected())
                {
                    yield return null;
                }
            }
            
            while (active == null)
            {
                yield return null;
                var devices = exactManager.GetConnectedDevices();
                if (devices.Count > 0)
                {
                    Restart();
                }
            }
            Debug.Log("Startup complete");
        }

        void Restart()
        {
            StopAllCoroutines();
            scoreKeeper.Score = 0;

            var devices = exactManager.GetConnectedDevices();

            for (int i = 1; i < devices.Count; i++)
            {
                var led = devices[i].GetComponent<LedRing>();
                led.StopFading();
                led.SetColor(Color.black);
            }

            active = devices[0];
            active.GetComponent<LedRing>().SetColorAndIntensity(color, intensity);
            active.GetComponent<TonePlayer>().PlayTone(500, 0.1f);
        }

        public void OnTapped(Device device)
        {
            if (device != active) { return; }
            scoreKeeper.Score++;
            SetNextActive();
        }

        void SetNextActive()
        {
            StopAllCoroutines();

            var devices = exactManager.GetDevicesWithComponent<Device>();
            if (devices.Count <= 1) { return; }

            var led = active.GetComponent<LedRing>();
            led.StopFading();
            led.SetColor(Color.black);

            devices.Remove(active);
            int i = Random.Range(0, devices.Count);
            active = devices[i];

            if (time > 0)
            {
                StartCoroutine(StartFade());
            }
            else
            {
                active.GetComponent<LedRing>().SetColorAndIntensity(color, intensity);
            }

            active.GetComponent<TonePlayer>().PlayTone(500, 0.1f);
        }

        IEnumerator StartFade()
        {
            active.GetComponent<LedRing>().StartFading(color, fadeIntensity.y, fadeIntensity.x, time);
            yield return new WaitForSeconds(time);
            Restart();
        }
    }
}
