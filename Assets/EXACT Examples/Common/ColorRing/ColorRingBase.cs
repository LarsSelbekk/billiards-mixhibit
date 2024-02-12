using UnityEngine;

using System.Collections.Generic;

namespace Exact.Example
{
    public abstract class ColorRingBase : MonoBehaviour
    {
        protected int numSegments = 0;
        protected float intensity = 1.0f;
        protected float rotation = 0.0f;
        protected List<Color> segmentColors = new List<Color>();

        public abstract void SetNumberOfSegments(int num);
        protected abstract void SetSegmentColorInternal(int segment, Color color);

        public virtual void SetSegmentColor(int segment, Color color)
        {
            if (segment >= numSegments)
            {
                Debug.LogError("Index out of range");
                return;
            }
            segmentColors[segment] = color;
            Color displayColor = color * intensity;
            displayColor.a = 1;
            SetSegmentColorInternal(GetSegmentIndex(numSegments, segment, rotation), displayColor);
        }

        public virtual void SetUniformColor(Color color)
        {
            Color displayColor = color * intensity;
            displayColor.a = 1;

            for (int i = 0; i < numSegments; i++)
            {
                segmentColors[i] = color;
                SetSegmentColorInternal(i, displayColor);
            }
        }

        public virtual Color GetColor(int segment)
        {
            if (segment >= numSegments)
            {
                Debug.LogError("Index out of range");
                return Color.black;
            }
            return segmentColors[segment];
        }

        public virtual void SetIntensity(float intensity)
        {
            this.intensity = intensity;

            for (int i = 0; i < numSegments; i++)
            {
                Color displayColor = segmentColors[i] * intensity;
                displayColor.a = 1;
                SetSegmentColorInternal(GetSegmentIndex(numSegments, i, rotation), displayColor);
            }
        }

        public virtual void SetRotation(float rotation)
        {
            this.rotation = rotation;
            for (int i = 0; i < numSegments; i++)
            {
                Color displayColor = segmentColors[i] * intensity;
                displayColor.a = 1;
                SetSegmentColorInternal(GetSegmentIndex(numSegments, i, rotation), displayColor);
            }
        }

        protected int GetSegmentIndex(int numSegments, int segment, float rotation)
        {
            float oneSegRot = 360 / numSegments;
            return (Mathf.FloorToInt(rotation / oneSegRot) + segment) % numSegments;
        }
    }
}
