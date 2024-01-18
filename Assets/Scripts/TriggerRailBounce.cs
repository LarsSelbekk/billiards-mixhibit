using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using System.Linq.Expressions;
using UnityEngine.Assertions;

public class TriggerRailBounce : MonoBehaviour
{
    [SerializeField] private float energyMultiplier;

    // private BoxCollider _collider;

    private readonly List<Impact> _impacts = new();
    private Vector3[] _corners = new Vector3[8];

    private record Impact
    {
        public Vector3 Point, BallPosition;
        public Vector3 V0, V, I, N;
        public float StartTime;
    }

    private void Awake()
    {
        // _collider = GetComponent<BoxCollider>();
    }

    // Requires that the collider is set to Use Trigger
    void OnTriggerEnter(Collider ballCollider)
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        var ball = ballCollider.gameObject;
        var ballRigidbody = ball.GetComponent<Rigidbody>();

        Debug.Log($"#Trigger enter {ball.name}");


        // var collisionPoint = _collider.ClosestPoint(ballRigidbody.position);
        var collisionPoint = PointOnSurfaceClostestTo(ballRigidbody.position);
        var n = (collisionPoint - ballRigidbody.position).normalized;
        // var n = new Vector3(contact.normal.x, 0, contact.normal.z);
        // var v0 = collision.relativeVelocity;
        var ballVelocity = ballRigidbody.velocity;
        var v0 = new Vector3(ballVelocity.x, 0, ballVelocity.z);
        // var v0 = collision.rigidbody.velocity;
        // var rotationCenterToContactPointVector = contact.point - collision.rigidbody.centerOfMass;
        // var v0 = collision.relativeVelocity + Vector3.Cross(collision.rigidbody.angularVelocity, rotationCenterToContactPointVector);

        var impulse = Mathf.Sqrt(energyMultiplier) *
                      (v0 - 2 * n * Vector3.Dot(n, v0))
                      - v0;


        Debug.Log($"#Trigger col: {collisionPoint}, v0: {v0}, i: {impulse}");
        Debug.DrawRay(collisionPoint, n * 0.1f, Color.cyan, duration: 45);
        Debug.DrawRay(collisionPoint, v0, Color.yellow, duration: 45);
        Debug.DrawRay(collisionPoint + v0, impulse, Color.gray, duration: 45);
        Debug.DrawRay(collisionPoint, v0 + impulse, Color.green, duration: 45);
        // Debug.DrawRay(contact.point, n, Color.cyan, duration: 45);

        _impacts.Add(new Impact
        {
            Point = collisionPoint,
            V0 = v0,
            V = v0 + impulse,
            I = impulse,
            N = n,
            BallPosition = ball.transform.position,
            StartTime = Time.time
        });

        // var abovePoint = contact.point + Vector3.up * 2f
        // _impacts.Add(new Impact(){
        //     Point = abovePoint,
        //     V0 = collision.rigidbody.velocity.magnitude,
        //     V =
        //     });
        // Debug.DrawRay(contact.point, v0, Color.yellow, duration: 45);
        // Debug.DrawRay(contact.point + v0, impulse, Color.gray, duration: 45);
        // Debug.DrawRay(contact.point, v0 + impulse, Color.green, duration: 45);
        ballRigidbody.AddForce(impulse, ForceMode.VelocityChange);
        // collision.rigidbody.AddTorque(
        //     -collision.rigidbody.angularVelocity * (1 / Mathf.Sqrt(energyMultiplier)), ForceMode
        //         .VelocityChange);
        // collision.rigidbody.AddForceAtPosition(impulse * collision.rigidbody.mass, contact.point, ForceMode.Impulse);
        // collision.rigidbody.AddForceAtPosition(impulse * collision.rigidbody.mass, contact.point, ForceMode.Impulse);
    }

    public Vector3 PointOnSurfaceClostestTo(Vector3 point)
    {
        // convert point to local space
        point = transform.InverseTransformPoint(point);

        var boxCollider = GetComponent<BoxCollider>();
        var corners = new Vector3[8];
        var size = boxCollider.size;
        for (var i = 0; i < 8; i++)
        {
            corners[i] = boxCollider.center + new Vector3(
                size.x * (i % 1 * 2 - 1),
                size.y * (i % 2 * 2 - 1),
                size.z * (i % 4 * 2 - 1));
        }

        // plane spanned by vectors 0 -> 1 and 0 -> 2
        // 1 x 2 must point out of the box by right-hand rule
        var planes = new ValueTuple<Vector3, Vector3, Vector3>[]
        {
            new(corners[0b000], corners[0b010], corners[0b001]),
            new(corners[0b000], corners[0b100], corners[0b010]),
            new(corners[0b000], corners[0b001], corners[0b100]),
            new(corners[0b111], corners[0b110], corners[0b101]),
            new(corners[0b111], corners[0b101], corners[0b011]),
            new(corners[0b111], corners[0b101], corners[0b011]),
        };
        // TODO: verify that the length is 8
        corners = GetComponent<MeshFilter>().mesh.vertices.GroupBy(v => v).Select(g => g.First()).ToArray();
        Assert.AreEqual(8, corners.Length, "Can't be more than 8 corners in a box; deduplication failed");

        _corners = corners.Select(p => transform.TransformPoint(p)).ToArray();

        var normals = new Vector3[6];
        // scan all planes to find nearest
        for (var i = 0; i < 6; i++)
        {
            var plane = planes[i];
            var u = plane.Item2 - plane.Item1;
            var v = plane.Item3 - plane.Item1;
            normals[i] = Vector3.Project(point, Vector3.Cross(u, v));
        }

        var smallestNormal = Vector3.positiveInfinity;
        for (var i = 0; i < 6; i++)
        {
            if (normals[i].magnitude < smallestNormal.magnitude)
            {
                smallestNormal = normals[i];
            }
        }

        // convert nearest vertex back to world space
        return transform.TransformPoint(point - smallestNormal);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        foreach (var impact in _impacts.ToArray())
        {
            if (Time.time - impact.StartTime >= 45)
            {
                _impacts.Remove(impact);
            }
            else
            {
                Handles.Label(impact.Point + Vector3.up * 0.005f,
                    $"v0: {impact.V0}\nv: {impact.V}\ni: {impact.I}\nn: {impact.N}");
                Gizmos.DrawSphere(impact.BallPosition, 0.001f);
                Gizmos.DrawCube(impact.Point, Vector3.one * 0.001f);
            }
        }

        foreach (var corner in _corners)
        {
            Gizmos.DrawCube(corner, Vector3.one * 0.004f);
        }
    }
#endif
}
