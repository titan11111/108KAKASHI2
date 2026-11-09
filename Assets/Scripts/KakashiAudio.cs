using UnityEngine;

namespace Kakashi
{
    public class KakashiAudio : MonoBehaviour
    {
        static KakashiAudio instance;
        AudioSource effects, wind, rain;
        AudioClip click, impact, bird, rumble, thunder, monkey, hiss;
        bool muted;

        void Awake()
        {
            instance = this;
            effects = gameObject.AddComponent<AudioSource>();
            wind = gameObject.AddComponent<AudioSource>();
            rain = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = wind.playOnAwake = rain.playOnAwake = false;
            effects.spatialBlend = wind.spatialBlend = rain.spatialBlend = 0;
            click = Tone("Click", 660, .065f, false);
            impact = Tone("Impact", 90, .32f, true);
            bird = Tone("Crow", 380, .24f, true);
            rumble = Tone("Wind", 45, 2, true);
            wind.clip = rumble; wind.loop = true; wind.volume = 0;
            thunder = Rumble("Thunder", 3.2f);
            monkey = Tone("Monkey", 920, .2f, false);
            hiss = Hiss("Rain", 2f);
            rain.clip = hiss; rain.loop = true; rain.volume = 0;
        }

        /// <summary>雷鳴：最初の「バリッ」と、長く尾を引く低い「ゴロゴロ…」</summary>
        static AudioClip Rumble(string name, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random(23);
            float lp = 0, lp2 = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float n = (float)random.NextDouble() * 2 - 1;
                lp = Mathf.Lerp(lp, n, .035f);
                lp2 = Mathf.Lerp(lp2, n, .5f);
                float crack = lp2 * Mathf.Exp(-t * 28f) * .9f;
                float roll = lp * 5f * Mathf.Exp(-t * 0.9f) * (0.75f + 0.25f * Mathf.Sin(t * 9f));
                samples[i] = Mathf.Clamp((crack + roll) * Mathf.Clamp01(t * 40f), -1f, 1f) * .8f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }

        /// <summary>雨音：やわらかいホワイトノイズ</summary>
        static AudioClip Hiss(string name, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random(5);
            float lp = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                lp = Mathf.Lerp(lp, (float)random.NextDouble() * 2 - 1, .55f);
                samples[i] = lp * .5f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
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
            AudioClip clip;
            switch (cue)
            {
                case "crow": clip = instance.bird; break;
                case "impact": clip = instance.impact; break;
                case "thunder": case "thunder_far": clip = instance.thunder; break;
                case "monkey": clip = instance.monkey; break;
                default: clip = instance.click; break;
            }
            instance.effects.PlayOneShot(clip, cue == "thunder" ? .85f : cue == "thunder_far" ? .4f : .45f);
        }

        public static bool Toggle()
        {
            if (instance == null) return false;
            instance.muted = !instance.muted;
            instance.effects.mute = instance.muted; instance.wind.mute = instance.muted; instance.rain.mute = instance.muted;
            return !instance.muted;
        }

        void Update()
        {
            wind.volume = Mathf.Clamp01(Wind.Strength / 3) * .35f;
            if (Wind.Strength > .01f && !wind.isPlaying) wind.Play();
            if (Wind.Strength <= .01f && wind.isPlaying) wind.Stop();
            rain.volume = Mathf.Clamp01(Weather.RainAmount) * .4f;
            if (Weather.RainAmount > .01f && !rain.isPlaying) rain.Play();
            if (Weather.RainAmount <= .01f && rain.isPlaying) rain.Stop();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            Destroy(click); Destroy(impact); Destroy(bird); Destroy(rumble); Destroy(thunder); Destroy(monkey); Destroy(hiss);
        }
    }
}
