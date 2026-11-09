using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    /// <summary>選んだパーツからカカシの3Dモデルをプリミティブで組み立てる。</summary>
    public class Scarecrow : MonoBehaviour
    {
        public Dictionary<string, Transform> Parts = new Dictionary<string, Transform>();
        readonly Dictionary<string, PartPose> poses = new Dictionary<string, PartPose>();
        readonly Dictionary<string, Vector3> pivots = new Dictionary<string, Vector3>();

        public PartPose Pose(string slot)
        {
            if (!poses.TryGetValue(slot, out var pose)) poses[slot] = pose = new PartPose();
            return pose;
        }

        public int ShapePenalty
        {
            get
            {
                float penalty = 0;
                foreach (var pair in Parts)
                {
                    if (pair.Value == null || pair.Value.childCount == 0) continue;
                    var p = Pose(pair.Key);
                    penalty += Mathf.Abs(p.offset.x) * 5 + Mathf.Abs(p.offset.z) * 5;
                    penalty += Mathf.Abs(p.tilt) * .08f + Mathf.Abs(p.scale - 1) * 6;
                    if (pair.Key == "pole") penalty += Mathf.Abs(p.tilt) * .3f;
                }
                return Mathf.RoundToInt(penalty);
            }
        }

        public void ApplyPose(string slot)
        {
            if (!Parts.TryGetValue(slot, out var part) || part == null) return;
            var p = Pose(slot);
            p.Clamp();
            part.localPosition = pivots[slot] + p.offset;
            part.localRotation = Quaternion.Euler(0, p.twist, p.tilt);
            part.localScale = Vector3.one * p.scale;
        }

        public void ResetShapes()
        {
            poses.Clear();
            foreach (var slot in Parts.Keys) ApplyPose(slot);
        }

        const float AY = 1.62f;   // 腕の高さ
        const float HY = 1.97f;   // 頭の中心
        const float SH = 0.188f;  // 顔を貼る球の半径

        static readonly Color[] STRAW = { Data.Hex("#e6c254"), Data.Hex("#d4ad3e"), Data.Hex("#f0d378"), Data.Hex("#c79a3a") };
        static readonly Color[] GREEN = { Data.Hex("#78b04c"), Data.Hex("#5f9a3a"), Data.Hex("#8cc25a"), Data.Hex("#4d8a32") };
        static readonly Color[] COTTON = { Data.Hex("#ffffff"), Data.Hex("#f4f1e8"), Data.Hex("#ebe7da"), Data.Hex("#f8f6ef") };

        static float Surf(float x, float y) { return Mathf.Sqrt(Mathf.Max(0f, SH * SH - x * x - (y - HY) * (y - HY))); }

        public void Build(Dictionary<string, string> sel)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            Parts.Clear();
            pivots.Clear();
            Geo.Force = "auto"; // 色から木目・わら・布柄などを自動で貼る
            try
            {
                foreach (var slot in Data.Slots)
                {
                    var root = new GameObject(slot.id).transform;
                    root.SetParent(transform, false);
                    Parts[slot.id] = root;
                    pivots[slot.id] = Vector3.zero;
                    string id = sel[slot.id];
                    if (id == "none") continue;
                    var rnd = new System.Random(slot.id.GetHashCode() ^ id.GetHashCode());
                    switch (slot.id)
                    {
                        case "pole": Pole(root, id); break;
                        case "body": Body(root, id, rnd); break;
                        case "head": Head(root, id, rnd); break;
                        case "arms": Arms(root, id, rnd); break;
                        case "cloth": Cloth(root, id, rnd); break;
                        case "hat": Hat(root, id); break;
                        case "face": Face(root, id, rnd); break;
                        case "deco": Deco(root, id, rnd); break;
                        case "acc": Acc(root, id, rnd); break;
                    }
                    // Geometry is authored in scarecrow space. Move the origin to the
                    // part's center before applying rotation/scale so it pivots in place.
                    var renderers = root.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        var pivot = root.InverseTransformPoint(bounds.center);
                        if (slot.id == "pole") pivot = Vector3.zero;
                        for (int i = 0; i < root.childCount; i++) root.GetChild(i).localPosition -= pivot;
                        pivots[slot.id] = pivot;
                    }
                    ApplyPose(slot.id);
                }
            }
            finally { Geo.Force = null; }
        }

        public void SetPartVisible(string slot, bool on)
        {
            Transform t;
            if (Parts.TryGetValue(slot, out t) && t != null) t.gameObject.SetActive(on);
        }

        // ---------- 共通 ----------
        static void Stalks(Transform p, System.Random r, int n, Vector3 origin, float spread, Vector3 dir, float len, float jitter, Color[] cols, float thick = 0.016f)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 d = (dir + new Vector3(Geo.Rf(r, -1, 1), Geo.Rf(r, -1, 1), Geo.Rf(r, -1, 1)) * jitter).normalized;
                Vector3 o = origin + new Vector3(Geo.Rf(r, -1, 1), Geo.Rf(r, -1, 1), Geo.Rf(r, -1, 1)) * spread;
                float l = len * Geo.Rf(r, 0.8f, 1.1f);
                Geo.Stick(p, o, o + d * l, thick, cols[i % cols.Length]);
            }
        }

        static void Blades(Transform p, System.Random r, int n, Vector3 origin, float spread, float len, float tilt, bool down, Color[] cols)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Geo.Rf(r, 0, 360);
                Vector3 o = origin + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Geo.Rf(r, -.05f, .05f), Mathf.Sin(a * Mathf.Deg2Rad)) * spread;
                Vector3 e = new Vector3(Geo.Rf(r, -tilt, tilt) + (down ? 180f : 0f), 0, Geo.Rf(r, -tilt, tilt));
                Geo.Cone(p, o, 0.022f, len * Geo.Rf(r, 0.7f, 1.1f), cols[i % cols.Length], e);
            }
        }

        static GameObject Pivot(Transform p, Vector3 pos)
        {
            var g = new GameObject("pivot");
            g.transform.SetParent(p, false);
            g.transform.localPosition = pos;
            return g;
        }

        // ---------- 支柱 ----------
        static void Pole(Transform p, string id)
        {
            Color w = Data.MatColor("wood"), dk = Geo.Dark(w, 0.5f);
            if (id == "log")
            {
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 1.0f, 0), new Vector3(0.2f, 1.2f, 0.2f), w);
                Geo.Sphere(p, new Vector3(0.09f, 0.6f, 0.04f), 0.07f, dk);
                Geo.Sphere(p, new Vector3(-0.05f, 1.1f, 0.09f), 0.06f, dk);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 2.201f, 0), new Vector3(0.18f, 0.005f, 0.18f), Geo.Dark(w, 1f) * 1.2f);
            }
            else if (id == "branch")
            {
                Geo.Stick(p, new Vector3(0, -0.1f, 0), new Vector3(0.03f, 1.1f, -0.02f), 0.07f, w);
                Geo.Stick(p, new Vector3(0.03f, 1.1f, -0.02f), new Vector3(0, 2.15f, 0), 0.06f, w);
                Geo.Stick(p, new Vector3(0.03f, 0.9f, 0), new Vector3(0.2f, 1.15f, 0.02f), 0.03f, w);
                Geo.Stick(p, new Vector3(0, 0.6f, 0), new Vector3(-0.17f, 0.85f, 0.01f), 0.025f, w);
            }
            else
            {
                foreach (int s in new[] { -1, 1 })
                    Geo.Stick(p, new Vector3(s * 0.12f, -0.1f, 0), new Vector3(s * 0.06f, 2.0f, 0), 0.08f, w);
                Geo.Stick(p, new Vector3(0, 1.2f, 0), new Vector3(0, 2.1f, 0), 0.09f, w);
                Geo.P(PrimitiveType.Cube, p, new Vector3(0, 0.45f, 0), new Vector3(0.3f, 0.035f, 0.035f), dk, new Vector3(0, 0, 3));
                Geo.P(PrimitiveType.Cube, p, new Vector3(0, 0.95f, 0), new Vector3(0.24f, 0.035f, 0.035f), dk, new Vector3(0, 0, -3));
            }
        }

        // ---------- 胴 ----------
        static void Body(Transform p, string id, System.Random r)
        {
            if (id == "straw")
            {
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 1.25f, 0), new Vector3(0.4f, 0.38f, 0.4f), Data.Hex("#d1a947"));
                Stalks(p, r, 70, new Vector3(0, 1.0f, 0), 0.17f, Vector3.up, 0.5f, 0.12f, STRAW, 0.02f);
                foreach (float y in new[] { 0.95f, 1.25f, 1.55f })
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, y, 0), new Vector3(0.44f, 0.012f, 0.44f), Data.Hex("#b99b66"));
            }
            else if (id == "cotton")
            {
                for (int i = 0; i < 16; i++)
                {
                    float a = Geo.Rf(r, 0, 6.28f), y = Geo.Rf(r, -1f, 1f);
                    float rr = 0.12f * Mathf.Sqrt(1 - y * y * 0.5f);
                    Geo.Sphere(p, new Vector3(Mathf.Cos(a) * rr, 1.26f + y * 0.3f, Mathf.Sin(a) * rr), Geo.Rf(r, 0.2f, 0.3f), COTTON[i % 4], 0.02f);
                }
                Geo.Sphere(p, new Vector3(0, 1.26f, 0), 0.34f, COTTON[1], 0.02f);
            }
            else if (id == "grass")
            {
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 1.25f, 0), new Vector3(0.3f, 0.36f, 0.3f), Data.Hex("#3f6a2c"));
                Blades(p, r, 46, new Vector3(0, 1.58f, 0), 0.15f, 0.55f, 14f, true, GREEN);
            }
            else // cloth
            {
                Color[] cs = { Data.Hex("#c9553a"), Data.Hex("#e0b54a"), Data.Hex("#3f7a58") };
                for (int i = 0; i < 3; i++)
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 0.99f + i * 0.26f, 0), new Vector3(0.41f - i * 0.005f, 0.125f, 0.41f - i * 0.005f), cs[i]);
                foreach (float y in new[] { 0.98f, 1.52f })
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, y, 0), new Vector3(0.43f, 0.01f, 0.43f), Data.Hex("#3d2918"));
                Blades(p, r, 20, new Vector3(0, 0.9f, 0), 0.14f, 0.2f, 20f, true, STRAW);
            }
        }

        // ---------- 頭 ----------
        static void Head(Transform p, string id, System.Random r)
        {
            Vector3 c = new Vector3(0, HY, 0);
            if (id == "straw")
            {
                Geo.Sphere(p, c, 0.36f, Data.Hex("#cdb27a"));
                Stalks(p, r, 36, c + new Vector3(0, 0.1f, 0), 0.08f, new Vector3(0, 1, -0.2f), 0.22f, 0.9f, STRAW);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 1.8f, 0), new Vector3(0.22f, 0.012f, 0.22f), Data.Hex("#b99b66"));
            }
            else if (id == "grass")
            {
                Geo.Sphere(p, c, 0.36f, Data.Hex("#3f6a2c"));
                Blades(p, r, 40, c + new Vector3(0, 0.08f, 0), 0.1f, 0.32f, 60f, false, GREEN);
            }
            else if (id == "cotton")
            {
                Geo.Sphere(p, c, 0.32f, COTTON[1], 0.02f);
                for (int i = 0; i < 12; i++)
                {
                    float a = Geo.Rf(r, 0, 6.28f), y = Geo.Rf(r, -0.8f, 1f);
                    float rr = 0.15f * Mathf.Sqrt(1 - y * y * 0.5f);
                    Geo.Sphere(p, c + new Vector3(Mathf.Cos(a) * rr, y * 0.13f, Mathf.Sin(a) * rr * 0.85f - 0.01f), Geo.Rf(r, 0.1f, 0.15f), COTTON[i % 4], 0.02f);
                }
            }
            else // mask
            {
                Color w = Data.MatColor("wood");
                Geo.Sphere(p, c, 0.38f, w);
                Geo.P(PrimitiveType.Cube, p, new Vector3(0, HY + 0.06f, 0.16f), new Vector3(0.3f, 0.035f, 0.05f), Geo.Dark(w, 0.5f), new Vector3(-12, 0, 0));
                Geo.Cone(p, new Vector3(0, HY - 0.01f, 0.17f), 0.032f, 0.08f, w, new Vector3(90, 0, 0));
                foreach (int s in new[] { -1, 1 }) Geo.Sphere(p, new Vector3(s * 0.19f, HY, 0), 0.07f, w);
            }
        }

        // ---------- 腕 ----------
        static void Fingers(Transform p, int sx, float x, float y, Color col, float metal)
        {
            for (int k = -1; k <= 1; k++)
                Geo.Stick(p, new Vector3(sx * x, y, 0), new Vector3(sx * (x + 0.1f), y - 0.07f + k * 0.01f, k * 0.06f), 0.014f, col, metal);
        }

        static void Arms(Transform p, string id, System.Random r)
        {
            Color w = Data.MatColor("wood"), wire = Data.MatColor("wire");
            if (id == "straw")
            {
                foreach (int s in new[] { -1, 1 })
                {
                    Stalks(p, r, 26, new Vector3(s * 0.1f, AY, 0), 0.035f, new Vector3(s, -0.1f, 0), 0.8f, 0.05f, STRAW);
                    foreach (float x in new[] { 0.35f, 0.62f })
                        Geo.P(PrimitiveType.Cylinder, p, new Vector3(s * x, AY - 0.03f, 0), new Vector3(0.1f, 0.012f, 0.1f), Data.Hex("#b99b66"), new Vector3(0, 0, 90));
                }
            }
            else if (id == "wire")
            {
                foreach (int s in new[] { -1, 1 })
                {
                    Vector3[] pts = { new Vector3(s * 0.08f, AY, 0), new Vector3(s * 0.4f, AY + 0.02f, 0.02f), new Vector3(s * 0.7f, AY - 0.06f, 0), new Vector3(s * 0.9f, AY - 0.2f, 0.03f) };
                    for (int i = 0; i < pts.Length - 1; i++) Geo.Stick(p, pts[i], pts[i + 1], 0.024f, wire, 0.8f);
                    Fingers(p, s, 0.9f, AY - 0.2f, wire, 0.8f);
                }
            }
            else
            {
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, AY, 0), new Vector3(0.06f, 0.9f, 0.06f), w, new Vector3(0, 0, 90));
                foreach (int s in new[] { -1, 1 })
                {
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(s * 0.15f, AY, 0), new Vector3(0.1f, 0.012f, 0.1f), Data.Hex("#b99b66"), new Vector3(0, 0, 90));
                    Fingers(p, s, 0.9f, AY, id == "hybrid" ? wire : Geo.Dark(w, 0.5f), id == "hybrid" ? 0.8f : 0f);
                    if (id == "hybrid")
                        for (int i = 0; i < 12; i++)
                        {
                            float t = i / 11f, a = t * 6f * Mathf.PI;
                            Geo.Sphere(p, new Vector3(s * (0.2f + t * 0.65f), AY + Mathf.Cos(a) * 0.045f, Mathf.Sin(a) * 0.045f), 0.022f, wire, 0.4f, 0.8f);
                        }
                }
            }
        }

        // ---------- 服・キレ ----------
        static void Cloth(Transform p, string id, System.Random r)
        {
            Color c = Data.ClothColor(id);
            float met = id == "gold" ? 0.5f : 0f, sm = id == "gold" ? 0.5f : 0.1f;
            Geo.P(PrimitiveType.Cube, p, new Vector3(0, 1.58f, 0), new Vector3(1.0f, 0.04f, 0.42f), c, default(Vector3), sm, met);
            // 前後の垂れ布は上端を支点にして、風でひらひら揺らす
            foreach (int fz in new[] { 1, -1 })
            {
                var fp = Pivot(p, new Vector3(0, 1.62f, fz * 0.2f));
                var fsw = fp.AddComponent<Swayer>();
                fsw.axis = Vector3.right; fsw.amp = 5f; fsw.freq = 1.7f; fsw.phase = fz;
                Geo.P(PrimitiveType.Cube, fp.transform, new Vector3(0, -0.24f, 0), new Vector3(0.48f, 0.46f, 0.03f), c, new Vector3(-fz * 6, 0, 0), sm, met);
            }
            foreach (int s in new[] { -1, 1 })
            {
                var sp = Pivot(p, new Vector3(s * 0.4f, 1.58f, 0));
                var ssw = sp.AddComponent<Swayer>();
                ssw.axis = Vector3.forward; ssw.amp = 6f; ssw.freq = 2.1f; ssw.phase = s * 1.5f;
                Geo.P(PrimitiveType.Cube, sp.transform, new Vector3(s * 0.15f, -0.08f, 0), new Vector3(0.3f, 0.2f, 0.03f), c, new Vector3(0, 0, s * 8), sm, met);
            }
            if (id == "patch")
            {
                Color[] pc = { Data.Hex("#e0b54a"), Data.Hex("#3f7a58"), Data.Hex("#e8dcc0"), Data.Hex("#2f4d8a") };
                for (int i = 0; i < 6; i++)
                    Geo.P(PrimitiveType.Cube, p, new Vector3(Geo.Rf(r, -0.15f, 0.15f), Geo.Rf(r, 1.25f, 1.5f), 0.22f), new Vector3(0.1f, 0.1f, 0.01f), pc[i % 4]);
            }
            if (id == "indigo")
                for (int i = 0; i < 8; i++)
                    Geo.P(PrimitiveType.Cube, p, new Vector3(Geo.Rf(r, -0.18f, 0.18f), Geo.Rf(r, 1.2f, 1.55f), 0.222f), new Vector3(0.03f, 0.03f, 0.005f), Data.Hex("#c9d4ef"));
            // 背中でなびくキレ
            var piv = Pivot(p, new Vector3(0, 1.7f, -0.22f));
            var sw = piv.AddComponent<Swayer>();
            sw.axis = Vector3.right; sw.amp = 12f; sw.freq = 2f;
            float w2 = id == "red" ? 0.2f : 0.12f, len = id == "red" ? 1.1f : 0.7f;
            Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0, -len * 0.5f, 0), new Vector3(w2, len, 0.015f), c, default(Vector3), sm, met);
        }

        // ---------- 帽子 ----------
        static void Hat(Transform p, string id)
        {
            Color st = Data.MatColor("straw");
            if (id == "strawhat")
            {
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 2.1f, 0), new Vector3(0.84f, 0.0125f, 0.84f), st);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 2.17f, 0), new Vector3(0.34f, 0.07f, 0.34f), st);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0, 2.13f, 0), new Vector3(0.38f, 0.015f, 0.38f), Data.Hex("#c23a2a"));
            }
            else if (id == "kasa")
            {
                Geo.Cone(p, new Vector3(0, 2.06f, 0), 0.5f, 0.28f, st);
                foreach (int s in new[] { -1, 1 })
                {
                    Geo.Stick(p, new Vector3(s * 0.2f, 2.08f, 0.04f), new Vector3(s * 0.14f, 1.9f, 0.14f), 0.016f, Data.Hex("#c23a2a"));
                    Geo.Stick(p, new Vector3(s * 0.14f, 1.9f, 0.14f), new Vector3(0, 1.82f, 0.17f), 0.016f, Data.Hex("#c23a2a"));
                }
            }
            else if (id == "tenugui")
            {
                Color blue = Data.Hex("#2c4a8c");
                Geo.P(PrimitiveType.Sphere, p, new Vector3(0, 2.0f, 0), new Vector3(0.4f, 0.34f, 0.4f), blue);
                Geo.Sphere(p, new Vector3(0, 2.03f, -0.2f), 0.07f, blue);
                var piv = Pivot(p, new Vector3(0, 2.03f, -0.2f));
                var sw = piv.AddComponent<Swayer>(); sw.axis = Vector3.right; sw.amp = 15f; sw.freq = 2.2f;
                Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0, -0.2f, -0.02f), new Vector3(0.09f, 0.4f, 0.012f), blue);
            }
            else // horns
            {
                Color wire = Data.MatColor("wire");
                for (int k = -2; k <= 2; k++)
                {
                    float a = k * 0.55f, bx = Mathf.Sin(a) * 0.17f, bz = Mathf.Cos(a) * 0.17f;
                    Geo.Cone(p, new Vector3(bx, 2.08f, bz), 0.02f, 0.3f - Mathf.Abs(k) * 0.05f, wire, new Vector3(Mathf.Cos(a) * 18f, 0, -Mathf.Sin(a) * 18f), 0.5f, 0.8f);
                }
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2f / 12f;
                    Geo.Sphere(p, new Vector3(Mathf.Cos(a) * 0.17f, 2.08f, Mathf.Sin(a) * 0.17f), 0.02f, wire, 0.5f, 0.8f);
                }
            }
        }

        // ---------- 顔 ----------
        static void Face(Transform p, string id, System.Random r)
        {
            Color blk = Data.Hex("#111114");
            if (id == "buttons")
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float x = s * 0.07f, y = 2.0f;
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(x, y, Surf(x, y) + 0.002f), new Vector3(0.07f, 0.01f, 0.07f), Geo.Dark(Data.MatColor("wood"), 0.4f), new Vector3(90, 0, 0));
                    foreach (float ang in new[] { 40f, -40f })
                        Geo.P(PrimitiveType.Cube, p, new Vector3(x, y, Surf(x, y) + 0.014f), new Vector3(0.05f, 0.006f, 0.004f), blk, new Vector3(0, 0, ang));
                }
                for (int i = -3; i <= 3; i++)
                {
                    float x = i * 0.03f, y = 1.9f + 0.025f * Mathf.Pow(x / 0.09f, 2f);
                    Geo.P(PrimitiveType.Cube, p, new Vector3(x, y, Surf(x, y) + 0.003f), new Vector3(0.008f, 0.03f, 0.006f), blk, new Vector3(0, 0, -x * 170f));
                }
            }
            else if (id == "wireeyes")
            {
                Color wire = Data.MatColor("wire");
                foreach (int s in new[] { -1, 1 })
                {
                    float x = s * 0.07f, y = 2.0f, z = Surf(x, y) + 0.01f;
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * Mathf.PI * 2f / 10f;
                        Geo.Sphere(p, new Vector3(x + Mathf.Cos(a) * 0.04f, y + Mathf.Sin(a) * 0.04f, z), 0.012f, wire, 0.5f, 0.8f);
                    }
                    Geo.Sphere(p, new Vector3(x, y, z), 0.028f, blk);
                    Geo.Stick(p, new Vector3(x - 0.045f, y + 0.07f, z), new Vector3(x + 0.045f, y + 0.065f, z + 0.005f), 0.01f, wire, 0.8f);
                }
                Geo.Stick(p, new Vector3(-0.08f, 1.9f, Surf(-0.08f, 1.9f) + 0.008f), new Vector3(0.08f, 1.9f, Surf(0.08f, 1.9f) + 0.008f), 0.01f, wire, 0.8f);
            }
            else if (id == "whisker")
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float x = s * 0.07f;
                    Geo.Sphere(p, new Vector3(x, 2.0f, Surf(x, 2.0f) + 0.003f), 0.028f, blk);
                }
                Geo.Cone(p, new Vector3(0, 1.95f, Surf(0, 1.95f) + 0.02f), 0.025f, 0.06f, Data.Hex("#c79a3a"), new Vector3(90, 0, 0));
                foreach (int s in new[] { -1, 1 })
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 a = new Vector3(s * 0.02f, 1.915f, Surf(0.02f, 1.915f) + 0.012f);
                        Vector3 d = new Vector3(s, -0.15f + (i - 3) * 0.08f, 0.12f).normalized;
                        Geo.Stick(p, a, a + d * 0.2f, 0.008f, STRAW[i % 4]);
                    }
            }
            else // oni
            {
                Color red = Data.Hex("#a82b1c");
                Geo.P(PrimitiveType.Sphere, p, new Vector3(0, HY, 0.015f), new Vector3(0.4f, 0.4f, 0.38f), red, default(Vector3), 0.3f);
                foreach (int s in new[] { -1, 1 })
                {
                    Geo.Sphere(p, new Vector3(s * 0.075f, 2.0f, 0.18f), 0.07f, Color.white, 0.3f);
                    Geo.Sphere(p, new Vector3(s * 0.075f, 2.0f, 0.21f), 0.03f, blk);
                    Geo.P(PrimitiveType.Cube, p, new Vector3(s * 0.075f, 2.055f, 0.2f), new Vector3(0.1f, 0.03f, 0.04f), blk, new Vector3(0, 0, -s * 22f));
                    Geo.Cone(p, new Vector3(s * 0.09f, 2.12f, 0.08f), 0.04f, 0.14f, Data.Hex("#c79a62"), new Vector3(0, 0, -s * 20f));
                    Geo.Cone(p, new Vector3(s * 0.05f, 1.86f, 0.2f), 0.015f, 0.05f, Color.white, new Vector3(180, 0, 0));
                }
                Geo.Cone(p, new Vector3(0, 1.93f, 0.21f), 0.03f, 0.07f, red, new Vector3(90, 0, 0));
                Geo.P(PrimitiveType.Cube, p, new Vector3(0, 1.9f, 0.2f), new Vector3(0.13f, 0.025f, 0.03f), blk);
            }
        }

        // ---------- 飾り ----------
        static void Deco(Transform p, string id, System.Random r)
        {
            Color wire = Data.MatColor("wire");
            if (id == "pinwheel")
            {
                Geo.Stick(p, new Vector3(0.75f, 1.62f, 0.05f), new Vector3(0.75f, 1.78f, 0.22f), 0.016f, Data.Hex("#3d2918"));
                var piv = Pivot(p, new Vector3(0.75f, 1.78f, 0.23f));
                piv.AddComponent<Spinner>().speed = 150f;
                Color[] cs = { Data.Hex("#e0473a"), Data.Hex("#f2c33f"), Data.Hex("#3f7fd0"), Color.white };
                for (int k = 0; k < 4; k++)
                {
                    var arm = new GameObject("blade").transform;
                    arm.SetParent(piv.transform, false);
                    arm.localRotation = Quaternion.Euler(0, 0, k * 90f);
                    Geo.P(PrimitiveType.Cube, arm, new Vector3(0.09f, 0.04f, 0), new Vector3(0.18f, 0.09f, 0.008f), cs[k]);
                }
            }
            else if (id == "mobile")
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = -0.5f + i * 0.2f;
                    var piv = Pivot(p, new Vector3(x, 1.66f, 0.22f));
                    var sw = piv.AddComponent<Swayer>();
                    sw.amp = 14f; sw.freq = 2f + i * 0.2f; sw.phase = i;
                    Geo.Stick(piv.transform, Vector3.zero, new Vector3(0, -0.15f - (i % 3) * 0.05f, 0), 0.006f, wire, 0.8f);
                    Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0, -0.18f - (i % 3) * 0.05f, 0), new Vector3(0.05f, 0.05f, 0.005f), Data.Hex("#ffe9a8"), new Vector3(0, 0, 45), 0.9f, 0.9f, 0.3f);
                }
            }
            else if (id == "cloak")
            {
                Blades(p, r, 40, new Vector3(0, 1.62f, -0.2f), 0.18f, 0.9f, 8f, true, GREEN);
            }
            else if (id == "bell")
            {
                Color gold = Data.Hex("#d4a02a");
                for (int i = 0; i < 9; i++)
                {
                    float a = Mathf.PI * (i / 8f);
                    Geo.Sphere(p, new Vector3(Mathf.Cos(a) * 0.17f, 1.72f - Mathf.Sin(a) * 0.04f, 0.14f + Mathf.Sin(a) * 0.04f), 0.02f, wire, 0.5f, 0.8f);
                }
                var piv = Pivot(p, new Vector3(0, 1.72f, 0.2f));
                piv.AddComponent<Swayer>().amp = 10f;
                Geo.Sphere(piv.transform, new Vector3(0, -0.04f, 0), 0.07f, gold, 0.7f, 0.8f);
            }
            else // shime
            {
                Color st = Data.MatColor("straw");
                for (int i = 0; i <= 14; i++)
                {
                    float t = i / 14f, x = Mathf.Lerp(-0.3f, 0.3f, t);
                    Geo.Sphere(p, new Vector3(x, 1.66f - Mathf.Sin(t * Mathf.PI) * 0.07f, 0.24f), 0.045f, STRAW[i % 4]);
                }
                foreach (float x in new[] { -0.15f, 0f, 0.15f })
                {
                    var piv = Pivot(p, new Vector3(x, 1.6f - 0.05f * (1f - Mathf.Abs(x) * 3f), 0.25f));
                    piv.AddComponent<Swayer>().amp = 12f;
                    Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0, -0.05f, 0), new Vector3(0.045f, 0.1f, 0.004f), Color.white);
                    Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0.01f, -0.13f, 0), new Vector3(0.045f, 0.08f, 0.004f), Color.white, new Vector3(0, 0, 20));
                }
            }
        }

        // ---------- アクセント ----------
        static void Acc(Transform p, string id, System.Random r)
        {
            Color wood = Data.MatColor("wood"), wire = Data.MatColor("wire");
            if (id == "naruko")
            {
                Geo.Stick(p, new Vector3(-0.55f, 1.6f, 0.05f), new Vector3(-0.55f, 1.32f, 0.05f), 0.01f, Data.Hex("#b99b66"));
                var piv = Pivot(p, new Vector3(-0.55f, 1.32f, 0.05f));
                piv.AddComponent<Swayer>().amp = 8f;
                Geo.P(PrimitiveType.Cube, piv.transform, new Vector3(0, -0.04f, 0), new Vector3(0.3f, 0.07f, 0.02f), wood);
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.12f + i * 0.08f;
                    var b = Pivot(piv.transform, new Vector3(x, -0.07f, 0));
                    var sw = b.AddComponent<Swayer>(); sw.amp = 20f; sw.freq = 3f; sw.phase = i * 1.3f;
                    Geo.P(PrimitiveType.Cylinder, b.transform, new Vector3(0, -0.1f, 0), new Vector3(0.025f, 0.1f, 0.025f), Data.Hex("#c8b060"));
                }
            }
            else if (id == "cd")
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.35f - i * 0.13f;
                    var piv = Pivot(p, new Vector3(x, AY - 0.02f, 0.06f));
                    var sw = piv.AddComponent<Swayer>(); sw.amp = 18f; sw.freq = 2.2f + i * 0.3f; sw.phase = i;
                    float len = 0.22f + (i % 2) * 0.1f;
                    Geo.Stick(piv.transform, Vector3.zero, new Vector3(0, -len, 0), 0.006f, wire);
                    Geo.P(PrimitiveType.Cylinder, piv.transform, new Vector3(0, -len - 0.06f, 0), new Vector3(0.12f, 0.003f, 0.12f), Data.Hex("#d9e4f0"), new Vector3(90, 0, 0), 0.95f, 1f, 0.15f);
                }
            }
            else if (id == "chochin")
            {
                Geo.Stick(p, new Vector3(0.55f, 1.62f, 0.08f), new Vector3(0.55f, 1.46f, 0.1f), 0.01f, Data.Hex("#3d2918"));
                var piv = Pivot(p, new Vector3(0.55f, 1.46f, 0.1f));
                piv.AddComponent<Swayer>().amp = 7f;
                Geo.P(PrimitiveType.Sphere, piv.transform, new Vector3(0, -0.12f, 0), new Vector3(0.22f, 0.26f, 0.22f), Data.Hex("#d9402a"), default(Vector3), 0.3f, 0f, 0.9f);
                Geo.P(PrimitiveType.Cylinder, piv.transform, new Vector3(0, -0.01f, 0), new Vector3(0.1f, 0.012f, 0.1f), Data.Hex("#2a1a10"));
                Geo.P(PrimitiveType.Cylinder, piv.transform, new Vector3(0, -0.25f, 0), new Vector3(0.1f, 0.012f, 0.1f), Data.Hex("#2a1a10"));
                var lg = new GameObject("lamp").AddComponent<Light>();
                lg.transform.SetParent(piv.transform, false);
                lg.transform.localPosition = new Vector3(0, -0.12f, 0);
                lg.type = LightType.Point; lg.color = Data.Hex("#ff9a4a"); lg.range = 3.5f; lg.intensity = 1.4f;
            }
            else if (id == "kuwa")
            {
                Geo.Stick(p, new Vector3(0.93f, 0.55f, 0.08f), new Vector3(0.93f, 1.65f, 0.04f), 0.05f, wood);
                Geo.P(PrimitiveType.Cube, p, new Vector3(0.93f, 0.52f, 0.14f), new Vector3(0.22f, 0.15f, 0.015f), Data.Hex("#8a94a6"), new Vector3(-25, 0, 0), 0.6f, 0.7f);
                Geo.Stick(p, new Vector3(0.93f, 0.58f, 0.1f), new Vector3(0.93f, 0.5f, 0.2f), 0.035f, wood);
            }
            else if (id == "kago")
            {
                Color bam = Data.Hex("#b8914a");
                Geo.Stick(p, new Vector3(-0.9f, 1.55f, 0.05f), new Vector3(-0.9f, 1.4f, 0.05f), 0.012f, Data.Hex("#3d2918"));
                var piv = Pivot(p, new Vector3(-0.9f, 1.4f, 0.05f));
                piv.AddComponent<Swayer>().amp = 5f;
                Geo.P(PrimitiveType.Cylinder, piv.transform, new Vector3(0, -0.1f, 0), new Vector3(0.28f, 0.1f, 0.28f), bam);
                Geo.P(PrimitiveType.Cylinder, piv.transform, new Vector3(0, -0.01f, 0), new Vector3(0.3f, 0.012f, 0.3f), Geo.Dark(bam, 0.7f));
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 2.1f;
                    Vector3 o = new Vector3(Mathf.Cos(a) * 0.06f, -0.02f, Mathf.Sin(a) * 0.06f);
                    Geo.Cone(piv.transform, o + new Vector3(0, 0.0f, 0), 0.04f, 0.16f, Color.white, new Vector3(180, 0, 0));
                    Geo.Cone(piv.transform, o + new Vector3(0, 0.0f, 0), 0.03f, 0.14f, Data.Hex("#4d8a32"));
                }
            }
            else // issho
            {
                Color glass = Data.Hex("#5a6b3a");
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.18f, 0.35f), new Vector3(0.14f, 0.18f, 0.14f), glass, default(Vector3), 0.8f);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.42f, 0.35f), new Vector3(0.05f, 0.08f, 0.05f), glass, default(Vector3), 0.8f);
                for (int i = 0; i < 12; i++)
                    Geo.P(PrimitiveType.Cylinder, p, new Vector3(0.3f + Mathf.Cos(i * 0.52f) * 0.075f, 0.15f, 0.35f + Mathf.Sin(i * 0.52f) * 0.075f), new Vector3(0.018f, 0.12f, 0.018f), STRAW[i % 4]);
                Geo.P(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.5f, 0.35f), new Vector3(0.06f, 0.015f, 0.06f), Data.Hex("#e8dcc0"));
            }
        }
    }
}
