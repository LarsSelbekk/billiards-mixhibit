using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSlowdownTailShortener : MonoBehaviour
{
    public float comeToRestAngularSpeedThreshold = 0.01f;
    public float slowdownAngularSpeedThreshold = 8f;
    public float slowdownPerSecond = 1.75f;
    private Rigidbody _rigidbody;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_rigidbody.angularVelocity.magnitude < comeToRestAngularSpeedThreshold)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            return;
        }
        if (_rigidbody.angularVelocity.magnitude < slowdownAngularSpeedThreshold)
        {
            _rigidbody.AddTorque(-_rigidbody.angularVelocity * (slowdownPerSecond * Time.fixedDeltaTime), ForceMode
                .VelocityChange);
        }
    }
}
