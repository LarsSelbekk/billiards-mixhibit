using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

namespace Exact.Example
{
    [RequireComponent(typeof(Device))]
    public class IMU : DeviceComponent
    {
        public override string GetComponentType() { return "imu"; }

        [SerializeField, OnValueChanged("OnSensitivityChanged"), Range(0, 1)]
        float sensitivity = 0.975f;

        public UnityEvent OnTap;

        public void OnConnect()
        {
            SetSensitivity(sensitivity, true);
        }

        public override void OnEvent(string eventType, byte[] payload)
        {
            switch (eventType)
            {
                case "tapped":
                    Tap();
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Trigger a tap event.
        /// Also called when a tap is detected by the physical device.
        /// </summary>
        [Button]
        public void Tap()
        {
            Debug.Log("Tap!");
            OnTap.Invoke();
        }

        /// <summary>
        /// Sets the sensitivity for the IMU when detecting a tap.
        /// </summary>
        /// <param name="sensitivity">The new sensitivity as a value from 0 to 1.</param>
        /// <param name="forceUpdate">Whether the physical device is updated even if the sensitivity has not changed.</param>
        public void SetSensitivity(float sensitivity, bool forceUpdate = false)
        {
            if (this.sensitivity != sensitivity || forceUpdate)
            {
                this.sensitivity = sensitivity;
                SendAction("set_sensitivity", Mathf.RoundToInt((1.0f - sensitivity) * 2046 + 1));
            }
        }

        //
        // Value changed callbacks
        //

        private void OnSensitivityChanged()
        {
            SetSensitivity(sensitivity, true);
        }
    }
}
