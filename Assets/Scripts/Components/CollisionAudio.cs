// #define DEBUG_COLLISION_AUDIO

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(AudioSource))]
public class CollisionAudio : MonoBehaviour
{
    public AudioSource collisionAudioSource;
    public List<AudioClip> ballBallHitAudioClips, cueStickBallHitAudioClips, railBallHitAudioClips;
    public AnimationCurve audioScale;
    public float maxVolumeVelocity;

    private void OnCollisionEnter(Collision collision)
    {
        var clips = collision.gameObject.tag switch
        {
            "CueStick" => cueStickBallHitAudioClips,
            "Ball" or "CueBall" => ballBallHitAudioClips,
            "RailBallCollider" or "TableSlate" or "Table" => railBallHitAudioClips,
            _ => null,
        };

        if (clips == null || !clips.Any())
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
        collisionAudioSource.PlayOneShot(
            clips[new Random().Next(clips.Count)],
            Mathf.Clamp01(
                audioScale.Evaluate(collision.relativeVelocity.magnitude / scaledMaxVolumeVelocity)
            )
        );
    }
}
