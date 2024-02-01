// #define DEBUG_TRAINING_WHEELS

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

[RequireComponent(typeof(LineRenderer))]
public class PathPredictorTrainingWheels : MonoBehaviour
{
    public MeshFilter cueTip;
    public LayerMask cueRayLayerMask;
    public MeshCollider tableSurface;
    public float maxHorizontalDistanceToCueBall = 1.0f;
    public float maxVerticalDistanceToCueBall = 0.5f;
    public float maxCueBallRollDistance = 1f;
    public float ballRadius = 0.05715f / 2;

    private const int RaycastHitsBufferSize = 100;

    private LineRenderer _lineRenderer;
    private Transform _cueTipTransform;
    private Mesh _cueTipMesh;
    private Bounds _tableSurfaceBounds;

#if DEBUG_TRAINING_WHEELS
    private struct DebugPoint
    {
        public Vector3 Position;
        public Color? Color;
    }

    private readonly List<DebugPoint> _debugPoints = new();
#endif

    private void OnEnable()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _cueTipTransform = cueTip.transform;
        _cueTipMesh = cueTip.mesh;
        _tableSurfaceBounds = tableSurface.bounds;
    }

    private void FixedUpdate()
    {
        var scaledBallRadius = TransformScalar(ballRadius);
        var scaledMaxHorizontalDistanceToCueBall = TransformScalar(maxHorizontalDistanceToCueBall);
        var scaledMaxVerticalDistanceToCueBall = Mathf.Pow(TransformScalar(maxVerticalDistanceToCueBall), 2);
        var scaledMaxCueBallRollDistance = TransformScalar(maxCueBallRollDistance);

        // Reset line renderer
        _lineRenderer.positionCount = 0;
        _lineRenderer.startWidth = scaledBallRadius * 2;

        var cueTipPosition = _cueTipTransform.TransformPoint(
            _cueTipMesh.bounds.center + _cueTipMesh.bounds.extents.z * Vector3.forward
        );

        // Collider bounds are axis-aligned and in world coords, so do not need to be transformed
        var heightCorrectedCueTipPosition = new Vector3(
            cueTipPosition.x,
            _tableSurfaceBounds.center.y + _tableSurfaceBounds.extents.y + scaledBallRadius,
            cueTipPosition.z
        );
        var cueDirection =
            _cueTipTransform.forward.AlignedToXZPlane().normalized * scaledMaxHorizontalDistanceToCueBall;

#if DEBUG_TRAINING_WHEELS
        _debugPoints.Clear();
        Debug.DrawRay(heightCorrectedCueTipPosition, cueDirection, Color.green);
#endif

        // Shoot a ray from the height-corrected cue tip position to see if we're pointing at a cue ball
        var didRaycastFindObjects = Physics.Raycast(
            heightCorrectedCueTipPosition,
            cueDirection,
            out var cueRayHit,
            scaledMaxHorizontalDistanceToCueBall,
            cueRayLayerMask
        );
        if (!didRaycastFindObjects) return;

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

        if (!cueRayHit.transform.gameObject.CompareTag("CueBall")) return;
        var cueBallRigidbody = cueRayHit.rigidbody;

        // Only show training wheels if cue tip is close enough to cue ball
        if (Mathf.Abs(_cueTipTransform.position.y - heightCorrectedCueTipPosition.y)
            > scaledMaxVerticalDistanceToCueBall)
        {
            return;
        }

        var predictedPath = GetPredictedBallPath(
            scaledMaxCueBallRollDistance,
            cueDirection,
            cueBallRigidbody
        );

        // Update line renderer
        _lineRenderer.positionCount = predictedPath.Length;
        _lineRenderer.SetPositions(predictedPath);
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
        var hits = new RaycastHit[RaycastHitsBufferSize];

        // TODO: account for radius of ball - send multiple rays
        while (remainingDistance > 0f)
        {
            var numHits =
                Physics.RaycastNonAlloc(nextPosition, nextDirection, hits, remainingDistance, cueRayLayerMask);
            var potentialHit = hits[..numHits]
                .OrderBy(raycastHit => raycastHit.distance)
                .Where(raycastHit => raycastHit.rigidbody != ballRigidbody)
                .Cast<RaycastHit?>()
                .FirstOrDefault();

            if (potentialHit is { } hit)
            {
#if DEBUG_TRAINING_WHEELS
                Debug.DrawRay(nextPosition, nextDirection.normalized * hit.distance, Color.yellow);
                Debug.DrawRay(nextPosition, nextDirection.normalized * hit.distance, Color.yellow);
                _debugPoints.Add(new DebugPoint { Position = hit.point, Color = Color.red });
#endif

                points.Add(hit.point);
                remainingDistance -= hit.distance;
                nextDirection = Vector3.Reflect(nextDirection, hit.normal).AlignedToXZPlane().normalized;
                nextPosition = hit.point;
            }
            else
            {
#if DEBUG_TRAINING_WHEELS
                Debug.DrawRay(nextPosition, nextDirection.normalized * remainingDistance, Color.yellow);
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
    }
#endif
}
