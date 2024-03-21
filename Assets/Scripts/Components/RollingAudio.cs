// #define DEBUG_ROLLING_AUDIO

using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RollingAudio : MonoBehaviour
{
    public AudioSource rollingAudioSource;
    public AudioClip woodAudioClip;
    public AnimationCurve audioScale, pitchScale;
    public float maxVolumeVelocity, maxPitchVelocity;

    private readonly ContactPoint[] _contactPointsBuffer = new ContactPoint[4];

    private void OnCollisionEnter(Collision collision)
    {
        var clip = GetAudioClip(collision.gameObject.tag);
        if (clip == null)
        {
#if DEBUG_ROLLING_AUDIO
            Debug.Log($"Rolling on unknown tag {collision.gameObject.tag}", this);
#endif
            return;
        }

        rollingAudioSource.clip = clip;
        UpdateAudioProperties(collision);
        rollingAudioSource.Play();

#if DEBUG_ROLLING_AUDIO
        Debug.Log($"Playing rolling sound for tag {collision.gameObject.tag}", this);
#endif
    }

    private void OnCollisionExit(Collision collision)
    {
        if (GetAudioClip(collision.gameObject.tag) == null) return;
        rollingAudioSource.Stop();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (GetAudioClip(collision.gameObject.tag) == null) return;

        UpdateAudioProperties(collision);
    }

    private void UpdateAudioProperties(Collision collision)
    {
        var scaledMaxVolumeVelocity = transform.InverseTransformVector(Vector3.forward * maxVolumeVelocity).magnitude;
        var scaledMaxPitchVelocity = transform.InverseTransformVector(Vector3.forward * maxPitchVelocity).magnitude;

        var averageNormal = _contactPointsBuffer
            .Take(collision.GetContacts(_contactPointsBuffer))
            .Aggregate(Vector3.zero, (sum, contact) => contact.normal + sum)
            .normalized;
        var parallelSpeed = Vector3.Cross(averageNormal, collision.relativeVelocity).magnitude;

        rollingAudioSource.pitch =
            pitchScale.Evaluate(Mathf.Clamp01(parallelSpeed / scaledMaxPitchVelocity));
        rollingAudioSource.volume =
            audioScale.Evaluate(Mathf.Clamp01(parallelSpeed / scaledMaxVolumeVelocity));
    }

    private AudioClip GetAudioClip(string otherTag)
    {
        return otherTag switch
        {
            "Table" => woodAudioClip,
            _ => null,
        };
    }
}
