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

        [SerializeField, ReadOnlyInInspector] private PocketEnum pocketLocation;

        private void Awake()
        {
            Device = GetComponent<Device>();
            LedRing = GetComponent<LedRing>();
            if (Device == null || LedRing == null)
            {
                throw new MissingComponentException();
            }
        }

        public void Initialize(Color newColor, float newConnectedPulseTime, PocketEnum newPocketLocation)
        {
            connectedColor = newColor;
            connectedPulseTime = newConnectedPulseTime;
            pocketLocation = newPocketLocation;
            if (Device.GetLinkStatus())
            {
                Connected();
            }
        }

        public void Connected()
        {
            LedRing.StartPulsing(connectedColor, 0, 1, connectedPulseTime);
            var detectors = FindObjectsByType<PocketDetector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PocketDetector? selected = null;
            foreach (var e in detectors)
            {
                if (e.GetPocketLocation() != pocketLocation) continue;
                selected = e;
                e.SetColor(connectedColor);
                break;
            }
            Debug.LogWarning($"Pocket Connected called SetColor on {selected}");
        }

        public void Disconnected()
        {
            LedRing.SetColorAndIntensity(disconnectedColor, disconnectedIntensity);
            var detectors = FindObjectsByType<PocketDetector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PocketDetector? selected = null;
            foreach (var e in detectors)
            {
                if (e.GetPocketLocation() != pocketLocation) continue;
                selected = e;
                e.SetColor(disconnectedColor);
                break;
            }
            Debug.LogWarning($"Pocket Disconnected called SetColor on {selected}");
        }
    }
}
