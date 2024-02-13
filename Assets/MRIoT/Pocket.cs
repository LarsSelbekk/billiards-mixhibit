#nullable enable

using System;
using Exact;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(Device))]
    [RequireComponent(typeof(ModifiedLedRing))]
    public class Pocket : MonoBehaviour
    {
        public enum PocketPairingType {
            DeviceName,
            DeviceType,
        }

        /**
         * Individual configuration, different for each instance
         */
        [Serializable]
        public class PocketDeviceConfig
        {
            public PocketPairingType pairingType;
            public string pairingValue = null!;
            public Color connectedColor;
            public PocketEnum pocketLocation;
        }

        /**
         * Common configuration, shared by all instances
         */
        [Serializable]
        public class PocketCommonConfig
        {
            public int connectedAnimationTime = 10;
            public float connectedMinIntensity = 0.3f;
            public float connectedMaxIntensity = 1f;
            public int endAnimationTime = 1;
            public float endMinIntensity = 0.2f;
            public float endMaxIntensity = 1f;
            public int scoredFadeTime = 10;
            public int scoredSegments = 8;
            public int scoredAnimationTime = 2;
            public float scoredMinIntensity; // = 0f;
            public float scoredMaxIntensity = 0.5f;
        }

        private class GenericValueLock<T>
        {
            public T Value;

            public GenericValueLock(T value)
            {
                Value = value;
            }
        }

        private Device _device = null!;
        private ModifiedLedRing _ledRing = null!;

        [SerializeField] private Color disconnectedColor = Color.gray;
        [SerializeField] private float disconnectedIntensity = 0.5f;

        // Config
        private PocketEnum _pocketLocation;
        private Color _connectedColor;

        // Props
        private int _connectedPulseTime;
        private float _connectedMinIntensity;
        private float _connectedMaxIntensity;
        private int _endPulseTime;
        private float _endMinIntensity;
        private float _endMaxIntensity;
        // private int _scoredFadeTime;
        private int _scoredSegments;
        private int _scoredAnimationTime;
        private float _scoredMinIntensity;
        private float _scoredMaxIntensity;

        // Computed
        private int _scoredSegmentSize;

        // Dynamic / Local
        private readonly GenericValueLock<BallEnum?> _lastBall = new(null);

        private void Awake()
        {
            _device = GetComponent<Device>();
            _ledRing = GetComponent<ModifiedLedRing>();
            if (_device == null || _ledRing == null)
            {
                throw new MissingComponentException();
            }
        }

        public void Initialize(PocketDeviceConfig deviceConfig, PocketCommonConfig commonConfig, ExactManager exactManager)
        {
            _connectedColor = deviceConfig.connectedColor;
            _pocketLocation = deviceConfig.pocketLocation;

            _connectedPulseTime = commonConfig.connectedAnimationTime;
            _connectedMinIntensity = commonConfig.connectedMinIntensity;
            _connectedMaxIntensity = commonConfig.connectedMaxIntensity;
            _endPulseTime = commonConfig.endAnimationTime;
            _endMinIntensity = commonConfig.endMinIntensity;
            _endMaxIntensity = commonConfig.endMaxIntensity;
            // _scoredFadeTime = props.scoredFadeTime;
            _scoredSegments = commonConfig.scoredSegments;
            _scoredAnimationTime = commonConfig.scoredAnimationTime;
            _scoredMinIntensity = commonConfig.scoredMinIntensity;
            _scoredMaxIntensity = commonConfig.scoredMaxIntensity;

            _scoredSegmentSize = _ledRing.GetNumLeds() / commonConfig.scoredSegments;

            switch (deviceConfig.pairingType)
            {
                case PocketPairingType.DeviceName:
                    _device.SetDeviceName(deviceConfig.pairingValue);
                    break;
                case PocketPairingType.DeviceType:
                    if (deviceConfig.pairingValue != _device.GetDeviceType())
                    {
                        throw new NotImplementedException(
                            "Device Type must be set in the prefab due to missing functionality in the API");
                    }
                    _device.useDeviceType = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            exactManager.AddDevice(_device);

            // Refresh if already connected
            if (_device.GetLinkStatus())
            {
                Connected();
            }
        }

        public void Connected()
        {
            _ledRing.StartPulsing(_connectedColor, _connectedMinIntensity, _connectedMaxIntensity, _connectedPulseTime);
            var detectors = FindObjectsByType<PocketDetector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PocketDetector? selected = null;
            foreach (var e in detectors)
            {
                if (e.GetPocketLocation() != _pocketLocation) continue;
                selected = e;
                e.SetColor(_connectedColor);
                break;
            }
            Debug.Log($"Pocket Connected called SetColor on {selected}");
        }

        public void Disconnected()
        {
            _ledRing.SetColorAndIntensity(disconnectedColor, disconnectedIntensity);
            var detectors = FindObjectsByType<PocketDetector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PocketDetector? selected = null;
            foreach (var e in detectors)
            {
                if (e.GetPocketLocation() != _pocketLocation) continue;
                selected = e;
                e.SetColor(disconnectedColor);
                break;
            }
            Debug.Log($"Pocket Disconnected called SetColor on {selected}");
        }

        public bool Scored(BallDefinition ballDefinition)
        {
            Debug.Log($"Pocket Scored {ballDefinition.Name} in pocket {_pocketLocation}");
            lock (_lastBall)
            {
                _lastBall.Value = ballDefinition.Enum;
            }

            // Reset and check connection state
            if (!ResetScored())
                return false;

            if (ballDefinition.IsStriped)
                ScoredStriped(ballDefinition.Color);
            else
                ScoredSolid(ballDefinition.Color);

            return true;
        }

        private void ScoredSolid(Color color)
        {
            _ledRing.StartPulsing(color, _scoredMinIntensity, _scoredMaxIntensity, _scoredAnimationTime);
        }

        private void ScoredStriped(Color color)
        {
            // Set base color
            _ledRing.SetColorAndIntensity(color, _scoredMaxIntensity, true);

            // Set stripes to black
            for (var i = 0; i < _scoredSegments; i += 2)
            {
                for (var j = 0; j < _scoredSegmentSize; j++)
                {
                    var led = _scoredSegmentSize * i + j;
                    _ledRing.SetColor(led, Color.black, true);
                }
            }

            // Start rotation animation
            _ledRing.StartRotating(_scoredAnimationTime);
        }

        /**
         * Resets the pocket from score.
         *
         * Returns true if ran to completion (successfully reset).
         */
        public bool ResetScored()
        {
            if (!_device.GetLinkStatus())
            {
                Debug.LogWarning($"Pocket ResetScored pocket {_pocketLocation} has no connected device");
                return false;
            }

            _ledRing.ResetModifications();
            return true;
        }

        public void ScoreBlack()
        {
            lock (_lastBall)
            {
                _lastBall.Value = BallEnum.Black;
            }
            _ledRing.StartPulsing(Color.white, _endMinIntensity, _endMaxIntensity, _endPulseTime);
        }

        public void ResetBlack()
        {
            lock (_lastBall)
            {
                if (_lastBall.Value != BallEnum.Black) return;

                _lastBall.Value = null;
                ResetScored();
            }
        }
    }
}
