using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    public static class Wind { public static float Strength; }

    /// <summary>風でゆらゆら揺れる。</summary>
    public class Swayer : MonoBehaviour
    {
        public float amp = 6f, freq = 1.5f, phase;
        public Vector3 axis = Vector3.forward;
        Quaternion baseRot;
        void Start() { baseRot = transform.localRotation; }
        void Update()
        {
            float k = 1f + Wind.Strength * 2.5f;
            float f = freq * (1f + Wind.Strength * 0.5f);
            transform.localRotation = baseRot * Quaternion.AngleAxis(Mathf.Sin(Time.time * f + phase) * amp * k, axis);
        }
    }

    /// <summary>風車用。風が強いほど速く回る。</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 120f;
        void Update() { transform.Rotate(0f, 0f, speed * (1f + Wind.Strength * 3f) * Time.deltaTime, Space.Self); }
    }

    /// <summary>プリミティブ生成のヘルパー（Prefab・外部アセット不要）。</summary>
    public static class Geo
    {
        static Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Dictionary<string, Mesh> cones = new Dictionary<string, Mesh>();
        static Shader sh;

        /// <summary>
        /// null = 質感なし / "auto" = 色から木・わら・草・綿・布柄を自動判定 / それ以外 = その質感を強制。
        /// 生成中だけ一時的に設定して、終わったら null に戻す。
        /// </summary>
        public static string Force;

        static string PickKind(Color c, float smooth, float metal, float emis)
        {
            if (Force == null) return null;
            if (Force != "auto") return (metal > 0.3f || emis > 0f || smooth > 0.5f) ? null : Force;
            switch (ColorUtility.ToHtmlStringRGB(c))
            {
                case "24386E": return "indigo";
                case "CF3E2C": return "redcloth";
                case "D4A02A": return "goldcloth";
                case "C9553A": return "patch";
            }
            if (metal > 0.3f || emis > 0f || smooth > 0.5f) return null;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (v < 0.2f) return null;
            if (s < 0.14f && v > 0.8f) return "cotton";
            if (h >= 0.05f && h <= 0.11f && s >= 0.3f && v < 0.78f) return "wood";
            if (h > 0.09f && h <= 0.17f && s >= 0.28f && v >= 0.6f) return "straw";
            if (h >= 0.2f && h <= 0.42f) return "leaf";
            return null;
        }

        public static Material Mat(Color c, float smooth = 0.1f, float metal = 0f, float emis = 0f)
        {
            string kind = PickKind(c, smooth, metal, emis);
            string key = ColorUtility.ToHtmlStringRGB(c) + "_" + smooth + "_" + metal + "_" + emis + "_" + kind;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            if (sh == null)
            {
                sh = Shader.Find("Standard");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
            }
            m = new Material(sh);
            m.color = c;
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emis > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emis);
            }
            if (kind != null)
            {
                m.mainTexture = Tex.Get(kind);
                m.mainTextureScale = Tex.Scale(kind);
                if (Tex.IsPattern(kind)) m.color = Color.white;
            }
            cache[key] = m;
            return m;
        }

        public static Color Dark(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }
        public static float Rf(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        public static GameObject P(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color col,
            Vector3 euler = default(Vector3), float smooth = 0.1f, float metal = 0f, float emis = 0f)
        {
            var go = GameObject.CreatePrimitive(t);
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.GetComponent<Renderer>().sharedMaterial = Mat(col, smooth, metal, emis);
            return go;
        }

        public static GameObject Sphere(Transform parent, Vector3 pos, float dia, Color col, float smooth = 0.1f, float metal = 0f)
        {
            return P(PrimitiveType.Sphere, parent, pos, new Vector3(dia, dia, dia), col, default(Vector3), smooth, metal);
        }

        /// <summary>2点間を結ぶ細い棒。</summary>
        public static GameObject Stick(Transform parent, Vector3 a, Vector3 b, float thick, Color col, float metal = 0f)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            var go = P(PrimitiveType.Cylinder, parent, (a + b) * 0.5f, new Vector3(thick, len * 0.5f, thick), col, default(Vector3), 0.1f, metal);
            if (len > 0.0001f) go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            return go;
        }

        public static GameObject Cone(Transform parent, Vector3 pos, float r, float h, Color col,
            Vector3 euler = default(Vector3), float smooth = 0.1f, float metal = 0f)
        {
            var go = new GameObject("cone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh(r, h, 16);
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(col, smooth, metal);
            return go;
        }

        /// <summary>底面が y=0、頂点が y=h の円錐メッシュ。</summary>
        static Mesh ConeMesh(float r, float h, int seg)
        {
            string key = r + "_" + h + "_" + seg;
            Mesh m;
            if (cones.TryGetValue(key, out m) && m != null) return m;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = 2f * Mathf.PI * i / seg, a1 = 2f * Mathf.PI * (i + 1) / seg;
                int b = v.Count;
                v.Add(new Vector3(0, h, 0));
                v.Add(new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r));
                v.Add(new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r));
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                int c = v.Count;
                v.Add(Vector3.zero);
                v.Add(v[b + 1]);
                v.Add(v[b + 2]);
                t.Add(c); t.Add(c + 1); t.Add(c + 2);
            }
            m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            cones[key] = m;
            return m;
        }
    }
}
