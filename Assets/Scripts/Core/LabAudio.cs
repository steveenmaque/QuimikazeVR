using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    public enum LabSound { Hover, Click, Whoosh, Boom, Fizz, Chime }

    /// <summary>Sonidos generados por codigo (no necesita archivos de audio).</summary>
    public class LabAudio : MonoBehaviour
    {
        static LabAudio instance;
        AudioSource source;
        readonly Dictionary<LabSound, AudioClip> clips = new Dictionary<LabSound, AudioClip>();

        public static void Play(LabSound sound, float volume = 1f)
        {
            if (!Application.isPlaying) return;
            if (instance == null)
            {
                var go = new GameObject("LabAudio");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<LabAudio>();
            }
            instance.PlayInternal(sound, volume);
        }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void PlayInternal(LabSound sound, float volume)
        {
            if (!clips.TryGetValue(sound, out var clip))
            {
                clip = Create(sound);
                clips[sound] = clip;
            }
            source.PlayOneShot(clip, volume);
        }

        const int Rate = 22050;

        static AudioClip Create(LabSound sound)
        {
            switch (sound)
            {
                case LabSound.Hover: return Tone("hover", 0.05f, t => 1400f, 0.12f);
                case LabSound.Click: return Tone("click", 0.12f, t => t < 0.05f ? 880f : 1320f, 0.25f);
                case LabSound.Chime: return Tone("chime", 0.6f, t => t < 0.15f ? 660f : t < 0.3f ? 880f : 1320f, 0.25f);
                case LabSound.Whoosh: return Noise("whoosh", 0.5f, t => Mathf.Sin(t / 0.5f * Mathf.PI) * 0.25f, 0.85f);
                case LabSound.Boom: return Noise("boom", 1.2f, t => Mathf.Exp(-t * 4f) * 0.9f, 0.97f);
                case LabSound.Fizz: return Noise("fizz", 1.0f, t => (0.5f + 0.5f * Mathf.Sin(t * 60f)) * Mathf.Exp(-t * 2f) * 0.3f, 0.2f);
                default: return Tone("x", 0.1f, t => 440f, 0.2f);
            }
        }

        static AudioClip Tone(string name, float length, System.Func<float, float> freq, float vol)
        {
            int n = Mathf.CeilToInt(length * Rate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                phase += freq(t) * 2f * Mathf.PI / Rate;
                float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 6f / length);
                data[i] = Mathf.Sin(phase) * env * vol;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Noise(string name, float length, System.Func<float, float> env, float smoothing)
        {
            int n = Mathf.CeilToInt(length * Rate);
            var data = new float[n];
            var rng = new System.Random(7);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2 - 1);
                last = Mathf.Lerp(white, last, smoothing);
                data[i] = last * env(t) * (1f / (1f - smoothing * 0.9f)) * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
