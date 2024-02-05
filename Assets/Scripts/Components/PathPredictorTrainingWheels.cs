// #define DEBUG_TRAINING_WHEELS

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Utils;

#if DEBUG_TRAINING_WHEELS
using System;
using Color = UnityEngine.Color;
#endif

[RequireComponent(typeof(LineRenderer))]
public class PathPredictorTrainingWheels : MonoBehaviour
{
    public MeshFilter cueTip;
    public LayerMask cueRayLayerMask;
    public LayerMask cueBallRayLayerMask;
    public LayerMask cueBallLayerMask;
    public MeshCollider tableSurface;
    public float maxHorizontalDistanceToCueBall = 1.0f;
    public float maxVerticalDistanceToCueBall = 0.5f;
    public float maxCueBallRollDistance = 1f;
    public float ballRadius = 0.05715f / 2;

    private LineRenderer _lineRenderer;
    private Transform _cueTipTransform;
    private Mesh _cueTipMesh;
    private float _scaledBallRadius;
    private float _scaledMaxHorizontalDistanceToCueBall;
    private float _scaledMaxVerticalDistanceToCueBall;
    private float _scaledMaxCueBallRollDistance;

    private struct FindCueBallResult
    {
        public Vector3 CueDirection;
        public Rigidbody CueBallRigidbody;
    }

#if DEBUG_TRAINING_WHEELS
    private struct DebugPoint
    {
        public Vector3 Position;
        public Color? Color;
        public bool IsOnPredictedPath;
    }

    private readonly List<DebugPoint> _debugPoints = new();
    private ValueTuple<Vector3, float, Color>? _debugOverlapSphere;

    private Mesh _cylinderMesh;
#endif


    private void OnEnable()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _cueTipTransform = cueTip.transform;
        _cueTipMesh = cueTip.mesh;

#if DEBUG_TRAINING_WHEELS
        _cylinderMesh = UnityEngine.Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
#endif
    }

    private void FixedUpdate()
    {
        _scaledBallRadius = TransformScalar(ballRadius);
        _scaledMaxHorizontalDistanceToCueBall = TransformScalar(maxHorizontalDistanceToCueBall);
        _scaledMaxVerticalDistanceToCueBall = TransformScalar(maxVerticalDistanceToCueBall);
        _scaledMaxCueBallRollDistance = TransformScalar(maxCueBallRollDistance);

        // Reset line renderer
        _lineRenderer.positionCount = 0;
        _lineRenderer.startWidth = _scaledBallRadius * 2;

        if (FindCueBall() is not { } findCueBallResult) return;

        var predictedPath = GetPredictedBallPath(
            _scaledMaxCueBallRollDistance,
            findCueBallResult.CueDirection,
            findCueBallResult.CueBallRigidbody
        );

        // Update line renderer
        _lineRenderer.positionCount = predictedPath.Length;
        _lineRenderer.SetPositions(predictedPath);
    }

    private FindCueBallResult? FindCueBall()
    {
        var cueTipPosition = _cueTipTransform.TransformPoint(
            _cueTipMesh.bounds.center + _cueTipMesh.bounds.extents.z * Vector3.forward
        );

        // Collider bounds are axis-aligned and in world coords, so do not need to be transformed
        var tableSurfaceBounds = tableSurface.bounds;
        var heightCorrectedCueTipPosition = new Vector3(
            cueTipPosition.x,
            tableSurfaceBounds.center.y + tableSurfaceBounds.extents.y + _scaledBallRadius,
            cueTipPosition.z
        );

        // Only show training wheels if cue tip is close enough to cue ball
        if (Mathf.Abs(_cueTipTransform.position.y - heightCorrectedCueTipPosition.y)
            > _scaledMaxVerticalDistanceToCueBall)
        {
            return null;
        }

        var cueDirection =
            _cueTipTransform.forward.AlignedToXZPlane().normalized * _scaledMaxHorizontalDistanceToCueBall;

#if DEBUG_TRAINING_WHEELS
        _debugPoints.Clear();
        _debugOverlapSphere = null;
        Debug.DrawRay(heightCorrectedCueTipPosition, cueDirection, Color.green);
#endif

        if ((
                FindOverlappingCueBall(heightCorrectedCueTipPosition)
                ?? FindPointedAtCueBall(heightCorrectedCueTipPosition, cueDirection)
            ) is not { } cueBallRigidbody)
        {
            return null;
        }

        return new FindCueBallResult
        {
            CueDirection = cueDirection,
            CueBallRigidbody = cueBallRigidbody
        };
    }

    /**
     * Check if cue tip (height-corrected) is inside a cue ball
     */
    [CanBeNull]
    private Rigidbody FindOverlappingCueBall(Vector3 heightCorrectedCueTipPosition)
    {
        var collidingObjects = new Collider[1];
        var numOverlapping = Physics.OverlapSphereNonAlloc(
            heightCorrectedCueTipPosition,
            TransformScalar(0.1f),
            collidingObjects,
            cueBallLayerMask
        );

#if DEBUG_TRAINING_WHEELS
        _debugOverlapSphere = ValueTuple.Create(
            heightCorrectedCueTipPosition,
            TransformScalar(0.1f),
            numOverlapping
            > 0
                ? Color.green
                : Color.red
        );
#endif

        return numOverlapping > 0 ? collidingObjects.First().attachedRigidbody : null;
    }

    /**
     * Shoot a ray from the height-corrected cue tip position to see if we're pointing at a cue ball
     */
    [CanBeNull]
    private Rigidbody FindPointedAtCueBall(Vector3 heightCorrectedCueTipPosition, Vector3 cueDirection)
    {
        var didRaycastFindObjects = Physics.Raycast(
            heightCorrectedCueTipPosition,
            cueDirection,
            out var cueRayHit,
            _scaledMaxHorizontalDistanceToCueBall,
            cueRayLayerMask
        );

        if (!didRaycastFindObjects) return null;

#if DEBUG_TRAINING_WHEELS
        _debugPoints.Add(
            new DebugPoint
            {
                Position = cueRayHit.point,
                Color = cueRayHit.transform.gameObject.CompareTag("CueBall")
                    ? Color.green
                    : Color.red
            }
        );
#endif

        return cueRayHit.transform.gameObject.CompareTag("CueBall") ? cueRayHit.rigidbody : null;
    }

    /**
     * Get the points (inclusive) of the predicted path taken by the ball.
     * All vectors, in and out, are in world space.
     */
    private Vector3[] GetPredictedBallPath(
        float maxDistance,
        Vector3 initialDirection,
        Rigidbody ballRigidbody
    )
    {
        var remainingDistance = maxDistance;
        var nextDirection = initialDirection.normalized;
        var nextPosition = ballRigidbody.position;
        var points = new List<Vector3> { nextPosition };

#if DEBUG_TRAINING_WHEELS
        _debugPoints.Add(new DebugPoint { Position = nextPosition, IsOnPredictedPath = true });
#endif

        while (remainingDistance > 0f)
        {
            var didHit = Physics.SphereCast(
                nextPosition,
                _scaledBallRadius,
                nextDirection,
                out var hit,
                remainingDistance,
                cueBallRayLayerMask
            );

            if (didHit)
            {
                var newCenter = nextPosition + nextDirection * hit.distance;

#if DEBUG_TRAINING_WHEELS
                Debug.DrawRay(nextPosition, nextDirection * hit.distance, Color.yellow);
                _debugPoints.Add(
                    new DebugPoint { Position = newCenter, Color = Color.blue, IsOnPredictedPath = true }
                );
                _debugPoints.Add(new DebugPoint { Position = hit.point, Color = Color.red });
#endif

                points.Add(newCenter);
                remainingDistance -= hit.distance;
                nextDirection = Vector3.Reflect(nextDirection, hit.normal).AlignedToXZPlane().normalized;
                nextPosition = newCenter;
            }
            else
            {
#if DEBUG_TRAINING_WHEELS
                Debug.DrawRay(nextPosition, nextDirection * remainingDistance, Color.yellow);
                _debugPoints.Add(
                    new DebugPoint
                    {
                        Position = nextPosition + nextDirection * remainingDistance,
                        Color = Color.red,
                        IsOnPredictedPath = true
                    }
                );
#endif

                points.Add(nextPosition + nextDirection * remainingDistance);
                break;
            }
        }

        return points.ToArray();
    }

    private float TransformScalar(float scalar)
    {
        return transform.TransformVector(Vector3.forward * scalar).magnitude;
    }

#if UNITY_EDITOR && DEBUG_TRAINING_WHEELS
    private void OnDrawGizmos()
    {
        foreach (var debugPoint in _debugPoints)
        {
            Gizmos.color = debugPoint.Color ?? Color.yellow;
            Gizmos.DrawCube(debugPoint.Position, Vector3.one * 0.005f);
        }

        var pathPoints = _debugPoints.Where(p => p.IsOnPredictedPath).Select(p => p.Position).ToArray();

        foreach (var (startPoint, endPoint) in pathPoints
                     .SkipLast(1)
                     .Zip(pathPoints.Skip(1), Tuple.Create)
                )
        {
            Gizmos.color = Color.gray;

            Gizmos.DrawWireMesh(
                _cylinderMesh,
                startPoint + (endPoint - startPoint) / 2,
                Quaternion.FromToRotation(Vector3.up, endPoint - startPoint),
                new Vector3(_scaledBallRadius, (endPoint - startPoint).magnitude / 2, _scaledBallRadius)
            );
            Gizmos.DrawWireSphere(endPoint, _scaledBallRadius);
        }

        if (_debugOverlapSphere is { } sphere)
        {
            Gizmos.color = sphere.Item3;
            Gizmos.DrawWireSphere(sphere.Item1, sphere.Item2);
        }
    }
#endif
}
