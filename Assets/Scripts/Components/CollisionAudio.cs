// #define DEBUG_COLLISION_AUDIO

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionAudio : MonoBehaviour
{
    public AudioClip ballBallHitAudioClip, cueStickBallHitAudioClip, railBallHitAudioClip;
    public AnimationCurve audioScale;
    public float maxVolumeVelocity;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        var clip = collision.gameObject.tag switch
        {
            "CueStick" => cueStickBallHitAudioClip,
            "Ball" or "CueBall" => ballBallHitAudioClip,
            "RailBallCollider" or "TableSlate" => railBallHitAudioClip,
            _ => null,
        };

        if (clip == null)
        {
#if DEBUG_COLLISION_AUDIO
            Debug.Log($"Hit unknown tag {collision.gameObject.tag}", this);
#endif
            return;
        }

#if DEBUG_COLLISION_AUDIO
        Debug.Log($"Playing sound for tag {collision.gameObject.tag}", this);
#endif

        var scaledMaxVolumeVelocity = transform.InverseTransformVector(Vector3.forward * maxVolumeVelocity).magnitude;
        _audioSource.PlayOneShot(
            clip,
            Mathf.Clamp01(
                audioScale.Evaluate(collision.relativeVelocity.magnitude / scaledMaxVolumeVelocity)
            )
        );
    }
}
