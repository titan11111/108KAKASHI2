using UnityEngine;

namespace Kakashi
{
    public class KakashiAudio : MonoBehaviour
    {
        static KakashiAudio instance;
        AudioSource effects, wind;
        AudioClip click, impact, bird, rumble;
        bool muted;

        void Awake()
        {
            instance = this;
            effects = gameObject.AddComponent<AudioSource>();
            wind = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = wind.playOnAwake = false;
            effects.spatialBlend = wind.spatialBlend = 0;
            click = Tone("Click", 660, .065f, false);
            impact = Tone("Impact", 90, .32f, true);
            bird = Tone("Crow", 380, .24f, true);
            rumble = Tone("Wind", 45, 2, true);
            wind.clip = rumble; wind.loop = true; wind.volume = 0;
        }

        static AudioClip Tone(string name, float frequency, float duration, bool noise)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random(17);
            float smooth = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
                smooth = Mathf.Lerp(smooth, (float)random.NextDouble() * 2 - 1, .12f);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t) * .2f + (noise ? smooth * .7f : 0)) * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }

        public static void Cue(string cue)
        {
            if (instance == null || instance.muted) return;
            var clip = cue == "crow" ? instance.bird : cue == "impact" ? instance.impact : instance.click;
            instance.effects.PlayOneShot(clip, .45f);
        }

        public static bool Toggle()
        {
            if (instance == null) return false;
            instance.muted = !instance.muted;
            instance.effects.mute = instance.muted; instance.wind.mute = instance.muted;
            return !instance.muted;
        }

        void Update()
        {
            wind.volume = Mathf.Clamp01(Wind.Strength / 3) * .35f;
            if (Wind.Strength > .01f && !wind.isPlaying) wind.Play();
            if (Wind.Strength <= .01f && wind.isPlaying) wind.Stop();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            Destroy(click); Destroy(impact); Destroy(bird); Destroy(rumble);
        }
    }
}
