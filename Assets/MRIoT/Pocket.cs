#nullable enable

using Attributes;
using Exact;
using Exact.Example;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(Device))]
    [RequireComponent(typeof(LedRing))]
    public class Pocket : MonoBehaviour
    {
        public Device Device { get; private set; } = null!;
        public LedRing LedRing {get; private set; } = null!;

        [SerializeField, ReadOnlyInInspector] private Color connectedColor = Color.white;
        [SerializeField, ReadOnlyInInspector] private float connectedPulseTime = 10;

        [SerializeField] private Color disconnectedColor = Color.gray;
        [SerializeField] private float disconnectedIntensity = 0.5f;

        private void Awake()
        {
            Device = GetComponent<Device>();
            LedRing = GetComponent<LedRing>();
            if (Device == null || LedRing == null)
            {
                throw new MissingComponentException();
            }
        }

        public void SetConnectedColorAndPulseTime(Color newColor, float newConnectedPulseTime)
        {
            connectedColor = newColor;
            connectedPulseTime = newConnectedPulseTime;
            if (Device.GetLinkStatus())
            {
                Connected();
            }
        }

        public void Connected()
        {
            LedRing.StartPulsing(connectedColor, 0, 1, connectedPulseTime);
        }

        public void Disconnected()
        {
            LedRing.SetColorAndIntensity(disconnectedColor, disconnectedIntensity);
        }
    }
}
