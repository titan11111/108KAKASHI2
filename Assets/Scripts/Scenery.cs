using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    /// <summary>
    /// 里山と木の手作りメッシュ（画像・Prefab不要）。
    /// 木は「幹1＋葉3色＋草1」の最大5ドローコール、山は「山肌1＋林4＋岩土」にまとめ、スマホの負荷に配慮。
    /// </summary>
    public static class Scenery
    {
        /// <summary>false にすると従来の球ベースの景色に戻る（変更前後の比較用）。</summary>
        public const bool Enabled = true;
        /// <summary>true で木も山も全部まとめて新方式にする（従来の一括スイッチ）。</summary>
        public const bool Expand = false;
        /// <summary>【手順①】true で山だけ全部を新方式にする。</summary>
        public const bool ExpandHills = true;
        /// <summary>【手順②】true で木を全部新方式にし、背景の木も足す。①の間は false（見本の1本だけ）。</summary>
        public const bool ExpandTrees = false;
        public static bool TreesExpanded { get { return Expand || ExpandTrees; } }

        /// <summary>見本の木の位置。畑の奥・柵の向こうで、通常のカメラ（カカシ正面）に入る場所。従来は(17,0,-6)で画面外だった。</summary>
        public static readonly Vector3 SampleTree = new Vector3(7.5f, 0f, -19.5f);
        public static readonly Vector3 OldSampleTree = new Vector3(17f, 0f, -6f);

        public static bool UseNewTree(Vector3 p)
        {
            if (!Enabled) return false;
            return TreesExpanded || (Mathf.Abs(p.x - SampleTree.x) < 0.01f && Mathf.Abs(p.z - SampleTree.z) < 0.01f);
        }

        public static bool UseNewHill(int layer, int i)
        {
            if (!Enabled) return false;
            return Expand || ExpandHills || (layer == 0 && i == 3);
        }

        // ------------------------------------------------------------------
        //  【手順③】夕方の空・霞・影
        // ------------------------------------------------------------------
        /// <summary>false で従来の空・光・フォグに戻る（Enabled=false でも戻る）。</summary>
        public const bool EveningSky = true;
        /// <summary>太陽の高さ（度）。従来30。少し低くして畝の影を約3割長くする。</summary>
        public const float SunElevation = 25f;
        /// <summary>影の中の色。従来(0.52,0.48,0.58)より青寄りにして、暖かい日向との冷暖の対比を出す。</summary>
        public static readonly Color EveningAmbient = new Color(0.47f, 0.47f, 0.64f);
        /// <summary>フォグ色：地平線近くの空（黄色寄り）に寄せ、山と空の境目に薄い霞をかける。従来 #f0a878。</summary>
        public static Color EveningFog { get { return Data.Hex("#f6bd86"); } }

        static Material skyMat;

        static Color SkyCol(float e)
        {
            float[] st = { 0f, 5f, 10f, 18f, 28f, 45f, 70f, 90f };
            string[] hx = { "#ffe9a8", "#ffd27c", "#ffb06c", "#f2866a", "#d6657c", "#a8508a", "#7a4590", "#5d3d7e" };
            if (e <= st[0]) return Data.Hex(hx[0]);
            for (int i = 1; i < st.Length; i++)
                if (e <= st[i]) return Color.Lerp(Data.Hex(hx[i - 1]), Data.Hex(hx[i]), (e - st[i - 1]) / (st[i] - st[i - 1]));
            return Data.Hex(hx[hx.Length - 1]);
        }

        /// <summary>
        /// 夕焼けの空ドーム：地平線近くは明るい黄色寄り→上へ橙→朱→赤紫。太陽の方向に薄い輝き。
        /// 頂点カラーだけの1メッシュ・1ドローコール（Sprites/Default＝ライト/フォグ無し）。背景キュー(1000)で最初に描く。
        /// 地平線の上10〜20度ほどしかゲーム画面に映らないので、色の変化はその範囲に詰めてある。
        /// </summary>
        public static bool SkyDome(Transform parent)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) return false;
            float[] el = { -20f, -4f, 0f, 2f, 4f, 6f, 8f, 11f, 14f, 18f, 23f, 30f, 40f, 55f, 70f, 90f };
            const int seg = 48;
            const float R = 200f;
            Vector3 sunDir = -(Quaternion.Euler(SunElevation, -38f, 0f) * Vector3.forward);
            Color glowC = Data.Hex("#fff0b8");
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            for (int r = 0; r < el.Length; r++)
            {
                float e = el[r] * Mathf.Deg2Rad;
                Color baseC = SkyCol(el[r]);
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    Vector3 d = new Vector3(Mathf.Cos(e) * Mathf.Cos(a), Mathf.Sin(e), Mathf.Cos(e) * Mathf.Sin(a));
                    float glow = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(d, sunDir)), 6f) * 0.45f;
                    Color cc = Color.Lerp(baseC, glowC, glow);
                    cc.a = 1f;
                    v.Add(d * R);
                    c.Add(cc);
                }
            }
            for (int r = 0; r < el.Length - 1; r++)
                for (int s = 0; s < seg; s++)
                {
                    int i0 = r * (seg + 1) + s, i1 = i0 + 1, i2 = i0 + seg + 1, i3 = i2 + 1;
                    t.Add(i0); t.Add(i2); t.Add(i1);
                    t.Add(i1); t.Add(i2); t.Add(i3);
                }
            var m = new Mesh();
            m.name = "SkyDome";
            m.SetVertices(v);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 500f);
            var go = new GameObject("SkyDome");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            skyMat = new Material(sh);
            skyMat.renderQueue = 1000;                 // 背景として最初に描く（雨や稲妻より後ろ）
            mr.sharedMaterial = skyMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return true;
        }

        /// <summary>Weather.cs から毎フレーム呼ぶ：夜(k=0..1)で空を暗い青に、稲妻(flash)で一瞬明るく。</summary>
        public static void SkyNight(float k, float flash)
        {
            if (skyMat == null) return;
            Color tint = Color.Lerp(Color.white, new Color(0.07f, 0.09f, 0.20f, 1f), k) + new Color(flash * 0.5f, flash * 0.55f, flash * 0.7f, 0f);
            tint.a = 1f;
            skyMat.color = tint;
        }

        /// <summary>同じ材質の遠景メッシュを1ドローコールにまとめる（遠景は動かないので静的バッチ）。</summary>
        public static void BatchStatic(Transform container)
        {
            if (container == null || container.childCount == 0) return;
            StaticBatchingUtility.Combine(container.gameObject);
        }

        // ------------------------------------------------------------------
        class MeshBuilder
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector2> uv = new List<Vector2>();
            public List<int> t = new List<int>();
            public Mesh Build(string name)
            {
                var m = new Mesh();
                m.name = name;
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v);
                m.SetUVs(0, uv);
                m.SetTriangles(t, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        static Material M(Color c, string force)
        {
            string old = Geo.Force;
            Geo.Force = force;
            var m = Geo.Mat(c, 0.05f);
            Geo.Force = old;
            return m;
        }

        static void Emit(Transform parent, string name, MeshBuilder mb, Material mat)
        {
            if (mb.v.Count == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.Build(name);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // 背景は影なし（軽量化）
            mr.receiveShadows = false;
        }

        static Material shadowMat;
        /// <summary>ふちがぼやけた丸影用の半透明マテリアル（全部の木で共有）。影ではなく板1枚なので軽い。</summary>
        static Material ShadowMat()
        {
            if (shadowMat != null) return shadowMat;
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) return null;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(0.17f, 0.14f, 0.26f, a * 0.40f));   // 青みのある暗色で薄く
                }
            tex.Apply();
            shadowMat = new Material(sh);
            shadowMat.mainTexture = tex;
            return shadowMat;
        }

        /// <summary>足元の薄い丸影。太陽の向きへ少しずらして長くし、地面になじませる。</summary>
        static void FootShadow(Transform root, float radius, Vector3 sunDirLocal)
        {
            var mat = ShadowMat();
            if (mat == null) return;
            Vector3 f = new Vector3(sunDirLocal.x, 0f, sunDirLocal.z);
            f = f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized;
            Vector3 r = new Vector3(f.z, 0f, -f.x);
            Vector3 c = f * radius * 0.45f + Vector3.up * 0.04f;
            float a = radius * 1.35f, b = radius;
            var mb = new MeshBuilder();
            mb.v.Add(c - f * a - r * b); mb.uv.Add(new Vector2(0f, 0f));
            mb.v.Add(c - f * a + r * b); mb.uv.Add(new Vector2(0f, 1f));
            mb.v.Add(c + f * a + r * b); mb.uv.Add(new Vector2(1f, 1f));
            mb.v.Add(c + f * a - r * b); mb.uv.Add(new Vector2(1f, 0f));
            mb.t.Add(0); mb.t.Add(1); mb.t.Add(2);
            mb.t.Add(0); mb.t.Add(2); mb.t.Add(3);
            Emit(root, "FootShadow", mb, mat);
        }

        /// <summary>根元が太く、先が細い枝・幹（a→b）。</summary>
        static void Tube(MeshBuilder mb, Vector3 a, Vector3 b, float r0, float r1, int sides)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            d /= len;
            Vector3 u = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 w = Vector3.Cross(d, u);
            int baseI = mb.v.Count;
            for (int ring = 0; ring < 2; ring++)
            {
                Vector3 c = ring == 0 ? a : b;
                float r = ring == 0 ? r0 : r1;
                for (int i = 0; i <= sides; i++)
                {
                    float ang = i / (float)sides * Mathf.PI * 2f;
                    mb.v.Add(c + (u * Mathf.Cos(ang) + w * Mathf.Sin(ang)) * r);
                    mb.uv.Add(new Vector2(i / (float)sides, ring * len * 0.5f));
                }
            }
            for (int i = 0; i < sides; i++)
            {
                int a0 = baseI + i, a1 = a0 + 1, b0 = baseI + sides + 1 + i, b1 = b0 + 1;
                mb.t.Add(a0); mb.t.Add(a1); mb.t.Add(b0);
                mb.t.Add(a1); mb.t.Add(b1); mb.t.Add(b0);
            }
        }

        /// <summary>でこぼこにゆがめた低ポリの球（葉のかたまり・低木・岩）。</summary>
        static void Blob(MeshBuilder mb, Vector3 c, Vector3 radii, System.Random rnd, int lat, int lon, float rough)
        {
            int baseI = mb.v.Count;
            var disp = new float[lat + 1, lon];
            for (int i = 0; i <= lat; i++)
                for (int j = 0; j < lon; j++)
                    disp[i, j] = 1f + ((float)rnd.NextDouble() * 2f - 1f) * rough;
            for (int j = 1; j < lon; j++) { disp[0, j] = disp[0, 0]; disp[lat, j] = disp[lat, 0]; }
            for (int i = 0; i <= lat; i++)
            {
                float phi = i / (float)lat * Mathf.PI;
                for (int j = 0; j <= lon; j++)
                {
                    float th = j / (float)lon * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    mb.v.Add(c + Vector3.Scale(dir, radii) * disp[i, j % lon]);
                    mb.uv.Add(new Vector2(j / (float)lon, i / (float)lat));
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int v00 = baseI + i * (lon + 1) + j, v01 = v00 + 1, v10 = v00 + lon + 1, v11 = v10 + 1;
                    mb.t.Add(v00); mb.t.Add(v01); mb.t.Add(v10);
                    mb.t.Add(v01); mb.t.Add(v11); mb.t.Add(v10);
                }
        }

        // ==================================================================
        //  木
        // ==================================================================
        static void AddLeaf(MeshBuilder[] leaf, System.Random rnd, Vector3 c, float s, float heightFrac)
        {
            // 上のほうは明るい黄緑（夕日が当たる）、下は濃い緑
            int ci = heightFrac > 0.82f ? 2 : (rnd.NextDouble() < 0.45 ? 1 : 0);
            Vector3 radii = new Vector3(s * (0.9f + 0.4f * (float)rnd.NextDouble()),
                                        s * (0.62f + 0.23f * (float)rnd.NextDouble()),
                                        s * (0.9f + 0.3f * (float)rnd.NextDouble()));
            Blob(leaf[ci], c, radii, rnd, 6, 8, 0.38f);
        }

        /// <summary>
        /// 1本の木：根元が太く上へ細くなる幹、途中から枝分かれ、大小の葉のかたまりを不規則に重ねる。
        /// 葉は枝先にだけ付け、幹の中心は空けるので、葉の間から枝と向こうの空が見える。
        /// </summary>
        public static Transform Tree(Transform parent, Vector3 pos, int seed, float h, float leanDeg, float yaw)
        {
            var rnd = new System.Random(seed);
            float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }
            var root = new GameObject("Tree_" + seed).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = new MeshBuilder();
            var leaf = new[] { new MeshBuilder(), new MeshBuilder(), new MeshBuilder() };
            var grass = new MeshBuilder();

            float la = R(0f, Mathf.PI * 2f);
            Vector3 ld = new Vector3(Mathf.Cos(la), 0f, Mathf.Sin(la));          // 傾く向き
            float lean = Mathf.Tan(leanDeg * Mathf.Deg2Rad);
            float ba = R(0f, Mathf.PI * 2f);
            Vector3 bias = new Vector3(Mathf.Cos(ba), 0f, Mathf.Sin(ba));        // 葉がよく茂る側（左右非対称）
            float rb = h * 0.07f;                                                 // 根元の半径

            // 幹：根元（太い）→ 分かれ目 → 頂部（細い）
            Vector3 p0 = new Vector3(0f, -0.15f, 0f);
            Vector3 p1 = ld * (lean * h * 0.18f) + new Vector3(R(-0.03f, 0.03f) * h, h * 0.28f, R(-0.03f, 0.03f) * h);
            Vector3 p2 = ld * (lean * h * 0.45f) + new Vector3(R(-0.04f, 0.04f) * h, h * 0.56f, R(-0.04f, 0.04f) * h);
            Vector3 p3 = p2 + ld * (lean * h * 0.2f) + new Vector3(R(-0.05f, 0.05f) * h, h * 0.3f, R(-0.05f, 0.05f) * h);
            Tube(wood, p0, p1, rb * 1.55f, rb * 0.9f, 7);   // 根元が広がって先へ細る
            Tube(wood, p1, p2, rb * 0.9f, rb * 0.62f, 7);
            Tube(wood, p2, p3, rb * 0.62f, rb * 0.22f, 6);
            // 根元の張り出し（地面を掴む根）
            for (int k = 0; k < 3; k++)
            {
                float a = (k / 3f + R(-0.1f, 0.1f)) * Mathf.PI * 2f;
                Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Tube(wood, p0 + o * rb * 0.4f + Vector3.up * rb * 0.6f, o * rb * 2.1f + new Vector3(0f, -0.04f, 0f), rb * 0.55f, rb * 0.18f, 5);
            }

            // 枝：途中（幹の上半分）から分かれ、さらに小枝へ
            var tips = new List<Vector3>();
            var mids = new List<Vector3>();
            tips.Add(p3);
            int nMain = 5 + (int)R(0f, 2.99f);
            for (int k = 0; k < nMain; k++)
            {
                float az = (k / (float)nMain) * Mathf.PI * 2f + R(-0.5f, 0.5f);
                float up = R(0.5f, 1.05f);
                Vector3 dir = new Vector3(Mathf.Sin(up) * Mathf.Cos(az), Mathf.Cos(up), Mathf.Sin(up) * Mathf.Sin(az)) + ld * 0.15f;
                dir.Normalize();
                float len = h * R(0.30f, 0.48f);
                Vector3 s = Vector3.Lerp(p1, p2, R(0.35f, 1f));
                Vector3 e = s + dir * len;
                float r0 = rb * R(0.4f, 0.55f);
                Tube(wood, s, e, r0, r0 * 0.35f, 5);
                mids.Add(Vector3.Lerp(s, e, 0.62f));
                int nSub = 1 + (int)R(0f, 1.99f);
                for (int q = 0; q < nSub; q++)
                {
                    Vector3 s2 = Vector3.Lerp(s, e, R(0.5f, 0.85f));
                    Vector3 d2 = (dir * 0.55f + new Vector3(R(-0.8f, 0.8f), R(0f, 0.5f), R(-0.8f, 0.8f))).normalized;
                    Vector3 e2 = s2 + d2 * (len * R(0.35f, 0.6f));
                    Tube(wood, s2, e2, r0 * 0.5f, r0 * 0.18f, 4);
                    tips.Add(e2);
                }
                tips.Add(e);
            }

            // 葉：枝先に大小のかたまりを不規則に重ねる（茂る側は大きく＝輪郭が左右非対称）
            foreach (var tip in tips)
            {
                if (tip != p3 && R(0f, 1f) < 0.2f) continue;   // 葉のない枝先も残す＝枝と空が見える
                Vector3 flat = new Vector3(tip.x - p2.x, 0f, tip.z - p2.z);
                float side = flat.sqrMagnitude > 0.01f ? Vector3.Dot(flat.normalized, bias) : 0f;
                float sz = h * R(0.085f, 0.145f) * (1f + 0.38f * side);   // 塊を小さめにして隙間を作る
                Vector3 c = tip + new Vector3(0f, sz * 0.25f, 0f);
                AddLeaf(leaf, rnd, c, sz, tip.y / h);
                int sat = (int)R(0f, 2.99f);
                for (int k = 0; k < sat; k++)
                {
                    Vector3 o = new Vector3(R(-1f, 1f), R(-0.4f, 0.9f), R(-1f, 1f)).normalized * sz * R(0.9f, 1.5f);
                    AddLeaf(leaf, rnd, c + o, sz * R(0.35f, 0.55f), (c.y + o.y) / h);
                }
            }
            foreach (var m in mids)
                if (R(0f, 1f) < 0.4f) AddLeaf(leaf, rnd, m + new Vector3(0f, h * 0.02f, 0f), h * R(0.045f, 0.075f), m.y / h);

            // 足元：草の束と低木を少し
            for (int k = 0; k < 9; k++)
            {
                float a = R(0f, Mathf.PI * 2f), rr = R(0.5f, 2.4f);
                Vector3 b0 = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                for (int q = 0; q < 5; q++)
                {
                    Vector3 top = b0 + new Vector3(R(-0.18f, 0.18f), R(0.35f, 0.7f), R(-0.18f, 0.18f));
                    Tube(grass, b0 + new Vector3(R(-0.05f, 0.05f), -0.02f, R(-0.05f, 0.05f)), top, 0.03f, 0.004f, 3);
                }
            }
            for (int k = 0; k < 3; k++)
            {
                float a = R(0f, Mathf.PI * 2f), rr = R(1.4f, 3.4f);
                Blob(leaf[0], new Vector3(Mathf.Cos(a) * rr, 0.2f, Mathf.Sin(a) * rr),
                     new Vector3(R(0.5f, 0.85f), R(0.32f, 0.5f), R(0.5f, 0.85f)), rnd, 5, 7, 0.3f);
            }

            FootShadow(root, h * 0.38f, Quaternion.Inverse(root.localRotation) * (Quaternion.Euler(SunElevation, -38f, 0f) * Vector3.forward));
            Emit(root, "Trunk", wood, M(Data.Hex("#765232"), "wood"));
            Emit(root, "LeafDark", leaf[0], M(Data.Hex("#3f6a2c"), "forest"));
            Emit(root, "LeafMid", leaf[1], M(Data.Hex("#4d7a34"), "forest"));
            Emit(root, "LeafLight", leaf[2], M(Data.Hex("#6c903b"), "forest"));
            Emit(root, "Grass", grass, M(Data.Hex("#8fa83a"), "leaf"));
            return root;
        }

        /// <summary>展開時：高さ・傾き・枝ぶりの違う木を、背景に少し足す（小屋と重ならない位置）。</summary>
        public static void ExtraTrees(Transform root)
        {
            Vector3[] pos = { new Vector3(21f, 0f, -12f), new Vector3(-23f, 0f, -7f), new Vector3(15f, 0f, -17f), new Vector3(-6f, 0f, -18f) };
            for (int i = 0; i < pos.Length; i++)
            {
                int sd = 100 + i * 31;
                Tree(root, pos[i], sd, 5.8f + (i % 3) * 1.1f, 2f + i * 3f, i * 70f);
            }
        }

        // ==================================================================
        //  里山
        // ==================================================================
        /// <summary>
        /// 尾根の高さ。緩やかな峰と谷を重ねて、半球が並ぶ輪郭にならないようにする。手前へ向かって下る。
        /// soft=1 で通常、小さいほど起伏がなだらか（遠い山＝明暗差を弱める）。
        /// </summary>
        static float RidgeH(float u, float tz, float height, int seed, float soft)
        {
            float edge = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI), 0.7f);
            float w = 0.55f + 0.45f * Mathf.PerlinNoise(u * 4.2f + seed * 3.1f, 0.37f)
                    + 0.25f * (Mathf.PerlinNoise(u * 9f + seed * 1.7f, 2.2f) - 0.5f);
            float wave = Mathf.Lerp(0.78f, w, soft);
            float front = Mathf.Pow(1f - tz, 1.35f);
            float rough = (Mathf.PerlinNoise(u * 14f + seed, tz * 6f + 3f) - 0.5f) * 0.16f * soft;
            return Mathf.Max(0f, height * (edge * wave * (0.12f + 0.88f * front) + rough * front));
        }

        /// <summary>
        /// 山ひとつ（旧：大きな球）。尾根のうねる山肌＋手前斜面の雑木林＋岩と土が少し。
        /// 林は「小さい塊を多め・たまに大きい塊」で、密な所と空く所をノイズで作り、等間隔のコブにならないようにする。
        /// 遠い層（layer 1）は林なし・起伏弱め・赤みのある霞色で簡略化。
        /// </summary>
        public static void Hill(Transform parent, float cx, float zc, float rx, float ry, float rz, Color col, int layer, int idx)
        {
            int seed = layer * 17 + idx * 5 + 3;
            var rnd = new System.Random(seed);
            float width = rx * 2.7f, height = ry * 1.05f, depth = rz * 1.4f;
            float z0 = zc - rz * 0.4f, y0 = -3f;
            bool far = layer >= 1;
            float soft = far ? 0.5f : 1f;
            // 夕方の霞：近い山は土ぼこり色（ばら色）、遠い山はもっと赤紫寄りに白っぽく → 明暗差が小さくなる
            Color hazeC = far ? Data.Hex("#c98f8e") : Data.Hex("#b8857c");
            float haze = far ? 0.70f : 0.30f;
            if (far) col = Data.Hex("#5f7064");

            const int nx = 34, nz = 8;
            var mb = new MeshBuilder();
            for (int j = 0; j <= nz; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float u = i / (float)nx, tz = j / (float)nz;
                    float x = cx + (u - 0.5f) * width, z = z0 + tz * depth;
                    mb.v.Add(new Vector3(x, y0 + RidgeH(u, tz, height, seed, soft), z));
                    mb.uv.Add(new Vector2(x * 0.12f, z * 0.12f));
                }
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    mb.t.Add(a); mb.t.Add(c); mb.t.Add(b);
                    mb.t.Add(b); mb.t.Add(c); mb.t.Add(d);
                }
            Emit(parent, "HillSlope", mb, M(Color.Lerp(col, hazeC, haze), "forest"));

            if (far) return;

            // 手前の斜面の雑木林：緑5色（暗い深緑〜夕日の当たる黄緑）。同じ材質を全部の山で共有→静的バッチで少ないドローコールに
            string[] greens = { "#2c5122", "#3b6529", "#4d7a34", "#62903c", "#7a9a42" };
            var cb = new MeshBuilder[5];
            for (int g = 0; g < 5; g++) cb[g] = new MeshBuilder();

            int placed = 0;
            for (int k = 0; k < 520 && placed < 120; k++)
            {
                float u = 0.05f + (float)rnd.NextDouble() * 0.9f;
                float tz = 0.04f + (float)System.Math.Pow(rnd.NextDouble(), 1.25) * 0.86f;
                // 密度ノイズ：茂る所と空く所を作る（谷は林が薄く、斜面が見える）
                float dens = Mathf.SmoothStep(0.28f, 0.78f, Mathf.PerlinNoise(u * 5.5f + seed * 1.3f, tz * 3.2f + 9.1f));
                if ((float)rnd.NextDouble() > dens) continue;
                float x = cx + (u - 0.5f) * width, z = z0 + tz * depth;
                float y = y0 + RidgeH(u, tz, height, seed, soft);
                if (y < 0.3f) continue;
                float persp = 0.7f + 0.5f * (1f - tz);
                // 小さい塊を多く、大きい塊はまれ（べき乗で偏らせる）
                float rr = (0.7f + (float)System.Math.Pow(rnd.NextDouble(), 2.2) * 3.0f) * persp;
                float tall = 0.6f + (float)rnd.NextDouble() * 0.55f;
                // 緑は近所で似つつ、塊ごとにも揺らす
                float tone = Mathf.PerlinNoise(u * 7f + 20f + seed, tz * 5f);
                int ci = Mathf.Clamp(Mathf.FloorToInt(tone * 5f + ((float)rnd.NextDouble() - 0.5f) * 2.2f), 0, 4);
                float jx = ((float)rnd.NextDouble() - 0.5f) * rr * 0.8f;
                Blob(cb[ci], new Vector3(x + jx, y + rr * tall * 0.35f, z), new Vector3(rr * 1.15f, rr * tall, rr * 0.95f), rnd, 4, 6, 0.4f);
                placed++;
            }
            // 尾根の稜線：小さく背の高い木をまばらに（稜線がなめらかな丸に見えないように）
            for (int k = 0; k < 40; k++)
            {
                float u = 0.08f + (float)rnd.NextDouble() * 0.84f;
                if (Mathf.PerlinNoise(u * 6.5f + seed * 0.7f, 5.5f) < 0.4f) continue;   // 稜線に木が無い区間も作る
                float tz = 0.02f + (float)rnd.NextDouble() * 0.12f;
                float x = cx + (u - 0.5f) * width, z = z0 + tz * depth;
                float y = y0 + RidgeH(u, tz, height, seed, soft);
                if (y < 0.3f) continue;
                float rr = 0.5f + (float)System.Math.Pow(rnd.NextDouble(), 1.8) * 1.5f;
                float tall = 0.9f + (float)rnd.NextDouble() * 0.8f;
                int ci = rnd.Next(5);
                Blob(cb[ci], new Vector3(x, y + rr * tall * 0.4f, z), new Vector3(rr, rr * tall, rr * 0.9f), rnd, 4, 6, 0.4f);
            }
            for (int g = 0; g < 5; g++)
                Emit(parent, "HillCanopy" + g, cb[g], M(Color.Lerp(Data.Hex(greens[g]), hazeC, haze), "forest"));

            // 山肌の岩と土：尾根の上のほうに、ほんの少しだけ
            var rock = new MeshBuilder();
            var soil = new MeshBuilder();
            for (int k = 0; k < 4; k++)
            {
                float u = 0.2f + (float)rnd.NextDouble() * 0.6f, tz = 0.06f + (float)rnd.NextDouble() * 0.22f;
                float x = cx + (u - 0.5f) * width, z = z0 + tz * depth;
                float y = y0 + RidgeH(u, tz, height, seed, soft);
                Blob(rock, new Vector3(x, y + 0.3f, z), new Vector3(1.8f, 1f, 1.4f), rnd, 4, 6, 0.45f);
                if (k < 2) Blob(soil, new Vector3(x + 2.2f, y - 0.2f, z + 1f), new Vector3(2.6f, 0.5f, 1.8f), rnd, 4, 6, 0.3f);
            }
            Emit(parent, "HillRock", rock, M(Color.Lerp(Data.Hex("#8a8478"), hazeC, haze), "rock"));
            Emit(parent, "HillSoil", soil, M(Color.Lerp(Data.Hex("#8a6a45"), hazeC, haze), "soil"));
        }
    }
}
