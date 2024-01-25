using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class LowLevelRailBounce : MonoBehaviour
{
    [SerializeField]
    private float impulseMultiplier = 0.001f;

    private struct JobResultStruct
    {
        public int ThisInstanceID;
        public int OtherInstanceID;
        public Vector3 AverageNormal;
        // public bool noLongerColliding;
    }

    private NativeArray<JobResultStruct> _results;
    private int _count;
    private JobHandle _jobHandle;
    private HashSet<ValueTuple<int, int>> _collidingPairs = new();

    // TODO: Add set caching known non-rigidbodies and check unknown instances at runtime to allow collison with delayed spawn rbs
    private readonly Dictionary<int, Rigidbody> _rigidbodyByInstanceId = new();

    private void OnEnable()
    {
        _results = new NativeArray<JobResultStruct>(16, Allocator.Persistent);

        Physics.ContactEvent += OnPhysicsContact;

        var allRBs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (var rb in allRBs)
            _rigidbodyByInstanceId.Add(rb.GetInstanceID(), rb);
    }

    private void OnDisable()
    {
        _jobHandle.Complete();
        _results.Dispose();

        Physics.ContactEvent -= OnPhysicsContact;

        _rigidbodyByInstanceId.Clear();
    }

    private void FixedUpdate()
    {
        _jobHandle.Complete(); // The buffer is valid until the next Physics.Simulate() call. Be it internal or manual

        // Do something with the contact data.
        // E.g. Add force based on the average contact normal for that body
        for (int i = 0; i < _count; i++)
        {
            var thisInstanceID = _results[i].ThisInstanceID;
            var otherInstanceID = _results[i].OtherInstanceID;

            var rb0 = thisInstanceID != 0 ? _rigidbodyByInstanceId[thisInstanceID] : null;
            var rb1 = otherInstanceID != 0 ? _rigidbodyByInstanceId[otherInstanceID] : null;
            if (_results[i].AverageNormal.magnitude == 0) continue;

            for (var rbIndex = 0; rbIndex < 2; rbIndex++)
            {
                var sign = rbIndex == 0 ? 1 : -1;
                var rb = rbIndex == 0 ? rb0 : rb1;
                if (!rb) continue;
                var n = _results[i].AverageNormal;
                 var v0 = rb.velocity;
                            // TODO: include angular velocity at contact point, with vector
                            // var rotationCenterToContactPointVector = contact.point - collision.rigidbody.centerOfMass;
                            // var v0 = collision.relativeVelocity + Vector3.Cross(collision.rigidbody.angularVelocity, rotationCenterToContactPointVector);


                            // TODO: increase dampening with square of speed or something
                            var impulse = Mathf.Sqrt(impulseMultiplier) *
                                          (v0 - n * (2 * Vector3.Dot(n, v0)) / Mathf.Pow(n.magnitude, 2))
                                          - v0;

                        Debug.DrawRay(rb.position, n, Color.cyan, duration: 45);
                        Debug.DrawRay(rb.position, v0, Color.yellow, duration: 45);
                        Debug.DrawRay(rb.position + v0, impulse, Color.gray, duration: 45);
                        Debug.DrawRay(rb.position, v0 + impulse, Color.green, duration: 45);

                // var imp = sign * impulseMultiplier * rb.velocity.magnitude;
                // rb.AddForce(_results[i].AverageNormal * imp, ForceMode.VelocityChange);
                rb.AddForce(impulse, ForceMode.VelocityChange);
            }
        }
    }

    private void OnPhysicsContact(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly collisionPairs)
    {
        int numCollisionPairs = collisionPairs.Length;

        if (_results.Length < numCollisionPairs)
        {
            _results.Dispose();
            _results = new NativeArray<JobResultStruct>(Mathf.NextPowerOfTwo(numCollisionPairs), Allocator
                .Persistent);
        }

        _count = numCollisionPairs;

        AddForceJob job = new()
        {
            CollisionPairs = collisionPairs,
            Results = _results,
        };

        _jobHandle = job.Schedule(numCollisionPairs, 256);
    }

    private struct AddForceJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<ContactPairHeader>.ReadOnly CollisionPairs;

        public NativeArray<JobResultStruct> Results;

        public void Execute(int index)
        {
            Vector3 averageNormal = Vector3.zero;
            int count = 0;

            for (int j = 0; j < CollisionPairs[index].pairCount; j++)
            {
                ref readonly var pair = ref CollisionPairs[index].GetContactPair(j);

                // if (pair.isCollisionExit)
                if (!pair.isCollisionEnter)
                {
                    // _collidingPairs.Remove(ValueTuple.Create(pair.colliderInstanceID, pair
                        // .otherColliderInstanceID));
                    continue;
                }

                for (int k = 0; k < pair.contactCount; k++)
                {
                    ref readonly var contact = ref pair.GetContactPoint(k);
                    averageNormal += contact.normal;
                }

                count += pair.contactCount;
            }

            if (count != 0)
                averageNormal /= count;

            JobResultStruct result = new()
            {
                ThisInstanceID = CollisionPairs[index].bodyInstanceID,
                OtherInstanceID = CollisionPairs[index].otherBodyInstanceID,
                AverageNormal = averageNormal,
            };

            Results[index] = result;
        }
    }
}
