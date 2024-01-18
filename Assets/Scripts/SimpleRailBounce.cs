// #define DEBUG_BOUNCES

#if DEBUG_BOUNCES
using System.Collections.Generic;
using UnityEditor;
#endif
using UnityEngine;

public class SimpleRailBounce : MonoBehaviour
{
    [SerializeField] private float energyMultiplier;

#if DEBUG_BOUNCES
    private readonly List<Impact> _impactsToVisualize = new();

    private record Impact
    {
        public Vector3 Point;
        public float InitialVelocity;
        public float NewVelocity;
        public float DeltaVelocity;
        public float ImpactTime;
    }
#endif

    void OnCollisionEnter(Collision collision)
    {
        if (!enabled || !gameObject.activeInHierarchy) return;

        foreach (var contact in collision.contacts)
        {
            // Alternative normal definitions
            var normal = contact.normal.normalized;
            // var n = new Vector3(contact.normal.x, 0, contact.normal.z);

            // Alternative initial velocity definitions
            var initialVelocity = new Vector3(collision.relativeVelocity.x, 0, collision.relativeVelocity.z);
            // var v0 = collision.relativeVelocity;
            // var v0 = collision.rigidbody.velocity;
            // var rotationCenterToContactPointVector = contact.point - collision.rigidbody.centerOfMass;
            // var v0 = collision.relativeVelocity + Vector3.Cross(collision.rigidbody.angularVelocity, rotationCenterToContactPointVector);

            var deltaVelocity = Mathf.Sqrt(energyMultiplier) *
                                (initialVelocity - 2 * normal * Vector3.Dot(normal, initialVelocity))
                                - initialVelocity;


            // Debugging
#if DEBUG_BOUNCES
            Debug.Log($"Collision enter {collision.rigidbody.gameObject.name}");
            Debug.Log(contact.normal);
            Debug.DrawRay(contact.point, initialVelocity, Color.yellow, duration: 45);
            Debug.DrawRay(contact.point + initialVelocity, deltaVelocity, Color.gray, duration: 45);
            Debug.DrawRay(contact.point, initialVelocity + deltaVelocity, Color.green, duration: 45);
            Debug.DrawRay(contact.point, normal, Color.cyan, duration: 45);
            _impactsToVisualize.Add(new Impact
            {
                Point = contact.point,
                InitialVelocity = initialVelocity.magnitude,
                NewVelocity = (initialVelocity + deltaVelocity).magnitude,
                DeltaVelocity = deltaVelocity.magnitude,
                ImpactTime = Time.time
            });
#endif

            // Alternative force application methods
            collision.rigidbody.velocity = initialVelocity + deltaVelocity;
            // collision.rigidbody.AddForce(deltaVelocity, ForceMode.VelocityChange);
            // collision.rigidbody.AddTorque(
            //     -collision.rigidbody.angularVelocity * (1 / Mathf.Sqrt(energyMultiplier)), ForceMode
            //         .VelocityChange);
            // collision.rigidbody.AddForceAtPosition(deltaVelocity * collision.rigidbody.mass, contact.point, ForceMode.Impulse);
        }
    }

#if UNITY_EDITOR && DEBUG_BOUNCES
    private void OnDrawGizmos()
    {
        foreach (var impact in _impactsToVisualize.ToArray())
        {
            if (Time.time - impact.ImpactTime >= 45)
            {
                _impactsToVisualize.Remove(impact);
            }
            else
            {
                Handles.Label(impact.Point + Vector3.up * 0.05f,
                    $"v0: {impact.InitialVelocity}\nv: {impact.NewVelocity}\ni: {impact.DeltaVelocity}");
            }
        }
    }
#endif
}
