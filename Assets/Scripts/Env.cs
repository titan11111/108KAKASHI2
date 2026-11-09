using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    /// <summary>マウス／タッチ両対応のポインタ入力。</summary>
    public static class Ptr
    {
        static readonly List<UnityEngine.EventSystems.RaycastResult> uiHits = new List<UnityEngine.EventSystems.RaycastResult>();
        public static bool OverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            var data = new UnityEngine.EventSystems.PointerEventData(es) { position = Pos };
            uiHits.Clear(); es.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
        public static bool Pressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var p = UnityEngine.InputSystem.Pointer.current;
                return p != null && p.press.isPressed;
#else
                return Input.touchCount > 0 ? Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled : Input.GetMouseButton(0);
#endif
            }
        }
        public static Vector2 Pos
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var p = UnityEngine.InputSystem.Pointer.current;
                return p != null ? p.position.ReadValue() : Vector2.zero;
#else
                return Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
#endif
            }
        }
    }

    /// <summary>ドラッグで回せるカメラ。テスト中はスクリプトから目標を指定する。</summary>
    public class OrbitCam : MonoBehaviour
    {
        public Vector3 target = new Vector3(0, 1.25f, 0);
        public float yaw = 15f, pitch = 12f, dist = 5.2f;
        public float tYaw = 15f, tPitch = 12f, tDist = 5.2f;
        public Vector3 tTarget = new Vector3(0, 1.25f, 0);
        public bool userControl = true;
        public float shake;
        bool dragging;
        bool wasPressed;
        Vector2 last;

        void LateUpdate()
        {
            bool pressed = Ptr.Pressed;
            Vector2 pos = Ptr.Pos;
            if (userControl)
            {
                if (pressed && !wasPressed)
                {
                    dragging = !Ptr.OverUI(); last = pos;
                }
                else if (pressed && dragging)
                {
                    Vector2 d = pos - last; last = pos;
                    tYaw += d.x * 0.25f;
                    tPitch = Mathf.Clamp(tPitch - d.y * 0.15f, 2f, 55f);
                }
                if (!pressed) dragging = false;
            }
            else dragging = false;
            wasPressed = pressed;

            float k = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            yaw = Mathf.LerpAngle(yaw, tYaw, k);
            pitch = Mathf.Lerp(pitch, tPitch, k);
            dist = Mathf.Lerp(dist, tDist, k);
            target = Vector3.Lerp(target, tTarget, k);

            // yaw=0 のとき +Z 側（カカシの正面）から見る
            float cp = Mathf.Cos(pitch * Mathf.Deg2Rad);
            transform.position = target + new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad) * cp * dist,
                                                      Mathf.Sin(pitch * Mathf.Deg2Rad) * dist,
                                                      Mathf.Cos(yaw * Mathf.Deg2Rad) * cp * dist);
            if (shake > 0.001f) transform.position += Random.insideUnitSphere * shake;
            transform.LookAt(target);
        }
    }

    /// <summary>動物ごとの簡易アニメーション（脚の歩行・羽ばたき・しっぽ）。</summary>
    public class Animal : MonoBehaviour
    {
        public string kind;
        public Transform visual, wingL, wingR, tail;
        public List<Transform> legs = new List<Transform>();
        public bool moving;
        float t;
        void Update()
        {
            t += Time.deltaTime;
            if (visual != null && kind != "crow")
                visual.localPosition = new Vector3(0, moving ? Mathf.Abs(Mathf.Sin(t * 14f)) * 0.07f : 0f, 0);
            for (int i = 0; i < legs.Count; i++)
            {
                if (legs[i] == null) continue;
                float ph = (i % 2 == 0) ? 0f : Mathf.PI;
                float a = moving ? Mathf.Sin(t * 14f + ph) * 38f : 0f;
                legs[i].localRotation = Quaternion.Euler(a, 0, 0);
            }
            if (wingL != null)
            {
                float a = moving ? Mathf.Sin(t * 30f) * 55f : Mathf.Sin(t * 2f) * 4f;
                wingL.localRotation = Quaternion.Euler(0, 0, a);
                wingR.localRotation = Quaternion.Euler(0, 0, -a);
            }
            if (tail != null)
                tail.localRotation = Quaternion.Euler(0, Mathf.Sin(t * (moving ? 12f : 3f)) * 25f, 0);
        }
    }

    /// <summary>畑・光・農家・動物をまとめて作る。</summary>
    public static class Env
    {
        public static OrbitCam MakeCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Data.Hex("#f2b27a");
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            go.AddComponent<AudioListener>();
            return go.AddComponent<OrbitCam>();
        }

        public static void BuildField()
        {
            var root = new GameObject("Field").transform;
            var rnd = new System.Random(7);

            // ---- 光（夕暮れ：逆光の太陽＋手前からの補助光）----
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Data.Hex("#ffd0a0");
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(30f, -38f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Data.Hex("#c5d4ff");
            fill.intensity = 0.55f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(18f, 180f, 0f);
            QualitySettings.shadowDistance = 45f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.48f, 0.58f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Data.Hex("#f0a878");
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 150f;

            // ---- 空（夕焼け）----
            var skySh = Shader.Find("Skybox/Procedural");
            if (skySh != null)
            {
                var sm = new Material(skySh);
                sm.SetFloat("_SunSize", 0.07f);
                sm.SetFloat("_SunSizeConvergence", 5f);
                sm.SetFloat("_AtmosphereThickness", 1.8f);
                sm.SetColor("_SkyTint", Data.Hex("#ff8a4a"));
                sm.SetColor("_GroundColor", Data.Hex("#6b5a33"));
                sm.SetFloat("_Exposure", 1.15f);
                RenderSettings.skybox = sm;
                var cam = Camera.main;
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }

            // ---- 地面（土の質感）----
            Geo.Force = "soil";
            Geo.P(PrimitiveType.Plane, root, Vector3.zero, new Vector3(14, 1, 14), Data.Hex("#7a6638"));
            Geo.P(PrimitiveType.Cylinder, root, new Vector3(0, 0.03f, 0), new Vector3(1.9f, 0.03f, 1.9f), Data.Hex("#5a4527"));
            Geo.Force = null;
            // 囲い石：大きさ・向き・色をばらして、ごつごつした自然な石に
            Geo.Force = "rock";
            Color[] stoneCols = { Data.Hex("#8a8478"), Data.Hex("#77736a"), Data.Hex("#9a9486"), Data.Hex("#6f6b63") };
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f + (float)rnd.NextDouble() * 0.18f;
                float rad = 0.95f + ((float)rnd.NextDouble() - 0.5f) * 0.18f;
                float sz = 0.11f + (float)rnd.NextDouble() * 0.17f;
                var stone = Geo.P(PrimitiveType.Sphere, root,
                    new Vector3(Mathf.Cos(a) * rad, sz * 0.26f, Mathf.Sin(a) * rad),
                    new Vector3(sz * 1.5f * (0.8f + (float)rnd.NextDouble() * 0.4f), sz * (0.65f + (float)rnd.NextDouble() * 0.3f), sz * 1.25f * (0.8f + (float)rnd.NextDouble() * 0.4f)),
                    stoneCols[i % 4],
                    new Vector3((float)rnd.NextDouble() * 14f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 14f), 0.15f);
            }
            Geo.Force = null;

            // ---- 畝（うね）と風に揺れる作物 ----
            for (int xi = -8; xi <= 8; xi++)
            {
                float x = xi * 1.5f;
                if (Mathf.Abs(x) < 2.4f) continue;
                Geo.P(PrimitiveType.Cube, root, new Vector3(x, 0.12f, 0), new Vector3(0.75f, 0.24f, 34f), Data.Hex("#5a4527"));
            }
            Geo.Force = "leaf";
            Color[] riceLeaf = { Data.Hex("#8fa83a"), Data.Hex("#a0b343"), Data.Hex("#7f9a35") };
            for (int xi = -8; xi <= 8; xi++)
            {
                float x = xi * 1.5f;
                if (Mathf.Abs(x) < 2.4f) continue;
                for (int zi = -8; zi <= 8; zi++)
                {
                    float z = zi * 1.8f + (float)rnd.NextDouble() * 0.5f;
                    var plant = new GameObject("plant").transform;
                    plant.SetParent(root, false);
                    plant.localPosition = new Vector3(x + ((float)rnd.NextDouble() - 0.5f) * 0.15f, 0.22f, z);
                    var sw = plant.gameObject.AddComponent<Swayer>();
                    sw.axis = new Vector3(1, 0, 1).normalized;
                    sw.amp = 3f + (float)rnd.NextDouble() * 2f;
                    sw.freq = 1.2f + (float)rnd.NextDouble();
                    sw.phase = (float)rnd.NextDouble() * 6f;
                    float sc = 0.8f + (float)rnd.NextDouble() * 0.5f;
                    // 稲：細い葉が株になって広がり、重い穂が垂れ下がる
                    for (int k = 0; k < 4; k++)
                    {
                        float yaw = k * 90f + (float)rnd.NextDouble() * 40f;
                        var blade = Geo.Cone(plant, Vector3.zero, 0.032f * sc, 0.8f * sc, riceLeaf[(xi + zi + k + 30) % 3], Vector3.zero);
                        blade.transform.localRotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(10f + (float)rnd.NextDouble() * 14f, 0, 0);
                    }
                    Geo.Force = "straw";
                    for (int k = 0; k < 2; k++)
                    {
                        float yaw = (k * 180f + (float)rnd.NextDouble() * 60f) * Mathf.Deg2Rad;
                        var dir = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
                        var top = dir * .13f * sc + Vector3.up * .78f * sc;
                        Geo.Stick(plant, Vector3.zero, top, .014f, Data.Hex("#b9a844"));
                        var droop = Geo.Cone(plant, top, .042f * sc, .3f * sc, Data.Hex("#e3b53b"), Vector3.zero);
                        droop.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (dir * .55f + Vector3.down * .8f).normalized);
                    }
                    Geo.Force = "leaf";
                }
            }
            Geo.Force = null;

            // ---- 背景の小道具（小屋・木・藁山）----
            Geo.Force = "auto";
            // 小屋
            Geo.P(PrimitiveType.Cube, root, new Vector3(-13f, 1.3f, -12f), new Vector3(5.5f, 2.6f, 4f), Data.Hex("#5a4527"));
            Geo.P(PrimitiveType.Cube, root, new Vector3(-13f, 3.45f, -12.9f), new Vector3(6.2f, 0.18f, 3.0f), Data.Hex("#7a3b2a"), new Vector3(-32f, 0, 0));
            Geo.P(PrimitiveType.Cube, root, new Vector3(-13f, 3.45f, -11.1f), new Vector3(6.2f, 0.18f, 3.0f), Data.Hex("#7a3b2a"), new Vector3(32f, 0, 0));
            Geo.P(PrimitiveType.Cube, root, new Vector3(-13f, 1.0f, -9.98f), new Vector3(1.4f, 2.0f, 0.1f), Data.Hex("#3d2918"));
            // 木
            Vector3[] trees = { new Vector3(13f, 0, -10f), new Vector3(17f, 0, -6f), new Vector3(-19f, 0, -3f), new Vector3(9f, 0, -15f) };
            foreach (var tp in trees)
            {
                Geo.P(PrimitiveType.Cylinder, root, tp + new Vector3(0, 1.6f, 0), new Vector3(0.5f, 1.6f, 0.5f), Data.Hex("#6b4a28"));
                Geo.Sphere(root, tp + new Vector3(0, 4.0f, 0), 3.4f, Data.Hex("#3f6a2c"), 0.05f);
                Geo.Sphere(root, tp + new Vector3(1.0f, 3.4f, 0.4f), 2.4f, Data.Hex("#4d7a34"), 0.05f);
                Geo.Sphere(root, tp + new Vector3(-0.9f, 3.5f, -0.3f), 2.2f, Data.Hex("#355f27"), 0.05f);
            }
            // 藁山（横に倒した俵）
            for (int i = 0; i < 4; i++)
            {
                float hx = -8f + (i % 2) * 1.3f, hy = 0.55f + (i / 2) * 0.95f, hz = -7f;
                Geo.P(PrimitiveType.Cylinder, root, new Vector3(hx + (i / 2) * 0.65f, hy, hz), new Vector3(1.1f, 0.65f, 1.1f), Data.Hex("#d4ad3e"), new Vector3(0, 0, 90f));
            }
            Geo.Force = null;

            // ---- 柵と遠くの山 ----
            for (int i = -10; i <= 10; i++)
                Geo.Stick(root, new Vector3(i * 2.2f, 0, -16f), new Vector3(i * 2.2f, 1f, -16f), 0.1f, Data.Hex("#4a3520"));
            Geo.Stick(root, new Vector3(-22f, 0.8f, -16f), new Vector3(22f, 0.8f, -16f), 0.06f, Data.Hex("#4a3520"));
            // 里山：丸くて低い、緑の山並み（遠くほど青くかすむ）
            // Assets/Resources/Backdrop/satoyama.png (.jpg) があれば、里山の写真を背景に貼る
            var backdrop = Resources.Load<Texture2D>("Backdrop/satoyama");
            Color[] hillCols = { Data.Hex("#2f5a2c"), Data.Hex("#37652f"), Data.Hex("#2a5128") };
            if (backdrop != null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var qc = quad.GetComponent<Collider>();
                if (qc != null) UnityEngine.Object.Destroy(qc);
                quad.transform.SetParent(root, false);
                float qw = 360f, qh = qw * backdrop.height / backdrop.width;
                quad.transform.localPosition = new Vector3(0f, qh * 0.5f - 6f, -115f);
                quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                quad.transform.localScale = new Vector3(qw, qh, 1f);
                var qs = Shader.Find("Unlit/Texture");
                if (qs == null) qs = Shader.Find("Sprites/Default");
                var qm = new Material(qs);
                qm.mainTexture = backdrop;
                quad.GetComponent<Renderer>().sharedMaterial = qm;
            }
            else
            for (int layer = 0; layer < 2; layer++)
            {
                float z = layer == 0 ? -62f : -95f;
                Color baseCol = layer == 0 ? hillCols[0] : Data.Hex("#4f7a78");
                for (int i = 0; i < 6; i++)
                {
                    float cx = -75f + i * 30f + layer * 14f, ry = 11f + ((i * 7 + layer * 3) % 4) * 3f, rx = 22f + (i % 3) * 5f, rz = 14f;
                    Color col = layer == 0 ? hillCols[i % 3] : baseCol;
                    Geo.Force = "forest";
                    Geo.P(PrimitiveType.Sphere, root, new Vector3(cx, -3f, z - (i % 2) * 6f), new Vector3(rx * 2f, ry * 2f, rz * 2f), col);
                    // 山の上に雑木林のふくらみ
                    Geo.Force = "forest";
                    for (int t = -2; t <= 2; t++)
                    {
                        float u = t * 0.32f, yy = -3f + ry * Mathf.Sqrt(1f - u * u) - 1.2f;
                        Geo.Sphere(root, new Vector3(cx + u * rx, yy, z - (i % 2) * 6f + 2f), 6f + ((t + i) % 3) * 1.5f, Geo.Dark(col, 0.82f + 0.12f * ((t + i + 4) % 3)));
                    }
                    Geo.Force = null;
                }
            }
        }

        // ---------- 農家 ----------
        public static GameObject MakeFarmer(Farmer f)
        {
            var g = new GameObject("Farmer_" + f.id);
            Color skin = Data.Hex("#e8b894"), dark = Geo.Dark(f.color, 0.55f);
            Geo.Force = "auto";
            Geo.P(PrimitiveType.Capsule, g.transform, new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.45f, 0.4f), f.color);
            foreach (int s in new[] { -1, 1 })
            {
                Geo.P(PrimitiveType.Capsule, g.transform, new Vector3(s * 0.12f, 0.35f, 0), new Vector3(0.17f, 0.35f, 0.17f), dark);
                Geo.P(PrimitiveType.Cube, g.transform, new Vector3(s * .12f, .10f, .065f), new Vector3(.18f, .20f, .30f), Data.Hex("#252b28"));
                var elbow = new Vector3(s * .32f, .96f, .07f);
                var hand = new Vector3(s * .29f, f.id == "ren" ? 1.10f : .83f, f.id == "ren" ? .30f : .20f);
                Geo.Stick(g.transform, new Vector3(s * .22f, 1.23f, 0), elbow, .12f, f.color);
                Geo.Stick(g.transform, elbow, hand, .10f, f.color);
                Geo.Force = null;
                Geo.Sphere(g.transform, hand, .105f, skin);
                Geo.Force = "auto";
            }
            Geo.Force = null;
            Geo.Sphere(g.transform, new Vector3(0, 1.62f, 0), 0.3f, skin);
            Geo.Sphere(g.transform, new Vector3(-.15f, 1.62f, 0), .07f, skin);
            Geo.Sphere(g.transform, new Vector3(.15f, 1.62f, 0), .07f, skin);
            Geo.Sphere(g.transform, new Vector3(0, 1.62f, .155f), .055f, skin);
            Geo.Sphere(g.transform, new Vector3(-0.06f, 1.65f, 0.13f), 0.035f, Data.Hex("#222222"));
            Geo.Sphere(g.transform, new Vector3(0.06f, 1.65f, 0.13f), 0.035f, Data.Hex("#222222"));
            Geo.Stick(g.transform, new Vector3(-.04f, 1.55f, .137f), new Vector3(.04f, 1.55f, .137f), .012f, Data.Hex("#854b3e"));
            Geo.Force = "auto";
            if (f.id == "gen")
            {
                Geo.P(PrimitiveType.Cylinder, g.transform, new Vector3(0, 1.78f, 0), new Vector3(0.55f, 0.01f, 0.55f), Data.MatColor("straw"));
                Geo.P(PrimitiveType.Cylinder, g.transform, new Vector3(0, 1.83f, 0), new Vector3(0.24f, 0.05f, 0.24f), Data.MatColor("straw"));
                Geo.Sphere(g.transform, new Vector3(0, 1.5f, 0.12f), 0.12f, Data.Hex("#eeeeee"));  // あごひげ
                Geo.Stick(g.transform, new Vector3(-.38f, .08f, .25f), new Vector3(-.29f, 1.35f, .20f), .035f, Data.MatColor("wood"));
                Geo.P(PrimitiveType.Cube, g.transform, new Vector3(-.38f, .07f, .37f), new Vector3(.23f, .035f, .30f), Data.Hex("#555b62"), default(Vector3), .3f, .7f);
            }
            else if (f.id == "hana")
            {
                Geo.P(PrimitiveType.Sphere, g.transform, new Vector3(0, 1.7f, -0.01f), new Vector3(0.34f, 0.2f, 0.34f), Data.Hex("#d9402a"));
                Geo.Sphere(g.transform, new Vector3(0, 1.64f, -0.17f), 0.1f, Data.Hex("#d9402a"));
                Geo.P(PrimitiveType.Cylinder, g.transform, new Vector3(.32f, .60f, .20f), new Vector3(.34f, .14f, .34f), Data.Hex("#b68b4b"));
                for (int i = 0; i < 5; i++) Geo.Sphere(g.transform, new Vector3(.23f + (i % 3) * .08f, .75f, .15f + (i / 3) * .09f), .09f, Data.Hex("#dc4931"));
            }
            else
            {
                Geo.P(PrimitiveType.Sphere, g.transform, new Vector3(0, 1.72f, 0), new Vector3(0.33f, 0.16f, 0.33f), Data.Hex("#2a3550"));
                Geo.P(PrimitiveType.Cube, g.transform, new Vector3(0, 1.67f, 0.17f), new Vector3(0.2f, 0.02f, 0.14f), Data.Hex("#2a3550"));
                Geo.P(PrimitiveType.Cube, g.transform, new Vector3(.23f, 1.13f, .31f), new Vector3(.09f, .16f, .018f), Data.Hex("#222a36"), new Vector3(-20, 0, -15));
            }
            Geo.Force = null;
            return g;
        }

        static Transform LegPivot(Transform v, Animal a, Vector3 pos)
        {
            var p = new GameObject("leg").transform;
            p.SetParent(v, false);
            p.localPosition = pos;
            a.legs.Add(p);
            return p;
        }

        // ---------- 動物 ----------
        public static Animal MakeAnimal(string kind)
        {
            var g = new GameObject("Animal_" + kind);
            var a = g.AddComponent<Animal>();
            a.kind = kind;
            var v = new GameObject("v").transform;
            v.SetParent(g.transform, false);
            a.visual = v;
            Color blk = Data.Hex("#15151a");
            Geo.Force = "fur";

            if (kind == "crow")
            {
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.15f, 0), new Vector3(0.2f, 0.18f, 0.36f), blk, default(Vector3), 0.3f);
                Geo.Sphere(v, new Vector3(0, 0.27f, 0.17f), 0.14f, blk, 0.3f);
                Geo.Cone(v, new Vector3(0, 0.27f, 0.24f), 0.03f, 0.12f, Data.Hex("#c9a23a"), new Vector3(90, 0, 0));
                Geo.Sphere(v, new Vector3(0.04f, 0.3f, 0.22f), 0.025f, Color.white);
                Geo.Sphere(v, new Vector3(-0.04f, 0.3f, 0.22f), 0.025f, Color.white);
                Geo.Sphere(v, new Vector3(0.045f, 0.3f, 0.235f), 0.012f, blk);
                Geo.Sphere(v, new Vector3(-0.045f, 0.3f, 0.235f), 0.012f, blk);
                // 尾羽
                var tl = new GameObject("tail").transform; tl.SetParent(v, false); tl.localPosition = new Vector3(0, 0.15f, -0.17f);
                Geo.P(PrimitiveType.Cube, tl, new Vector3(0, 0, -0.1f), new Vector3(0.1f, 0.02f, 0.22f), blk);
                var wl = new GameObject("wl").transform; wl.SetParent(v, false); wl.localPosition = new Vector3(-0.09f, 0.2f, 0);
                var wr = new GameObject("wr").transform; wr.SetParent(v, false); wr.localPosition = new Vector3(0.09f, 0.2f, 0);
                Geo.P(PrimitiveType.Cube, wl, new Vector3(-0.2f, 0, 0), new Vector3(0.4f, 0.02f, 0.22f), blk);
                Geo.P(PrimitiveType.Cube, wl, new Vector3(-0.42f, 0, -0.03f), new Vector3(0.14f, 0.015f, 0.16f), blk);
                Geo.P(PrimitiveType.Cube, wr, new Vector3(0.2f, 0, 0), new Vector3(0.4f, 0.02f, 0.22f), blk);
                Geo.P(PrimitiveType.Cube, wr, new Vector3(0.42f, 0, -0.03f), new Vector3(0.14f, 0.015f, 0.16f), blk);
                a.wingL = wl; a.wingR = wr; a.tail = tl;
            }
            else if (kind == "monkey")
            {
                Color br = Data.Hex("#8a5a34"), fc = Data.Hex("#e0b48a");
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.5f, 0), new Vector3(0.34f, 0.42f, 0.3f), br);
                Geo.Sphere(v, new Vector3(0, 0.85f, 0.04f), 0.26f, br);
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.82f, 0.15f), new Vector3(0.17f, 0.15f, 0.08f), fc);
                Geo.Sphere(v, new Vector3(-0.15f, 0.87f, 0.02f), 0.1f, fc);
                Geo.Sphere(v, new Vector3(0.15f, 0.87f, 0.02f), 0.1f, fc);
                Geo.Sphere(v, new Vector3(-0.05f, 0.87f, 0.17f), 0.03f, blk);
                Geo.Sphere(v, new Vector3(0.05f, 0.87f, 0.17f), 0.03f, blk);
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.76f, 0.2f), new Vector3(0.05f, 0.03f, 0.03f), blk);
                foreach (int s in new[] { -1, 1 })
                {
                    var arm = LegPivot(v, a, new Vector3(s * 0.2f, 0.62f, 0.05f));
                    Geo.P(PrimitiveType.Capsule, arm, new Vector3(0, -0.14f, 0.03f), new Vector3(0.09f, 0.16f, 0.09f), br, new Vector3(15, 0, 0));
                    var leg = LegPivot(v, a, new Vector3(s * 0.1f, 0.3f, 0));
                    Geo.P(PrimitiveType.Capsule, leg, new Vector3(0, -0.13f, 0), new Vector3(0.1f, 0.15f, 0.1f), br);
                }
                var tl = new GameObject("tail").transform; tl.SetParent(v, false); tl.localPosition = new Vector3(0, 0.35f, -0.12f);
                Geo.Stick(tl, Vector3.zero, new Vector3(0, 0.22f, -0.28f), 0.05f, br);
                Geo.Stick(tl, new Vector3(0, 0.22f, -0.28f), new Vector3(0, 0.5f, -0.2f), 0.04f, br);
                a.tail = tl;
            }
            else if (kind == "boar")
            {
                Color br = Data.Hex("#4a3a2e");
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.55f, 0), new Vector3(0.6f, 0.55f, 1.05f), br);
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.55f, 0.55f), new Vector3(0.42f, 0.4f, 0.45f), Data.Hex("#3a2c22"));
                Geo.P(PrimitiveType.Cylinder, v, new Vector3(0, 0.5f, 0.82f), new Vector3(0.2f, 0.07f, 0.2f), Data.Hex("#b98d78"), new Vector3(90, 0, 0), 0.1f);
                foreach (int s in new[] { -1, 1 })
                {
                    Geo.Cone(v, new Vector3(s * 0.12f, 0.45f, 0.8f), 0.025f, 0.14f, Color.white, new Vector3(-30, 0, -s * 25));
                    Geo.Cone(v, new Vector3(s * 0.12f, 0.78f, 0.45f), 0.06f, 0.14f, br);
                    Geo.Sphere(v, new Vector3(s * 0.1f, 0.62f, 0.72f), 0.03f, blk);
                    foreach (float z in new[] { -0.3f, 0.3f })
                    {
                        var leg = LegPivot(v, a, new Vector3(s * 0.2f, 0.4f, z));
                        Geo.P(PrimitiveType.Cylinder, leg, new Vector3(0, -0.17f, 0), new Vector3(0.1f, 0.17f, 0.1f), Data.Hex("#2e231b"));
                    }
                }
                for (int i = 0; i < 6; i++)
                    Geo.Cone(v, new Vector3(0, 0.82f, -0.35f + i * 0.14f), 0.04f, 0.14f, Data.Hex("#2a1f17"));
                var tl = new GameObject("tail").transform; tl.SetParent(v, false); tl.localPosition = new Vector3(0, 0.62f, -0.52f);
                Geo.Stick(tl, Vector3.zero, new Vector3(0, 0.12f, -0.14f), 0.03f, Data.Hex("#2a1f17"));
                a.tail = tl;
            }
            else // deer
            {
                Color tan = Data.Hex("#b98a54");
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 0.85f, 0), new Vector3(0.42f, 0.45f, 0.95f), tan);
                Geo.Stick(v, new Vector3(0, 0.95f, 0.35f), new Vector3(0, 1.4f, 0.55f), 0.14f, tan);
                Geo.P(PrimitiveType.Sphere, v, new Vector3(0, 1.45f, 0.62f), new Vector3(0.2f, 0.2f, 0.3f), tan);
                Geo.Sphere(v, new Vector3(0, 1.4f, 0.78f), 0.09f, Data.Hex("#2a1f17"));
                Color horn = Data.Hex("#d8c8a8");
                foreach (int s in new[] { -1, 1 })
                {
                    Geo.Sphere(v, new Vector3(s * 0.08f, 1.5f, 0.7f), 0.035f, blk);
                    // 耳
                    Geo.Cone(v, new Vector3(s * 0.1f, 1.52f, 0.52f), 0.05f, 0.16f, tan, new Vector3(0, 0, -s * 70f));
                    Geo.Stick(v, new Vector3(s * 0.07f, 1.55f, 0.55f), new Vector3(s * 0.2f, 1.9f, 0.5f), 0.025f, horn);
                    Geo.Stick(v, new Vector3(s * 0.2f, 1.9f, 0.5f), new Vector3(s * 0.12f, 2.12f, 0.58f), 0.02f, horn);
                    Geo.Stick(v, new Vector3(s * 0.15f, 1.72f, 0.52f), new Vector3(s * 0.3f, 1.85f, 0.6f), 0.018f, horn);
                    foreach (float z in new[] { -0.3f, 0.3f })
                    {
                        var leg = LegPivot(v, a, new Vector3(s * 0.12f, 0.72f, z));
                        Geo.Stick(leg, Vector3.zero, new Vector3(0, -0.72f, 0), 0.07f, Geo.Dark(tan, 0.8f));
                    }
                }
                for (int i = 0; i < 6; i++)
                    Geo.Sphere(v, new Vector3((i % 2 == 0 ? -1 : 1) * 0.14f, 0.95f, -0.2f + i * 0.08f), 0.05f, Color.white);
                var tl = new GameObject("tail").transform; tl.SetParent(v, false); tl.localPosition = new Vector3(0, 0.9f, -0.46f);
                Geo.Sphere(tl, new Vector3(0, 0, -0.04f), 0.12f, Color.white);
                a.tail = tl;
            }
            Geo.Force = null;
            return a;
        }
    }
}
