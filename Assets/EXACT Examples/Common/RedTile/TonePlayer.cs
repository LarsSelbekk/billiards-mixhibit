using UnityEngine;
using NaughtyAttributes;

using System.Collections;

namespace Exact.Example
{
    [RequireComponent(typeof(Device))]
    public class TonePlayer : DeviceComponent
    {
        public override string GetComponentType() { return "tone_player"; }

        [SerializeField, OnValueChanged("OnVolumeChanged"), Range(0, 1)]
        float volume = 1;
        AudioSource audioSource;
        int frequency;

        protected override void Awake()
        {
            base.Awake();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0; // Force 2D sound
            audioSource.Stop(); // Avoids the audiosource starting to play automatically
            audioSource.volume = volume;
        }

        public void OnConnect()
        {
            SetVolume(volume, true);
        }

        // DS 04.01.2023. Return a clip with sine audio
        private AudioClip CreateToneAudioClip(float frequency, float sampleDurationSecs)  // ChatGPT
        {
            int sampleRate = 44100; // Standard sample rate
            int sampleLength = (int)(sampleRate * sampleDurationSecs);
            float[] samples = new float[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                float t = i / (float)sampleRate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t);
            }

            AudioClip clip = AudioClip.Create("SineWaveTone", sampleLength, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        ///<summary>
        /// Plays a tone with the given frequency.
        /// Sends the frequency and duration to the physical tone player, making it play a tone at that frequency for the given duration.
        ///</summary>
        ///<param name="frequency">Frequency of the tone to play in Hz.</param>
        ///<param name="duration">Duration of the tone to play in seconds.</param>
        public void PlayTone(int frequency, float duration)
        {
            this.frequency = frequency;
            string payload = frequency.ToString() + "/" + Mathf.RoundToInt(duration * 1000).ToString();
            SendAction("tone", payload);
            AudioClip clip = CreateToneAudioClip(frequency, 1f); // DS. One second clip.
            audioSource.clip = clip;
            audioSource.loop = true;  // Loop the clip forever.
            audioSource.Play();
            StartCoroutine(StopAudioAfterDuration(duration));
        }

        ///<summary>
        /// Sends a stop signal to the physical tone player, making it stop playing whatever tone is currently playing.
        ///</summary>
        public void StopTone()
        {
            SendAction("no_tone", 0);
            audioSource.Stop();
            audioSource.loop = false;
        }

        /// <summary>
        /// Sets the volume of the tone player
        /// </summary>
        /// <param name="volume">The volume as a value from 0 to 1</param>
        public void SetVolume(float volume, bool forceUpdate = false)
        {
            if (this.volume != volume || forceUpdate)
            {
                this.volume = volume;
                audioSource.volume = volume;
                SendAction("set_volume", Mathf.RoundToInt(volume * 100));
            }
        }

        private IEnumerator StopAudioAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);
            audioSource.Stop();
        }

        //
        // Value changed callbacks
        //

        private void OnVolumeChanged()
        {
            SetVolume(volume, true);
        }

        //
        // Audio player and sine wave generator 
        //

        /// <summary>
        /// If OnAudioFilterRead is implemented, Unity will insert a custom filter into the audio DSP chain.
        /// OnAudioFilterRead is called every time a chunk of audio is sent to the filter.
        /// </summary>

    }
}
