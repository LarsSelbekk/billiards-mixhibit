using UnityEngine;

namespace Utils
{
    public class BoundUtils
    {
        public static Bounds GetObjectAndChildrenComponentBounds<T>(GameObject obj)
        {
            var t = typeof(T);
            return t == typeof(Renderer) ? GetObjectAndChildrenRendererBounds(obj) :
                t == typeof(Mesh) ? GetObjectAndChildrenMeshBounds(obj) :
                t == typeof(Collider) ? GetObjectAndChildrenColliderBounds(obj) : new Bounds();
        }

        private static Bounds GetObjectAndChildrenRendererBounds(GameObject obj)
        {
            var bounds = obj.GetComponent<Renderer>()?.bounds ?? new Bounds();
            foreach (var c in obj.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(c.bounds);
            }

            return bounds;
        }

        private static Bounds GetObjectAndChildrenMeshBounds(GameObject obj)
        {
            var bounds = obj.GetComponent<Mesh>()?.bounds ?? new Bounds();
            foreach (var c in obj.GetComponentsInChildren<Mesh>())
            {
                bounds.Encapsulate(c.bounds);
            }

            return bounds;
        }

        private static Bounds GetObjectAndChildrenColliderBounds(GameObject obj)
        {
            var bounds = obj.GetComponent<Collider>()?.bounds ?? new Bounds();
            foreach (var c in obj.GetComponentsInChildren<Collider>())
            {
                bounds.Encapsulate(c.bounds);
            }

            return bounds;
        }
    }
}