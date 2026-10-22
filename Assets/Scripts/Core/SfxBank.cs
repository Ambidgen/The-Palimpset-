using System.Collections.Generic;
using UnityEngine;

namespace Palimpsest
{
    /// <summary>Every cue the game can make. All of them are synthesised at runtime.</summary>
    public enum Sfx
    {
        Start,
        Ink,
        Ward,
        Scrape,
        Illuminate,
        Seal,
        Flame,
        Layer,
        Over,
        Denied,
        Surge
    }

    /// <summary>
    /// A tiny procedural sound bank.
    ///
    /// Audio files are binary assets this repository cannot ship, so every cue is
    /// generated as a short sine/noise burst the first time the world is built and
    /// then played through a single AudioSource. The drone is a slow, detuned
    /// loop that rises in pitch as the palimpsest deepens.
    /// </summary>
    public class SfxBank : MonoBehaviour
    {
        const int SampleRate = 44100;

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        AudioSource _oneShot;
        AudioSource _drone;
        float _masterVolume = 0.9f;

        public static SfxBank Build(Transform parent)
        {
            GameObject go = new GameObject("Sfx Bank");
            go.transform.SetParent(parent, false);
            SfxBank bank = go.AddComponent<SfxBank>();
            bank.Initialise();
            return bank;
        }

        void Initialise()
        {
            _oneShot = gameObject.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            _oneShot.spatialBlend = 0f;
            _oneShot.volume = 1f;

            _drone = gameObject.AddComponent<AudioSource>();
            _drone.playOnAwake = false;
            _drone.spatialBlend = 0f;
            _drone.loop = true;

            _clips[Sfx.Start] = Tone("pal_start", 0.60f, 196f, 392f, 0.10f, 1.7f, 0.32f);
            _clips[Sfx.Ink] = Tone("pal_ink", 0.11f, 900f, 720f, 0.30f, 2.6f, 0.16f);
            _clips[Sfx.Ward] = Tone("pal_ward", 0.42f, 520f, 1040f, 0.05f, 2.1f, 0.28f);
            _clips[Sfx.Scrape] = Tone("pal_scrape", 0.38f, 320f, 80f, 0.90f, 1.4f, 0.32f);
            _clips[Sfx.Illuminate] = Tone("pal_illuminate", 0.55f, 180f, 760f, 0.12f, 1.9f, 0.30f);
            _clips[Sfx.Seal] = Tone("pal_seal", 0.30f, 420f, 120f, 0.28f, 2.2f, 0.30f);
            _clips[Sfx.Flame] = Tone("pal_flame", 0.20f, 170f, 70f, 0.55f, 1.5f, 0.26f);
            _clips[Sfx.Layer] = Tone("pal_layer", 0.95f, 140f, 330f, 0.08f, 1.3f, 0.32f);
            _clips[Sfx.Over] = Tone("pal_over", 1.20f, 240f, 55f, 0.18f, 1.1f, 0.36f);
            _clips[Sfx.Denied] = Tone("pal_denied", 0.16f, 190f, 150f, 0.08f, 3.0f, 0.20f);
            _clips[Sfx.Surge] = Tone("pal_surge", 0.75f, 92f, 50f, 0.35f, 1.6f, 0.30f);

            _drone.clip = DroneClip("pal_drone", 8f);
            _drone.volume = 0.09f;
            _drone.Play();
        }

        /// <summary>Plays a one-shot cue. Pitch jitter keeps repeated sounds from feeling mechanical.</summary>
        public void Play(Sfx cue, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip;
            if (_oneShot == null) return;
            if (!_clips.TryGetValue(cue, out clip) || clip == null) return;

            _oneShot.pitch = Mathf.Clamp(pitch, 0.55f, 1.9f);
            _oneShot.PlayOneShot(clip, Mathf.Clamp01(volume) * _masterVolume);
        }

        /// <summary>0 = calm first folio, 1 = a deeply overwritten page.</summary>
        public void SetTension(float tension)
        {
            if (_drone == null) return;
            float t = Mathf.Clamp01(tension);
            _drone.volume = 0.07f + 0.06f * t;
            _drone.pitch = 0.92f + 0.16f * t;
        }

        public void Silence()
        {
            if (_drone != null) _drone.Stop();
        }

        /// <summary>Brings the drone back after Silence() (used when a folio restarts).</summary>
        public void Resume()
        {
            if (_drone != null && _drone.clip != null && !_drone.isPlaying) _drone.Play();
        }

        // ------------------------------------------------------------------
        // Synthesis
        // ------------------------------------------------------------------

        static AudioClip Tone(string name, float duration, float startHz, float endHz,
            float noiseMix, float decay, float gain)
        {
            int count = Mathf.Max(64, Mathf.RoundToInt(duration * SampleRate));
            float[] samples = new float[count];

            float phase = 0f;
            float overtone = 0f;
            uint seed = 0x9E3779B9u ^ (uint)name.GetHashCode();

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(startHz, endHz, t);

                phase += hz * Mathf.PI * 2f / SampleRate;
                overtone += hz * 1.503f * Mathf.PI * 2f / SampleRate;

                float attack = Mathf.Min(1f, t * 60f);
                float envelope = Mathf.Pow(1f - t, decay) * attack;
                float noise = NextNoise(ref seed);

                float sample = Mathf.Sin(phase) * 0.72f
                             + Mathf.Sin(overtone) * 0.18f
                             + noise * 0.40f * noiseMix;

                samples[i] = Mathf.Clamp(sample * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip DroneClip(string name, float duration)
        {
            int count = Mathf.Max(1024, Mathf.RoundToInt(duration * SampleRate));
            float[] samples = new float[count];

            float low = 0f;
            float high = 0f;
            uint seed = 0x1234567u;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                // Slow beating between three low partials keeps the drone unsettled.
                low += (55f * Mathf.PI * 2f / SampleRate);
                high += (55.75f * Mathf.PI * 2f / SampleRate);
                float third = Mathf.Sin(2f * Mathf.PI * 82.5f * (i / (float)SampleRate));

                float swell = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f);
                float grain = NextNoise(ref seed) * 0.05f;

                samples[i] = Mathf.Clamp(
                    (Mathf.Sin(low) * 0.35f + Mathf.Sin(high) * 0.25f + third * 0.10f + grain) * swell * 0.55f,
                    -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float NextNoise(ref uint seed)
        {
            seed = seed * 1664525u + 1013904223u;
            return ((seed >> 8) & 0xFFFF) / 32767.5f - 1f;
        }
    }
}
