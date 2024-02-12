#nullable enable

using Attributes;
using Exact;
using Exact.Example;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(Device))]
    [RequireComponent(typeof(LedRing))]
    public class Hole : MonoBehaviour
    {
        public Device Device { get; private set; } = null!;
        public LedRing LedRing {get; private set; } = null!;

        [SerializeField, ReadOnlyInInspector] private Color connectedColor = Color.white;
        [SerializeField] private Color disconnectedColor = Color.gray;
        [SerializeField] private float disconnectedIntensity = 0.5f;
        [SerializeField] private float connectedPulseTime = 10;

        private void Awake()
        {
            Device = GetComponent<Device>();
            LedRing = GetComponent<LedRing>();
            if (Device == null || LedRing == null)
            {
                throw new MissingComponentException();
            }
        }

        public void SetConnectedColor(Color newColor)
        {
            connectedColor = newColor;
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
