#nullable enable

using System;
using System.Collections;
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
        [SerializeField] private int scoredFadeTime = 10;
        [SerializeField] private int scoredAnimationTime = 2;
        [SerializeField] private int endPulseTime = 1;
        [SerializeField] private float scoredIntensity = 0.5f;
        [SerializeField] private int scoredSegments = 8;
        [SerializeField] private DeviceConfig[] deviceConfigs = { };

        private ExactManager _exactManager = null!;
        private readonly Dictionary<PocketEnum, Pocket> _pockets = new();
        private readonly Dictionary<PocketEnum, Coroutine> _coroutines = new();
        private bool _ended = false;

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

            var pocket = _pockets[pocketEnum];
            if (pocket == null)
            {
                Debug.LogWarning($"IOTController Scored in pocket {pocketDefinition.Enum} with no configured device, ignoring");
                return;
            }

            if (!pocket.Device.linked)
            {
                Debug.LogError($"IOTController Scored in pocket {pocketDefinition.Enum} with no connected device");
                return;
            }

            Scored(pocketDefinition, ballDefinition);
        }

        private void Scored(PocketDefinition pocketDefinition, BallDefinition ballDefinition)
        {
            // Start coroutine, stop if one is already running for this pocket, then add to map
            Debug.Log("IOTController Scored starting coroutine");
            var coroutine = StartCoroutine(SetPocketColorAndReset(pocketDefinition, ballDefinition));
            lock (_coroutines)
            {
                Debug.Log("IOTController Scored locked _coroutines");
                if (ballDefinition.Enum == BallEnum.Black)
                {
                    _ended = true;
                    foreach (var e in _coroutines.Keys)
                    {
                        _coroutines.Remove(e, out var previous);
                        StopCoroutine(previous);
                    }
                }
                else if (_ended)
                {
                    foreach (var e in _coroutines.Values)
                    {
                        StopCoroutine(e);
                    }
                    _ended = false;
                    foreach (var e in _pockets.Values)
                    {
                        e.LedRing.StopPulsing();
                        e.LedRing.SetColor(Color.black);
                    }
                }
                else if (_coroutines.Remove(pocketDefinition.Enum, out var previous))
                {
                    Debug.Log($"IOTController Scored stopping coroutine {previous}");
                    StopCoroutine(previous);
                }

                Debug.Log($"IOTController Scored adding coroutine {coroutine}");
                _coroutines.Add(pocketDefinition.Enum, coroutine);
            }
        }

        private IEnumerator SetPocketColorAndReset(PocketDefinition pocketDefinition, BallDefinition ballDefinition)
        {
            Debug.Log($"IOTController Coroutine entered");
            var pocket = _pockets[pocketDefinition.Enum];
            pocket.LedRing.StopPulsing();

            if (ballDefinition.Enum == BallEnum.Black)
            {
                foreach (var e in _pockets.Values)
                {
                    e.LedRing.StartPulsing(Color.white, 0.1f, 1f, endPulseTime);
                }
                yield break;
            }

            if (_ended)
            {
                Debug.LogError("IOTController Coroutine _ended should never be true here!");
            }

            if (!ballDefinition.IsStriped)
            {
                pocket.LedRing.StartPulsing(ballDefinition.Color, 0f, scoredIntensity, scoredAnimationTime);
            }
            else
            {
                pocket.LedRing.SetColorAndIntensity(ballDefinition.Color, scoredIntensity, true);
                var numLeds = pocket.LedRing.NumLeds;
                var segmentSize = numLeds / scoredSegments;
                Debug.Log(
                    $"IOTController Coroutine striped using {scoredSegments} segments of size {segmentSize} for {numLeds} LEDs");
                for (var i = 0; i < scoredSegments; i += 2)
                {
                    for (var j = 0; j < segmentSize; j++)
                    {
                        var led = segmentSize * i + j;
                        pocket.LedRing.SetColor(led, Color.black, true);
                    }
                }
                pocket.LedRing.StartRotating(scoredAnimationTime);
            }

            // var steps = 10f;
            // var fade = scoredIntensity / steps;
            // var wait = scoredFadeTime / steps;
            // Debug.Log($"IOTController Coroutine waiting for {wait} seconds before decreasing intensity by {fade}");
            // for (var i = scoredIntensity; i > 0f; i -= fade)
            // {
            //     yield return new WaitForSeconds(wait);
            //     Debug.Log($"IOTController Coroutine setting intensity to {i}");
            //     pocket.LedRing.SetIntensity(i);
            // }

            Debug.Log($"IOTController Coroutine [{pocketDefinition.Name}, {ballDefinition.Name}]waiting for {scoredFadeTime} seconds");
            yield return new WaitForSeconds(scoredFadeTime);
            Debug.Log($"IOTController Coroutine for [{pocketDefinition.Name}, {ballDefinition.Name}] resuming and shutting down");
            if (ballDefinition.IsStriped)
            {
                pocket.LedRing.StopRotating();
                pocket.LedRing.SetRotation(0);
            }
            pocket.LedRing.SetColor(Color.black);
        }
    }
}
