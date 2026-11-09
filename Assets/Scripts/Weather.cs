using System.Collections;
using UnityEngine;

namespace Kakashi
{
    /// <summary>夕暮れ ⇄ 夜の嵐：月明かり・霧・空、2層の大雨（雨筋＋霧雨）、地面の波紋、稲妻（ギザギザの雷光＋閃光＋遅れて雷鳴）。</summary>
    public class Weather : MonoBehaviour
    {
        public static Weather I;
        /// <summary>雨の強さ 0..1（雨音の音量にも使う）</summary>
        public static float RainAmount;
        /// <summary>0 = 夕暮れ / 1 = 真夜中</summary>
        public float Night;

        Light sun, fill;
        Material sky;
        ParticleSystem rain, mist, ripple;
        Transform rainRoot;
        LineRenderer bolt, fork;
        float flash;
        Color sunC, fillC, ambC, fogC, tintC;
        float sunI, fillI, fogS, fogE, exposure, sunSize;
        Quaternion sunRot;
        static Texture2D ringTex, streakTex;

        public static Weather Create(Light sun, Light fill, Material sky)
        {
            var w = new GameObject("Weather").AddComponent<Weather>();
            w.Init(sun, fill, sky);
            return w;
        }

        void Init(Light s, Light f, Material m)
        {
            I = this;
            sun = s; fill = f; sky = m;
            sunC = s.color; sunI = s.intensity; sunRot = s.transform.rotation;
            fillC = f.color; fillI = f.intensity;
            ambC = RenderSettings.ambientLight;
            fogC = RenderSettings.fogColor; fogS = RenderSettings.fogStartDistance; fogE = RenderSettings.fogEndDistance;
            if (sky != null && sky.HasProperty("_SkyTint"))
            {
                tintC = sky.GetColor("_SkyTint");
                exposure = sky.GetFloat("_Exposure");
                sunSize = sky.GetFloat("_SunSize");
            }
            else sky = null;
            BuildRain();
            bolt = MakeBolt("Bolt", 0.9f);
            fork = MakeBolt("BoltFork", 0.45f);
        }

        // ---------- 画像なしで作る雨の素材 ----------
        static Texture2D MakeRing()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.12f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return tex;
        }

        static Texture2D MakeStreak()
        {
            const int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float ax = 1f - Mathf.Abs((x + .5f) / w * 2f - 1f);
                    float ay = Mathf.Sin((y + .5f) / h * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, ax * ax * ay));
                }
            tex.Apply();
            return tex;
        }

        static Material SpriteMat(Texture2D tex)
        {
            Shader shd = Shader.Find("Sprites/Default");
            if (shd == null) shd = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shd == null) return null;
            var m = new Material(shd);
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        ParticleSystem MakeRainLayer(string name, float size, float speed, float life, Color col, int maxP, float len)
        {
            var go = new GameObject(name);
            go.transform.SetParent(rainRoot, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
            main.startColor = col;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxP;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(44f, 44f, 0.2f);
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.lengthScale = len;
            psr.velocityScale = 0.035f;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            var mat = SpriteMat(streakTex);
            if (mat != null) psr.sharedMaterial = mat;
            return ps;
        }

        void BuildRain()
        {
            if (streakTex == null) streakTex = MakeStreak();
            if (ringTex == null) ringTex = MakeRing();

            rainRoot = new GameObject("Rain").transform;
            rainRoot.SetParent(transform, false);
            rainRoot.position = new Vector3(0f, 16f, 1f);
            rainRoot.rotation = Quaternion.Euler(80f, 0f, 0f);
            // 太い雨筋（手前に見える大粒）と、細かい霧雨（奥行き・密度）の2層
            rain = MakeRainLayer("Streaks", 0.07f, 28f, 0.75f, new Color(0.82f, 0.9f, 1f, 0.6f), 6000, 7f);
            mist = MakeRainLayer("Drizzle", 0.035f, 22f, 0.95f, new Color(0.75f, 0.85f, 1f, 0.3f), 9000, 5f);

            // 地面の波紋（雨粒が落ちて広がる輪）
            var go = new GameObject("Ripples");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0.06f, 0f);
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ripple = go.AddComponent<ParticleSystem>();
            ripple.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ripple.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.85f, 0.92f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var em = ripple.emission; em.rateOverTime = 0f;
            var sh = ripple.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(24f, 24f, 0.05f);
            var sol = ripple.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 3f));
            var col = ripple.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            var mat = SpriteMat(ringTex);
            if (mat != null) psr.sharedMaterial = mat;
        }

        LineRenderer MakeBolt(string name, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.numCornerVertices = 2;
            lr.widthMultiplier = width;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
            lr.startColor = lr.endColor = new Color(0.92f, 0.96f, 1f, 1f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            var mat = SpriteMat(null);
            if (mat != null) lr.sharedMaterial = mat;
            lr.enabled = false;
            return lr;
        }

        // ---------- 毎フレーム ----------
        void Update()
        {
            if (sun == null || fill == null) return;
            float k = Mathf.Clamp01(Night);
            var add = new Color(flash * 0.55f, flash * 0.6f, flash * 0.75f, 0f);
            sun.color = Color.Lerp(sunC, new Color(0.55f, 0.66f, 1f), k);
            sun.intensity = Mathf.Lerp(sunI, 0.55f, k) + flash * 2f;
            sun.transform.rotation = Quaternion.Slerp(sunRot, Quaternion.Euler(52f, -25f, 0f), k);
            fill.color = Color.Lerp(fillC, new Color(0.45f, 0.55f, 1f), k);
            fill.intensity = Mathf.Lerp(fillI, 0.28f, k) + flash * 0.8f;
            RenderSettings.ambientLight = Color.Lerp(ambC, new Color(0.14f, 0.17f, 0.30f), k) + add;
            RenderSettings.fogColor = Color.Lerp(fogC, new Color(0.05f, 0.07f, 0.14f), k) + add * 0.5f;
            RenderSettings.fogStartDistance = Mathf.Lerp(fogS, 5f, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogE, 60f, k);
            if (sky != null)
            {
                sky.SetFloat("_Exposure", Mathf.Lerp(exposure, 0.14f, k) + flash * 1.4f);
                sky.SetColor("_SkyTint", Color.Lerp(tintC, new Color(0.15f, 0.18f, 0.34f), k));
                sky.SetFloat("_SunSize", Mathf.Lerp(sunSize, 0f, k));
            }
            Scenery.SkyNight(k, flash);   // 夕焼け空ドームも夜・稲妻に合わせる
            float r = Mathf.Clamp01(RainAmount);
            // 風が強いほど雨は斜めに降る
            rainRoot.rotation = Quaternion.Euler(80f - Wind.Strength * 3.5f, Wind.Strength * 10f, 0f);
            Drive(rain, r * 2600f);
            Drive(mist, r * 4200f);
            Drive(ripple, r * 520f);
        }

        void Drive(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = rate;
            if (rate > 1f && !ps.isPlaying) ps.Play();
        }

        // ---------- 稲妻 ----------
        /// <summary>稲妻：ギザギザの雷光が空に走り、画面がぱっと光って、少し遅れて雷鳴（近いほど遅れが短く、大きい）。</summary>
        public void Strike() { StartCoroutine(StrikeRoutine()); }

        void DrawBolt(LineRenderer lr, Vector3 top, Vector3 bottom, int n, float jitter)
        {
            lr.positionCount = n;
            Vector3 p = top;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                Vector3 line = Vector3.Lerp(top, bottom, k);
                if (i > 0 && i < n - 1)
                {
                    p = line + new Vector3(Random.Range(-jitter, jitter), 0f, Random.Range(-jitter, jitter));
                }
                else p = line;
                lr.SetPosition(i, p);
            }
            lr.enabled = true;
        }

        void ShowBolt()
        {
            var c = Camera.main;
            if (c == null) return;
            Vector3 f = c.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            f.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, f);
            Vector3 ground = c.transform.position + f * Random.Range(40f, 70f) + right * Random.Range(-22f, 22f);
            ground.y = 0f;
            Vector3 top = ground + new Vector3(Random.Range(-6f, 6f), 55f, Random.Range(-6f, 6f));
            DrawBolt(bolt, top, ground, 18, 2.6f);
            // 途中から枝分かれ
            int mid = 7;
            Vector3 from = bolt.GetPosition(mid);
            Vector3 to = from + new Vector3(Random.Range(-14f, 14f), -Random.Range(14f, 24f), Random.Range(-6f, 6f));
            DrawBolt(fork, from, to, 8, 1.8f);
        }

        void HideBolt()
        {
            if (bolt != null) bolt.enabled = false;
            if (fork != null) fork.enabled = false;
        }

        IEnumerator ThunderLater(float delay, bool close)
        {
            yield return new WaitForSeconds(delay);
            KakashiAudio.Cue(close ? "thunder" : "thunder_far");
        }

        IEnumerator StrikeRoutine()
        {
            bool close = Random.value < 0.4f;
            float power = close ? 1f : 0.55f;
            StartCoroutine(ThunderLater(close ? 0.15f : Random.Range(0.7f, 1.6f), close));
            ShowBolt();
            flash = power;
            yield return new WaitForSeconds(0.07f);
            flash = 0.1f * power;
            HideBolt();
            yield return new WaitForSeconds(0.08f);
            ShowBolt();                       // 二度光る（本物の稲妻のちらつき）
            flash = 0.85f * power;
            yield return new WaitForSeconds(0.06f);
            HideBolt();
            for (float t = 0f; t < 1f; t += Time.deltaTime * 3f)
            {
                flash = Mathf.Lerp(0.85f * power, 0f, t);
                yield return null;
            }
            flash = 0f;
        }

        /// <summary>夕暮れ・雨なしに戻す。</summary>
        public void ResetNow()
        {
            StopAllCoroutines();
            Night = 0f; RainAmount = 0f; flash = 0f;
            HideBolt();
            if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (mist != null) mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (ripple != null) ripple.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnDestroy() { if (I == this) I = null; RainAmount = 0f; }
    }
}
