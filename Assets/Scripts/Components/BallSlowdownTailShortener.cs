using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallSlowdownTailShortener : MonoBehaviour
{
    public float slowdownAngularSpeedThreshold = 1f;
    public float slowdownPerSecond = 1f;
    private Rigidbody _rigidbody;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_rigidbody.angularVelocity.magnitude < slowdownAngularSpeedThreshold)
        {
            _rigidbody.AddTorque(-_rigidbody.angularVelocity * (slowdownPerSecond * Time.fixedDeltaTime), ForceMode
                .VelocityChange);
        }
    }
}
