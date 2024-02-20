using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainingWheelsLineRenderer : MonoBehaviour
{
    public GameObject lineSegmentPrefab;
    public float width = 0.05f;
    private static readonly int GradientStart = Shader.PropertyToID("_Gradient_Start");
    private static readonly int GradientEnd = Shader.PropertyToID("_Gradient_End");

    public IList<Vector3> Positions
    {
        get => _positions;
        set
        {
            _positions = value;
            UpdatePositions();
        }
    }

    private readonly List<GameObject> _capsules = new();
    private IList<Vector3> _positions = new List<Vector3>();

    private float _maxLength = 1f;

    public float MaxLength
    {
        get => _maxLength;
        set
        {
            _maxLength = value;
            UpdatePositions();
        }
    }

    private void OnDisable()
    {
        _capsules.ForEach(Destroy);
        _capsules.Clear();
    }

    private void UpdatePositions()
    {
        var activeCapsuleCount = _positions.Count - 1;
        while (_capsules.Count < activeCapsuleCount)
        {
            var capsule = Instantiate(lineSegmentPrefab, transform);
            _capsules.Add(capsule);
        }

        foreach (var redundantCapsule in _capsules.Skip(activeCapsuleCount))
        {
            redundantCapsule.SetActive(false);
        }

        var lengthSoFar = 0f;
        foreach (var (capsule, index) in _capsules
                     .Take(activeCapsuleCount)
                     .Select(
                         (capsule, index) => ValueTuple
                             .Create(capsule, index)
                     ))
        {
            var startPoint = _positions[index];
            var endPoint = _positions[index + 1];
            var capsuleTransform = capsule.transform;
            var centerPoint = (startPoint + endPoint) / 2;

            capsuleTransform.position = centerPoint;
            capsuleTransform.rotation =
                Quaternion.FromToRotation(Vector3.up, endPoint - startPoint)
                // Rotate texture seam downward
                * ((endPoint - startPoint).x < 0
                    ? Quaternion.AngleAxis(180, Vector3.up)
                    : Quaternion.identity);
            capsuleTransform.localScale =
                new Vector3(
                    width,
                    (endPoint - startPoint).magnitude / 2,
                    width
                )
                * InverseTransformScalar(1f);

            var material = capsule.GetComponent<MeshRenderer>().material;
            material.SetFloat(GradientStart, lengthSoFar / MaxLength);
            lengthSoFar += InverseTransformScalar((endPoint - startPoint).magnitude);
            material.SetFloat(GradientEnd, lengthSoFar / MaxLength);
            capsule.SetActive(true);
        }
    }

    private float InverseTransformScalar(float scalar)
    {
        return transform.InverseTransformVector(Vector3.forward * scalar).magnitude;
    }
}
