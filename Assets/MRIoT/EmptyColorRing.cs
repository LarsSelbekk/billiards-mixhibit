#nullable enable

using Exact.Example;
using UnityEngine;

namespace MRIoT
{
    public class EmptyColorRing : ColorRingBase
    {
        public override void SetNumberOfSegments(int num)
        {
            Debug.Log("EmptyColorRing setting number of segments");

            if (num == numSegments) { return; }

            if (num > numSegments)
            {
                for (int i = numSegments; i < num; i++)
                {
                    segmentColors.Add(Color.black);
                }
            }
            else
            {
                for (int i = numSegments - 1; i >= num; i--)
                {
                    segmentColors.RemoveAt(i);
                }
            }

            numSegments = num;
        }

        protected override void SetSegmentColorInternal(int segment, Color color)
        {
            // Debug.Log("EmptyColorRing set segment color internal ignored");
        }
    }
}
