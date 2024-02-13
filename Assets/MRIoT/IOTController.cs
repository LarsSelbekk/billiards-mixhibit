#nullable enable

using System;
using System.Collections.Generic;
using Exact;
using NaughtyAttributes;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(ExactManager))]
    public class IOTController : MonoBehaviour
    {
        private enum DeviceConfigType
        {
            DeviceName,
            DeviceType,
        }
        [Serializable]
        private class DeviceConfig
        {
            public DeviceConfigType type;
            public string value = null!;
            public Color connectedColor;
            public PocketEnum pocketLocation;
        }

        [SerializeField, Required] private Pocket pocketPrefab = null!;
        [SerializeField] private int connectedPulseTime = 10;
        [SerializeField] private int scoredFadeTime = 3;
        [SerializeField] private DeviceConfig[] deviceConfigs = { };

        private ExactManager _exactManager = null!;
        private readonly Dictionary<PocketEnum, Pocket> _pockets = new();

        private void Awake()
        {
            if (pocketPrefab == null)
            {
                throw new ArgumentNullException();
            }

            _exactManager = GetComponent<ExactManager>();
            if (_exactManager == null)
            {
                throw new MissingComponentException();
            }

            if (deviceConfigs.Length < PocketDefinition.PocketDefinitions.Length)
            {
                Debug.LogWarning($"{PocketDefinition.PocketDefinitions.Length} devices recommended, only {deviceConfigs.Length} configured");
            }
        }

        private void Start()
        {
            foreach (var config in deviceConfigs)
            {
                var pocket = Instantiate(pocketPrefab);
                switch (config.type)
                {
                    case DeviceConfigType.DeviceType:
                        if (config.value != pocket.Device.GetDeviceType())
                        {
                            throw new ArgumentException("Device Type must be set in the prefab due to missing functionality in the API");
                        }
                        pocket.Device.useDeviceType = true;
                        // TODO: Implement properly
                        // pocket.Device.SetDeviceType(config.value);
                        break;
                    case DeviceConfigType.DeviceName:
                        pocket.Device.SetDeviceName(config.value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _exactManager.AddDevice(pocket.Device);
                _pockets.Add(config.pocketLocation, pocket);

                pocket.LedRing.SetColor(config.connectedColor);
                pocket.Initialize(config.connectedColor, connectedPulseTime, config.pocketLocation);
            }
        }

        public void Scored(BallEnum ballEnum, PocketEnum pocketEnum)
        {
            var ballDefinition = BallDefinition.BallDefinitions[(int)ballEnum];
            var pocketDefinition = PocketDefinition.PocketDefinitions[(int)pocketEnum];
            Debug.Log($"IOTController Scored: {ballDefinition.Name} shot down in {pocketDefinition.Name}");
            if (pocketDefinition.Index >= _pockets.Count)
            {
                Debug.LogError($"IOTController Scored: {pocketDefinition.Index} out of bounds for {_pockets.Count} devices");
                return;
            }

            var pocket = _pockets[pocketEnum];
            if (pocket == null)
            {
                Debug.LogWarning($"IOTController Scored in pocket {pocketDefinition.Index} with no configured device, ignoring");
                return;
            }

            if (!pocket.Device.linked)
            {
                Debug.LogError($"IOTController Scored in pocket {pocketDefinition.Index} with no connected device");
                return;
            }

            var lowestIntensity = ballDefinition.IsStriped ? 0f : 0.3f;
            pocket.LedRing.StartPulsing(ballDefinition.Color, lowestIntensity, 1f, scoredFadeTime);
        }
    }
}
