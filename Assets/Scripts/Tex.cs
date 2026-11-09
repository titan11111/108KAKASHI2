using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    /// <summary>
    /// 画像ファイルなしで質感を作る。すべてタイル可能（つなぎ目が出ない）。
    /// 灰色の「陰影テクスチャ」は色と掛け算して使い、柄ものはフルカラーで使う。
    /// </summary>
    public static class Tex
    {
        static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        const int N = 128;

        static HashSet<string> photoKinds = new HashSet<string>();

        public static bool IsPattern(string kind)
        {
            // 写真(Resources/Textures/<名前>)に差し替えた質感は、色を掛けずにそのまま使う
            return kind == "indigo" || kind == "redcloth" || kind == "goldcloth" || kind == "patch" || photoKinds.Contains(kind);
        }

        public static Vector2 Scale(string kind)
        {
            switch (kind)
            {
                case "soil": return new Vector2(70, 70);
                case "rock": return new Vector2(1, 1);
                case "forest": return new Vector2(5, 2);
                case "leaf": return new Vector2(1, 1);
                case "indigo": case "redcloth": case "goldcloth": return new Vector2(2, 2);
                case "patch": return new Vector2(1, 1);
                case "cotton": return new Vector2(2, 2);
            }
            return new Vector2(2, 2);
        }

        public static Texture2D Get(string kind)
        {
            Texture2D t;
            if (cache.TryGetValue(kind, out t) && t != null) return t;
            // Assets/Resources/Textures/<kind>.png (.jpg) があれば、それを使う
            var photo = Resources.Load<Texture2D>("Textures/" + kind);
            if (photo != null)
            {
                photo.wrapMode = TextureWrapMode.Repeat;
                photoKinds.Add(kind);
                cache[kind] = photo;
                return photo;
            }
            t = Make(kind);
            cache[kind] = t;
            return t;
        }

        // ---------- タイル可能なノイズ ----------
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        /// <summary>x,y は格子座標。px,py の周期でラップするので画像がタイルする。</summary>
        static float VN(float x, float y, int px, int py, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = ((x0 % px) + px) % px, xb = (xa + 1) % px;
            int ya = ((y0 % py) + py) % py, yb = (ya + 1) % py;
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fbm(float u, float v, int per, int seed)
        {
            return VN(u * per, v * per, per, per, seed) * 0.5f
                 + VN(u * per * 2, v * per * 2, per * 2, per * 2, seed + 1) * 0.3f
                 + VN(u * per * 4, v * per * 4, per * 4, per * 4, seed + 2) * 0.2f;
        }

        /// <summary>つなぎ目のないセル模様。f1=最寄りの点まで、f2=2番目まで。</summary>
        static void Worley(float u, float v, int per, int seed, out float f1, out float f2)
        {
            float x = u * per, y = v * per;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = 9f; f2 = 9f;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = xi + dx, cy = yi + dy;
                    int wx = ((cx % per) + per) % per, wy = ((cy % per) + per) % per;
                    float px = cx + Hash(wx, wy, seed), py = cy + Hash(wx, wy, seed + 7);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; }
                    else if (d < f2) f2 = d;
                }
        }

        static Color Gray(float g) { g = Mathf.Clamp01(g); return new Color(g, g, g, 1f); }

        // ---------- 生成 ----------
        static Texture2D Make(string kind)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
            var px = new Color[N * N];
            var rnd = new System.Random(kind.GetHashCode() & 0xffff);
            Color[] patchCols = { Data.Hex("#c9553a"), Data.Hex("#e0b54a"), Data.Hex("#3f7a58"), Data.Hex("#e8dcc0"), Data.Hex("#2f4d8a"), Data.Hex("#8a3d5c") };
            int[] patchIdx = new int[16];
            for (int i = 0; i < 16; i++) patchIdx[i] = rnd.Next(patchCols.Length);

            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    Color c;
                    switch (kind)
                    {
                        case "wood":
                            {
                                float g = VN(u * 24, v * 3, 24, 3, 5) * 0.55f + VN(u * 48, v * 6, 48, 6, 6) * 0.45f;
                                float line = Mathf.Abs(Mathf.Sin((u * 12f + VN(u * 4, v * 2, 4, 2, 9) * 1.5f) * Mathf.PI * 2f));
                                c = Gray(0.62f + 0.3f * g - 0.12f * (1f - line) * (1f - line));
                                break;
                            }
                        case "straw":
                            {
                                float g = VN(u * 48, v * 3, 48, 3, 11) * 0.5f + VN(u * 96, v * 6, 96, 6, 12) * 0.5f;
                                c = Gray(0.58f + 0.42f * g);
                                break;
                            }
                        case "cotton":
                            c = Gray(0.78f + 0.22f * Fbm(u, v, 8, 21));
                            break;
                        case "leaf":
                            {
                                float vein = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f * 6f));
                                c = Gray(0.62f + 0.3f * Fbm(u, v, 6, 31) - 0.1f * (1f - vein) * (1f - vein));
                                break;
                            }
                        case "fur":
                            c = Gray(0.6f + 0.4f * (VN(u * 40, v * 40, 40, 40, 41) * 0.6f + Fbm(u, v, 6, 42) * 0.4f));
                            break;
                        case "soil":
                            c = Gray(0.55f + 0.45f * Fbm(u, v, 8, 51));
                            break;
                        case "rock":
                            {
                                float f1, f2;
                                Worley(u, v, 5, 61, out f1, out f2);
                                float crack = Mathf.Clamp01(1f - (f2 - f1) * 7f);          // 割れ目
                                float grain = Fbm(u, v, 12, 62);                            // ざらつき
                                float patch = Fbm(u, v, 3, 63);                             // 大きなむら
                                c = Gray(0.46f + 0.28f * grain + 0.22f * patch - 0.34f * crack * crack);
                                break;
                            }
                        case "forest":
                            {
                                float canopy = VN(u * 14, v * 14, 14, 14, 71) * 0.5f + VN(u * 28, v * 28, 28, 28, 72) * 0.3f + VN(u * 56, v * 56, 56, 56, 73) * 0.2f;
                                float clump = Fbm(u, v, 4, 74);
                                c = Gray(0.30f + 0.55f * canopy * (0.6f + 0.6f * clump));
                                break;
                            }
                        case "weave":
                            c = Gray(0.72f + 0.28f * (Mathf.Sin(u * Mathf.PI * 2f * 32f) * Mathf.Sin(v * Mathf.PI * 2f * 32f) * 0.5f + 0.5f));
                            break;
                        case "indigo":
                            {
                                float weave = 0.9f + 0.1f * Mathf.Sin(u * Mathf.PI * 2f * 64f);
                                c = new Color(0.14f, 0.22f, 0.43f, 1f) * weave;
                                int cx = x % 16, cy = y % 16;
                                bool hLine = cy == 8 && cx >= 5 && cx <= 11;
                                bool vLine = cx == 8 && cy >= 5 && cy <= 11;
                                if (hLine || vLine) c = new Color(0.79f, 0.83f, 0.94f, 1f);
                                c.a = 1f;
                                break;
                            }
                        case "redcloth":
                            {
                                float weave = 0.92f + 0.08f * Mathf.Sin(v * Mathf.PI * 2f * 64f);
                                c = new Color(0.81f, 0.24f, 0.17f, 1f) * weave;
                                int cx = (x + ((y / 32) % 2) * 16) % 32 - 16, cy = y % 32 - 16;
                                if (cx * cx + cy * cy < 36) c = new Color(0.96f, 0.83f, 0.48f, 1f);
                                c.a = 1f;
                                break;
                            }
                        case "goldcloth":
                            {
                                c = new Color(0.72f, 0.54f, 0.16f, 1f);
                                int row = y / 20, cxs = (x + (row % 2) * 16) % 32 - 16, cys = y % 20;
                                float d = Mathf.Sqrt(cxs * cxs + cys * cys);
                                if (cys < 20 && (d % 5f) < 1.6f && d < 15f) c = new Color(0.95f, 0.81f, 0.42f, 1f);
                                c.a = 1f;
                                break;
                            }
                        default: // patch
                            {
                                int cx = x / 32, cy = y / 32;
                                c = patchCols[patchIdx[cy * 4 + cx]];
                                int lx = x % 32, ly = y % 32;
                                if ((lx == 3 || lx == 28 || ly == 3 || ly == 28) && ((lx + ly) % 6 < 3)) c = new Color(1f, 1f, 1f, 1f) * 0.9f;
                                c.a = 1f;
                                break;
                            }
                    }
                    px[y * N + x] = c;
                }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 4;
            t.Apply(true);
            return t;
        }
    }
}
