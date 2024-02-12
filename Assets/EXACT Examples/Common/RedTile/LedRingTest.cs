using UnityEngine;
using NaughtyAttributes;

namespace Exact.Example.Test
{
    [System.Serializable]
    public class LedRingTest : MonoBehaviour
    {
        LedRing ledRing;

        private void OnValidate()
        {
            ledRing = GetComponent<LedRing>();
        }

        [SerializeField, BoxGroup("Single led")] Color ledColor;
        [SerializeField, BoxGroup("Single led")] int ledNumber;

        [Button]
        void SetLedColor()
        {
            ledRing.SetColor(ledNumber, ledColor);
        }

        [SerializeField, BoxGroup("Fade")] Color fadeColor;
        [SerializeField, BoxGroup("Fade")] float fadeFromIntensity;
        [SerializeField, BoxGroup("Fade")] float fadeToIntensity = 1;
        [SerializeField, BoxGroup("Fade")] float fadeDuration = 1;

        [Button]
        void StartFading()
        {
            ledRing.StartFading(fadeColor, fadeFromIntensity, fadeToIntensity, fadeDuration);
        }

        [Button]
        void StopFading()
        {
            ledRing.StopFading();
        }
        [SerializeField, BoxGroup("Pulse")] Color pulseColor;
        [SerializeField, BoxGroup("Pulse")] float pulseFromIntensity;
        [SerializeField, BoxGroup("Pulse")] float pulseToIntensity = 1;
        [SerializeField, BoxGroup("Pulse")] float pulseLength = 1;

        [Button]
        void StartPulsing()
        {
            ledRing.StartPulsing(pulseColor, pulseFromIntensity, pulseToIntensity, pulseLength);
        }

        [Button]
        void StopPulsing()
        {
            ledRing.StopPulsing();
        }

        [SerializeField, BoxGroup("Rotation")] float rotationTime = 1;

        [Button]
        void StartRotating()
        {
            ledRing.StartRotating(rotationTime);
        }

        [Button]
        void StopRotating()
        {
            ledRing.StopRotating();
        }
    }
}
