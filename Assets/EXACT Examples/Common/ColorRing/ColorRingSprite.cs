using UnityEngine;

using System.Collections.Generic;

namespace Exact.Example
{
    public class ColorRingSprite : ColorRingBase
    {
        [SerializeField]
        GameObject segmentPrefab;

        [SerializeField]
        float ledSize = 0.4f;

        List<SpriteRenderer> segments = new List<SpriteRenderer>();

        public override void SetNumberOfSegments(int num)
        {
            if (num == numSegments) { return; }
            else if (num > numSegments)
            {
                for (int i = numSegments; i < num; i++)
                {
                    segments.Add(Instantiate(segmentPrefab, transform).GetComponent<SpriteRenderer>());
                    segmentColors.Add(Color.black);
                }
            }
            else
            {
                for (int i = numSegments - 1; i >= num; i--)
                {
                    segments.RemoveAt(i);
                    segmentColors.RemoveAt(i);
                }
            }

            numSegments = num;

            float r = 0.5f - ledSize / 2;
            float segmentSize = Mathf.PI * 2 / num;

            for (int i = 0; i < num; i++)
            {
                float t = segmentSize * -i - Mathf.PI / 2;
                segments[i].transform.localPosition = new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r, -0.1f);
                segments[i].transform.localScale = Vector3.one * ledSize;
            }
        }

        protected override void SetSegmentColorInternal(int segment, Color color)
        {
            segments[segment].color = color;
        }
    }
}
