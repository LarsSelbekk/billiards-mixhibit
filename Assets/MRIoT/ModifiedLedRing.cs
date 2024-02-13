#nullable enable

using System;
using Exact.Example;
using UnityEngine;

namespace MRIoT
{
    [RequireComponent(typeof(LedRing))]
    public class ModifiedLedRing : MonoBehaviour
    {
        private LedRing _ledRing = null!;

        private bool HasColor { get; set; }
        private bool HasIntensity { get; set; }
        private bool HasRotation { get; set; }
        private bool IsFading { get; set; }
        private bool IsPulsing { get; set; }
        private bool IsRotating { get; set; }

        [SerializeField] private Color defaultColor = Color.black;
        [SerializeField] private float defaultIntensity; // = 0f;
        [SerializeField] private float intensityTolerance = 0.01f;
        [SerializeField] private int defaultRotation; // = 0;

        private void Awake()
        {
            _ledRing = GetComponent<LedRing>();
            if (_ledRing == null)
            {
                throw new MissingComponentException("LedRing");
            }
        }

        public void SetColor(Color color, bool forceUpdate = false)
        {
            HasColor = color != defaultColor;
            _ledRing.SetColor(color, forceUpdate);
        }

        public void SetColor(int led, Color color, bool forceUpdate = false)
        {
            HasColor = true;
            _ledRing.SetColor(led, color, forceUpdate);
        }

        public void SetIntensity(float intensity, bool forceUpdate = false)
        {
            HasIntensity = Math.Abs(intensity - defaultIntensity) > intensityTolerance;
            _ledRing.SetIntensity(intensity, forceUpdate);
        }

        public void SetColorAndIntensity(Color color, float intensity, bool forceUpdate = false)
        {
            HasColor = color != defaultColor;
            HasIntensity = Math.Abs(intensity - defaultIntensity) > intensityTolerance;
            _ledRing.SetColorAndIntensity(color, intensity, forceUpdate);
        }

        public void StartFading(Color color, float fromIntensity, float toIntensity, float duration)
        {
            HasColor = color != defaultColor;
            HasIntensity = true;
            IsFading = true;
            _ledRing.StartFading(color, fromIntensity, toIntensity, duration);
        }

        public void StopFading()
        {
            IsFading = false;
            _ledRing.StopFading();
        }

        public void StartPulsing(Color color, float fromIntensity, float toIntensity, float pulseLength)
        {
            HasColor = color != defaultColor;
            HasIntensity = true;
            IsPulsing = true;
            _ledRing.StartPulsing(color, fromIntensity, toIntensity, pulseLength);
        }

        public void StopPulsing()
        {
            IsPulsing = false;
            _ledRing.StopPulsing();
        }

        public void SetRotation(int rotation, bool forceUpdate = false)
        {
            HasRotation = rotation != defaultRotation;
            _ledRing.SetRotation(rotation, forceUpdate);
        }

        public void StartRotating(float rotationTime)
        {
            HasRotation = true;
            IsRotating = true;
            _ledRing.StartRotating(rotationTime);
        }

        public void StopRotating()
        {
            IsRotating = false;
            _ledRing.StopRotating();
        }

        public void ResetModifications()
        {
            // Reset dynamic first
            if (IsFading)
                _ledRing.StopFading();
            if (IsPulsing)
                _ledRing.StopPulsing();
            if (IsRotating)
                _ledRing.StopRotating();

            // Reset static last
            if (HasColor || HasIntensity)
                _ledRing.SetColorAndIntensity(defaultColor, defaultIntensity);
            if (HasRotation)
                _ledRing.SetRotation(0);
        }

        public int GetNumLeds()
        {
            return _ledRing.NumLeds;
        }
    }
}
