using UnityEngine;
using NaughtyAttributes;

using System.Collections;

namespace Exact.Example
{
    [RequireComponent(typeof(Device))]
    public class LedRing : DeviceComponent
    {
        public override string GetComponentType() { return "led_ring"; }

        [SerializeField, OnValueChanged("OnUniformColorChanged")]
        Color uniformColor = Color.black;
        bool uniform = true;

        [SerializeField, OnValueChanged("OnIntensityChanged"), Range(0, 1)]
        float intensity = 0.3f;

        [SerializeField, OnValueChanged("OnRotationChanged"), Range(0, 360)]
        int rotation = 0;

        [SerializeField]
        int numLeds = 24;

        [SerializeField]
        ColorRingBase colorRing;

        protected override void Awake()
        {
            base.Awake();

            colorRing.SetNumberOfSegments(numLeds);
            colorRing.SetUniformColor(uniformColor);
            colorRing.SetIntensity(intensity);
        }

        public void OnConnect()
        {
            SetColor(uniformColor, true);
            SetIntensity(intensity, true);
        }

        /// <summary>
        /// Sets a uniform color for the led ring.
        /// </summary>
        /// <param name="color">The new color.</param>
        /// <param name="forceUpdate">Whether the physical device is updated even if the color has not changed.</param>
        public void SetColor(Color color, bool forceUpdate = false)
        {
            if (uniformColor != color || !uniform || forceUpdate)
            {
                uniform = true;
                uniformColor = color;

                colorRing.SetUniformColor(uniformColor);

                if (device != null && device.linked)
                {
                    int r = Mathf.RoundToInt(uniformColor.r * 255);
                    int g = Mathf.RoundToInt(uniformColor.g * 255);
                    int b = Mathf.RoundToInt(uniformColor.b * 255);

                    string payload = string.Format("{0}/{1}/{2}", r, g, b);
                    SendAction("set_color_all_leds", payload);
                }
            }
        }

        /// <summary>
        /// Sets the color of a single led.
        /// </summary>
        /// <param name="led">The index of the led the color is applied to.</param>
        /// <param name="color">The new color.</param>
        /// <param name="forceUpdate">Whether the physical device is updated even if the color has not changed.</param>
        public void SetColor(int led, Color color, bool forceUpdate = false)
        {
            if (colorRing.GetColor(led) != color || forceUpdate)
            {
                uniform = false;
                colorRing.SetSegmentColor(led, color);

                if (device != null && device.linked)
                {
                    int r = Mathf.RoundToInt(color.r * 255);
                    int g = Mathf.RoundToInt(color.g * 255);
                    int b = Mathf.RoundToInt(color.b * 255);

                    string payload = string.Format("{0}/{1}/{2}/{3}", led, r, g, b);
                    SendAction("set_color_one_led", payload);
                }
            }
        }

        /// <summary>
        /// Sets the intensity of leds.
        /// </summary>
        /// <param name="intensity">The intensity as a value from 0 to 1.</param>
        public void SetIntensity(float intensity, bool forceUpdate = false)
        {
            if (this.intensity != intensity || forceUpdate)
            {
                this.intensity = intensity;
                colorRing.SetIntensity(intensity);
                SendAction("set_intensity", Mathf.RoundToInt(intensity * 100));
            }
        }

        public void SetColorAndIntensity(Color color, float intensity, bool forceUpdate = false)
        {
            SetColor(color, forceUpdate);
            SetIntensity(intensity, forceUpdate);  
        }

        //
        //  Fading
        //

        /// <summary>
        /// Fades a color from a given intensity to another over the given duration.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="fromIntensity">The intensity at the start.</param>
        /// <param name="toIntensity">The intensity at the end.</param>
        /// <param name="duration">The time in seconds spent fading from the first intensity to the other.</param>
        public void StartFading(Color color, float fromIntensity, float toIntensity, float duration)
        {
            uniform = false;

            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            int from = Mathf.RoundToInt(fromIntensity * 100);
            int to = Mathf.RoundToInt(toIntensity * 100);

            int ms = Mathf.RoundToInt(duration * 1000);

            string payload = string.Format("{0}/{1}/{2}/{3}/{4}/{5}", r, g, b, from, to, ms);
            SendAction("start_fading", payload);

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(color, fromIntensity, toIntensity, duration));
        }

        /// <summary>
        /// Stops the fading effect.
        /// </summary>
        public void StopFading()
        {
            SendAction("stop_fading");
            StopAllCoroutines();
        }

        IEnumerator FadeRoutine(Color color, float fromIntensity, float toIntensity, float duration)
        {
            colorRing.SetUniformColor(color);

            float dir = (toIntensity - fromIntensity) / duration;
            intensity = fromIntensity;

            float min = Mathf.Min(fromIntensity, toIntensity);
            float max = Mathf.Max(fromIntensity, toIntensity);

            bool complete = false;

            while (!complete)
            {
                intensity += dir * Time.deltaTime;
                if (intensity <= min || intensity >= max)
                {
                    intensity = Mathf.Clamp(intensity, min, max);
                    complete = true;
                }
                colorRing.SetIntensity(intensity);
                yield return null;
            }
        }

        //
        //  Pulsing
        //

        /// <summary>
        /// Pulses a color from a given intensity to another and back again. This is a looping effect.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="fromIntensity">The first intensity.</param>
        /// <param name="toIntensity">The second intensity.</param>
        /// <param name="pulseLength">The time in seconds spent fading from the first intensity to the other and back to the first again./param>
        public void StartPulsing(Color color, float fromIntensity, float toIntensity, float pulseLength)
        {
            uniform = false;

            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            int from = Mathf.RoundToInt(fromIntensity * 100);
            int to = Mathf.RoundToInt(toIntensity * 100);

            int ms = Mathf.RoundToInt(pulseLength * 1000);

            string payload = string.Format("{0}/{1}/{2}/{3}/{4}/{5}", r, g, b, from, to, ms);
            SendAction("start_pulse", payload);

            StopAllCoroutines();
            StartCoroutine(PulseRoutine(color, fromIntensity, toIntensity, pulseLength));
        }

        /// <summary>
        /// Stops the pulsing effect
        /// </summary>
        public void StopPulsing()
        {
            SendAction("stop_pulse");
            StopAllCoroutines();
        }

        IEnumerator PulseRoutine(Color color, float fromIntensity, float toIntensity, float pulseLength)
        {
            colorRing.SetUniformColor(color);

            float dir = (toIntensity - fromIntensity) / (pulseLength / 2);
            intensity = fromIntensity;

            float min = Mathf.Min(fromIntensity, toIntensity);
            float max = Mathf.Max(fromIntensity, toIntensity);

            while (true)
            {
                intensity += dir * Time.deltaTime;
                if (intensity <= min || intensity >= max)
                {
                    intensity = Mathf.Clamp(intensity, min, max);
                    dir *= -1;
                }
                colorRing.SetIntensity(intensity);
                yield return null;
            }
        }

        //
        //  Rotation
        //

        /// <summary>
        /// Sets the rotation of the led ring
        /// </summary>
        /// <param name="rotation">The rotation in degrees</param>
        public void SetRotation(int rotation, bool forceUpdate = false)
        {
            uniform = false;

            if (this.rotation != rotation || forceUpdate)
            {
                this.rotation = rotation;
                SendAction("set_rotation", rotation);
                colorRing.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.back);
            }
        }


        /// <summary>
        /// Makes the led ring start rotating
        /// </summary>
        /// <param name="rotationTime">The time in seconds for one rotation</param>
        public void StartRotating(float rotationTime)
        {
            uniform = false;

            int ms = Mathf.RoundToInt(rotationTime * 1000);
            SendAction("start_rotating", ms);

            StopAllCoroutines();
            StartCoroutine(RotationRoutine(rotationTime));
        }

        /// <summary>
        /// Stops the rotating
        /// </summary>
        public void StopRotating()
        {
            SendAction("stop_rotating");
            StopAllCoroutines();
        }

        IEnumerator RotationRoutine(float rotationTime)
        {
            float rot = 0;
            //colorRing.transform.rotation = Quaternion.identity;
            while (true)
            {
                rot += 360 * Time.deltaTime / rotationTime;
                colorRing.SetRotation(rot);
                //colorRing.transform.Rotate(Vector3.back * 360 * Time.deltaTime / rotationTime);
                yield return null;
            }
        }

        //
        // Value changed callbacks
        //

        private void OnUniformColorChanged()
        {
            SetColor(uniformColor, true);
        }

        private void OnIntensityChanged()
        {
            SetIntensity(intensity, true);
        }

        private void OnRotationChanged()
        {
            SetRotation(rotation, true);
        }
    }
}
