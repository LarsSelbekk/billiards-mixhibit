using System;
using UnityEngine;
using Utils;

[RequireComponent(typeof(Rigidbody))]
public class BallRotationFrictionifier : MonoBehaviour
{
    public float scaleFactor = 0.005f;
    public float minAngularVelocity = 0.01f;
    public float minVelocity = 0.01f;

    private Rigidbody _rigidbody;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
        var scaledMinVelocity = TransformScalar(minVelocity);
        if (_rigidbody.angularVelocity.magnitude > minAngularVelocity
            && _rigidbody.velocity.magnitude > scaledMinVelocity)
        {
            _rigidbody.AddForce(
                (Vector3.Cross(
                     _rigidbody.angularVelocity,
                     transform.TransformVector(_rigidbody.velocity)
                 ) * scaleFactor).AlignedToXZPlane()
            );
        }
    }

    private float TransformScalar(float scalar)
    {
        return transform.TransformVector(Vector3.forward * scalar).magnitude;
    }
}
