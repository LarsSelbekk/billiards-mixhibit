using UnityEngine;
using UnityEngine.UI;

using System.Collections.Generic;

namespace Exact.Example
{
    public class ColorRingUI : ColorRingBase
    {
        [SerializeField]
        GameObject segmentPrefab;

        List<Image> segments = new List<Image>();

        public override void SetNumberOfSegments(int num)
        {
            if (num == numSegments) { return; }

            else if (num > numSegments)
            {
                for (int i = numSegments; i < num; i++)
                {
                    segments.Add(Instantiate(segmentPrefab, transform, false).GetComponent<Image>());
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

            float fillAmount = 1.0f / num;
            float segmentSize = 360.0f / num;

            for (int i = 0; i < num; i++)
            {
                segments[i].fillAmount = fillAmount;
                segments[i].rectTransform.localRotation = Quaternion.AngleAxis(segmentSize * i, -Vector3.forward);
            }
        }

        protected override void SetSegmentColorInternal(int segment, Color color)
        {
            segments[segment].color = color;
        }
    }
}
