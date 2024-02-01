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
        // TODO: handle scaling
        if (_rigidbody.angularVelocity.magnitude > minAngularVelocity && _rigidbody.velocity.magnitude > minVelocity)
        {
            _rigidbody.AddForce((Vector3.Cross(_rigidbody.velocity, _rigidbody.angularVelocity) * scaleFactor).AlignedToXZPlane());
        }
    }
}
