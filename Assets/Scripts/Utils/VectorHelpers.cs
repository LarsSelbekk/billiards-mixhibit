using UnityEngine;

namespace Utils
{
    public static class VectorHelpers
    {
        public static Vector3 AlignedToXZPlane(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
