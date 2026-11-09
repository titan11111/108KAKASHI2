using System.Collections.Generic;
using UnityEngine;

namespace Kakashi
{
    public class Opt { public string id, name, mat, note; public int cost, d, s, v, noise, shine; }
    public class Slot { public string id, name; public List<Opt> opts = new List<Opt>(); }
    public class Trig { public string label, act; public string[] items; }
    public class Hit { public string slot, name; }

    public class Farmer
    {
        public string id, name, role, kanji;
        public int age, bd, bs, bv;
        public Color color;
        public float wd, ws, wv;
        public string[] like, hate;
        public string top, good, mid, low, bad;
        public string[] catHi, catLo, crow;
        public string likeFmt, hateFmt;
    }

    /// <summary>テスト中に起きる事件。key は農家コメント表のキー。</summary>
    public class Ev { public string key, animal, item, slot; public int fled; }

    public class Score
    {
        public int D, S, V, Noise, Shine, Stab, Cost;
        public List<string> Syn = new List<string>();
        public List<string> Warn = new List<string>();
    }

    public class Result
    {
        public Farmer f;
        public int D, S, V, Total, Stars;
        public string Comment;
    }

    public static class Data
    {
        public const int Budget = 18;
        public static List<Slot> Slots = new List<Slot>();
        public static Dictionary<string, Dictionary<string, Opt>> Lookup = new Dictionary<string, Dictionary<string, Opt>>();
        public static Dictionary<string, Trig> Trigs = new Dictionary<string, Trig>();
        public static List<Farmer> Farmers = new List<Farmer>();
        public static List<string[]> Culture = new List<string[]>();
        static Dictionary<string, Dictionary<string, string>> fev = new Dictionary<string, Dictionary<string, string>>();

        static Opt O(string id, string name, string mat, int cost, int d, int s, int v, string note, int n = 0, int sh = 0)
        {
            return new Opt { id = id, name = name, mat = mat, cost = cost, d = d, s = s, v = v, note = note, noise = n, shine = sh };
        }
        static Slot S(string id, string name, params Opt[] o)
        {
            var sl = new Slot { id = id, name = name };
            sl.opts.AddRange(o);
            return sl;
        }

        static Data()
        {
            Opt none = O("none", "なし", "none", 0, 0, 0, 0, "つけない");
            Slots.Add(S("pole", "支柱",
                O("branch", "細い枝の柱", "wood", 1, 4, 2, 0, "軽くて、少し曲がっている"),
                O("frame", "二本脚の木組み", "wood", 2, 9, 3, 6, "どっしり立って風に強い"),
                O("log", "丸太の太柱", "wood", 3, 14, -2, 4, "台風でも倒れない")));
            Slots.Add(S("head", "頭",
                O("straw", "わら束の頭", "straw", 1, 6, 2, 3, "昔ながらの定番"),
                O("grass", "草玉の頭", "grass", 1, 3, 4, 4, "ぼさぼさの緑の髪"),
                O("cotton", "わたの丸顔", "cotton", 2, 1, 8, 6, "ふわふわで愛嬌あり"),
                O("mask", "木彫りの頭", "wood", 3, 8, 5, 8, "彫りの深い職人仕事")));
            Slots.Add(S("body", "胴",
                O("straw", "わら詰めの胴", "straw", 2, 12, 2, 4, "ぎっしり詰めて頑丈"),
                O("grass", "草編みの胴", "grass", 1, 7, 5, 3, "草のスカート風"),
                O("cotton", "わたふわの胴", "cotton", 2, 3, 8, 8, "雲のようなシルエット"),
                O("cloth", "キレ巻きの胴", "cloth", 2, 5, 9, 7, "端切れのつぎはぎ")));
            Slots.Add(S("arms", "腕",
                O("straw", "わらの腕", "straw", 1, 8, 2, 3, "ぶらんと垂れる"),
                O("woodbar", "木の横木", "wood", 2, 10, 1, 6, "まっすぐ両手を広げる"),
                O("wire", "針金の長い腕", "wire", 2, 7, 3, 10, "風でびよんと揺れる"),
                O("hybrid", "横木と針金の指", "wire", 3, 10, 4, 11, "木の芯に針金を巻く")));
            Slots.Add(S("cloth", "服・キレ", none,
                O("indigo", "藍染の野良着", "cloth", 2, 3, 9, 3, "絣模様の渋い一枚"),
                O("patch", "つぎはぎの端切れ", "cloth", 2, 5, 8, 6, "色とりどりの継ぎ接ぎ"),
                O("red", "真っ赤な派手布", "cloth", 2, 2, 4, 14, "遠くからでも見える"),
                O("gold", "金襴の端切れ", "cloth", 3, 3, 13, 10, "祭りの幕のような光沢")));
            Slots.Add(S("hat", "帽子", none,
                O("strawhat", "麦わら帽子", "straw", 1, 3, 6, 4, "農家の王道"),
                O("tenugui", "手ぬぐい頭巾", "cloth", 1, 1, 6, 3, "後ろで結んでなびく"),
                O("kasa", "編み笠", "straw", 2, 6, 7, 5, "とがった三角の笠"),
                O("horns", "針金のとげ冠", "wire", 2, 10, 2, 10, "ぎらっと逆立つ")));
            Slots.Add(S("face", "顔", none,
                O("buttons", "木のボタン目", "wood", 1, 2, 4, 3, "ステッチの笑顔"),
                O("wireeyes", "針金のぎょろ目", "wire", 1, 4, 1, 8, "見開いた輪っかの目"),
                O("whisker", "わらのヒゲ", "straw", 2, 1, 7, 4, "立派な口ひげ"),
                O("oni", "鬼の面", "wood", 3, 15, -4, 12, "カラスが震える形相")));
            Slots.Add(S("deco", "飾り", none,
                O("pinwheel", "キレの風車", "cloth", 2, 4, 5, 12, "風でくるくる回る"),
                O("mobile", "針金のきらめき", "wire", 2, 8, 3, 10, "光る小片が揺れる"),
                O("cloak", "草のマント", "grass", 2, 4, 6, 4, "背中に草を垂らす"),
                O("bell", "針金の鈴", "wire", 1, 7, 2, 4, "チリンと鳴る首飾り"),
                O("shime", "しめ縄", "straw", 2, 6, 7, 5, "神様の依代の印")));
            Slots.Add(S("acc", "アクセント", none,
                O("naruko", "鳴子", "wood", 2, 4, 2, 6, "板と竹がカラカラ鳴る", 10),
                O("cd", "反射CDの飾り", "wire", 1, 6, 1, 9, "ぎらっと光ってカラス除け", 0, 10),
                O("chochin", "提灯", "cloth", 2, 2, 8, 10, "夜にぽっと灯る"),
                O("kuwa", "使い込んだ鍬", "wood", 2, 9, 3, 3, "手に持つ。いざとなれば…"),
                O("kago", "野菜かご", "grass", 2, 1, 9, 4, "大根が顔を出す竹かご"),
                O("issho", "お供えの一升瓶", "straw", 1, 2, 6, 3, "稲わらを巻いた酒瓶")));

            foreach (var sl in Slots)
            {
                var d = new Dictionary<string, Opt>();
                foreach (var o in sl.opts) d[o.id] = o;
                Lookup[sl.id] = d;
            }

            Trigs["crow"] = new Trig { label = "カラス", act = "つついて壊す", items = new[] { "cloth:gold", "acc:issho" } };
            Trigs["monkey"] = new Trig { label = "猿", act = "飛びかかって奪う", items = new[] { "acc:kago", "face:wireeyes", "cloth:red" } };
            Trigs["boar"] = new Trig { label = "猪", act = "迷わず突進", items = new[] { "acc:kago", "acc:issho", "deco:cloak" } };
            Trigs["deer"] = new Trig { label = "鹿", act = "角で突く", items = new[] { "head:grass", "body:grass", "deco:cloak", "acc:kago" } };

            BuildFarmers();
            BuildCulture();
            BuildFev();
        }

        public static Opt Get(string slot, string id) { return Lookup[slot][id]; }

        public static Color MatColor(string mat)
        {
            switch (mat)
            {
                case "wood": return Hex("#a8743f");
                case "straw": return Hex("#e6c254");
                case "cotton": return Hex("#f3f0e8");
                case "grass": return Hex("#78b04c");
                case "cloth": return Hex("#e0603c");
                case "wire": return Hex("#b7c0d0");
            }
            return Hex("#667788");
        }
        public static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }

        public static Color ClothColor(string id)
        {
            switch (id)
            {
                case "indigo": return Hex("#24386e");
                case "patch": return Hex("#c9553a");
                case "red": return Hex("#cf3e2c");
                case "gold": return Hex("#d4a02a");
            }
            return Hex("#e0603c");
        }

        /// <summary>この動物の「反応アイテム」を付けているか。</summary>
        public static List<Hit> Triggers(Dictionary<string, string> sel, string animal)
        {
            var res = new List<Hit>();
            foreach (var it in Trigs[animal].items)
            {
                var p = it.Split(':');
                if (sel[p[0]] == p[1]) res.Add(new Hit { slot = p[0], name = Get(p[0], p[1]).name });
            }
            return res;
        }

        /// <summary>このオプションに反応する動物名（UI警告用）。</summary>
        public static List<string> TrigAnimals(string slot, string id)
        {
            var a = new List<string>();
            foreach (var kv in Trigs)
                foreach (var it in kv.Value.items)
                    if (it == slot + ":" + id) { a.Add(kv.Value.label); break; }
            return a;
        }

        public static Score Calc(Dictionary<string, string> s)
        {
            var r = new Score();
            int d = 0, sy = 0, v = 0, cost = 0, nz = 0, sh = 0, kinds = 0;
            var counts = new Dictionary<string, int>();
            foreach (var sl in Slots)
            {
                var o = Get(sl.id, s[sl.id]);
                d += o.d; sy += o.s; v += o.v; cost += o.cost; nz += o.noise; sh += o.shine;
                if (o.mat != "none")
                {
                    if (!counts.ContainsKey(o.mat)) { counts[o.mat] = 0; kinds++; }
                    counts[o.mat]++;
                }
            }
            if (kinds == 6) { d += 6; sy += 6; v += 6; r.Syn.Add("六素材そろい踏み 全+6"); }
            int mx = 0; foreach (var kv in counts) mx = Mathf.Max(mx, kv.Value);
            if (mx >= 5) { sy += 8; v -= 4; r.Syn.Add("渋い統一感 お洒落+8"); }
            if (s["deco"] == "shime" && (s["hat"] == "strawhat" || s["hat"] == "kasa")) { d += 4; sy += 4; r.Syn.Add("田の神の依代 守+4 粋+4"); }
            if (s["deco"] == "mobile" && s["face"] == "wireeyes") { v += 6; r.Syn.Add("キラキラ作戦 目+6"); }
            if (s["face"] == "oni" && s["deco"] == "shime") { d += 8; r.Syn.Add("鬼門ガード 守+8"); }
            if (s["arms"] == "hybrid" && s["cloth"] == "red") { v += 5; r.Syn.Add("はためく旗 目+5"); }
            if (s["acc"] == "issho" && s["deco"] == "shime") { d += 3; sy += 4; r.Syn.Add("お供えセット 守+3 粋+4"); }
            if (s["acc"] == "naruko" && s["deco"] == "bell") { nz += 8; r.Syn.Add("騒がしコンビ 音+8"); }
            if (s["acc"] == "chochin" && s["hat"] == "kasa") { sy += 5; r.Syn.Add("旅の行灯 粋+5"); }
            if (s["acc"] == "kuwa" && s["hat"] == "strawhat") { d += 3; sy += 3; r.Syn.Add("野良着の相棒 守+3 粋+3"); }
            if (s["acc"] == "cd" && s["cloth"] == "red") { v += 4; r.Syn.Add("ギラ赤作戦 目+4"); }
            if (s["deco"] == "bell") nz += 6;

            int poleBase = s["pole"] == "log" ? 72 : (s["pole"] == "frame" ? 55 : 30);
            r.Stab = Mathf.RoundToInt(poleBase + d * 0.25f);
            r.D = Mathf.Clamp(Mathf.RoundToInt(d * 1.3f), 0, 100);
            r.S = Mathf.Clamp(Mathf.RoundToInt(sy * 1.3f), 0, 100);
            r.V = Mathf.Clamp(Mathf.RoundToInt(v * 1.3f), 0, 100);
            r.Noise = nz; r.Shine = sh; r.Cost = cost;
            foreach (var kv in Trigs)
            {
                var hits = Triggers(s, kv.Key);
                if (hits.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var h in hits) names.Add(h.name);
                    r.Warn.Add(kv.Value.label + "が反応：" + string.Join("・", names));
                }
            }
            return r;
        }

        /// <summary>動物たちの襲来結果を決める（決定論）。</summary>
        public static List<Ev> Simulate(Dictionary<string, string> s, Score sc)
        {
            var evs = new List<Ev>();

            // カラス
            var ct = Triggers(s, "crow");
            if (ct.Count > 0)
                evs.Add(new Ev { animal = "crow", key = "crow_hit", item = ct[0].name, slot = ct[0].slot, fled = 0 });
            else
            {
                float power = sc.D * 0.5f + sc.V * 0.3f + sc.Noise + sc.Shine;
                int fled = Mathf.Clamp(Mathf.FloorToInt((power - 20f) / 15f), 0, 3);
                evs.Add(new Ev { animal = "crow", key = "crow", fled = fled });
            }

            // 猿
            var mt = Triggers(s, "monkey");
            if (mt.Count > 0)
                evs.Add(new Ev { animal = "monkey", key = "monkey_bad", item = mt[0].name, slot = mt[0].slot });
            else if (sc.D >= 30 && sc.V >= 25)
                evs.Add(new Ev { animal = "monkey", key = "monkey_ok" });
            else
            {
                string slot = s["acc"] != "none" ? "acc" : (s["deco"] != "none" ? "deco" : (s["hat"] != "none" ? "hat" : null));
                if (slot == null) evs.Add(new Ev { animal = "monkey", key = "monkey_ok" });
                else evs.Add(new Ev { animal = "monkey", key = "monkey_bad", slot = slot, item = Get(slot, s[slot]).name });
            }

            // 猪
            var bt = Triggers(s, "boar");
            string bk;
            if (bt.Count > 0) bk = sc.Stab >= 70 ? "boar_hold" : "boar_bad";
            else if (sc.D >= 45) bk = "boar_scare";
            else bk = sc.Stab >= 60 ? "boar_hold" : "boar_bad";
            evs.Add(new Ev { animal = "boar", key = bk });

            // 鹿
            var dt = Triggers(s, "deer");
            string dk;
            if (dt.Count > 0) dk = sc.Stab >= 65 ? "deer_hold" : "deer_bad";
            else if (sc.D >= 40) dk = "deer_scare";
            else dk = sc.Stab >= 55 ? "deer_hold" : "deer_bad";
            evs.Add(new Ev { animal = "deer", key = dk });

            // 台風
            evs.Add(new Ev { animal = "storm", key = sc.Stab >= 60 ? "storm_ok" : "storm_bad" });
            if (s["hat"] != "none" && sc.Stab < 80)
                evs.Add(new Ev { animal = "storm", key = "storm_hat", slot = "hat", item = Get("hat", s["hat"]).name });
            return evs;
        }

        public static string EvLabel(Ev e)
        {
            switch (e.key)
            {
                case "crow": return "カラス" + e.fled + "/3羽が逃げた";
                case "crow_hit": return "カラスに「" + e.item + "」をつつかれた";
                case "monkey_ok": return "猿は寄りつかなかった";
                case "monkey_bad": return "猿に「" + e.item + "」を奪われた";
                case "boar_scare": return "猪は引き返した";
                case "boar_hold": return "猪の突進に耐えた";
                case "boar_bad": return "猪に倒された";
                case "deer_scare": return "鹿は寄りつかなかった";
                case "deer_hold": return "鹿の角突きに耐えた";
                case "deer_bad": return "鹿に倒された";
                case "storm_ok": return "台風を耐え抜いた";
                case "storm_bad": return "台風で倒れた";
                case "storm_hat": return "台風で「" + e.item + "」が飛んだ";
            }
            return e.key;
        }

        static int EvAdj(Ev e)
        {
            switch (e.key)
            {
                case "crow": return e.fled * 2 - (3 - e.fled);
                case "crow_hit": return -6;
                case "monkey_ok": case "boar_scare": case "boar_hold": case "deer_scare": case "deer_hold": case "storm_ok": return 3;
                case "monkey_bad": return -6;
                case "boar_bad": case "deer_bad": return -8;
                case "storm_bad": return -12;
                case "storm_hat": return -3;
            }
            return 0;
        }

        public static Result Judge(Farmer f, Score sc, Dictionary<string, string> sel, List<Ev> evs)
        {
            var r = new Result { f = f };
            r.D = Mathf.Clamp(sc.D + f.bd, 0, 100);
            r.S = Mathf.Clamp(sc.S + f.bs, 0, 100);
            r.V = Mathf.Clamp(sc.V + f.bv, 0, 100);
            float total = f.wd * r.D + f.ws * r.S + f.wv * r.V;

            string likeName = null, hateName = null;
            foreach (var sl in Slots)
            {
                var o = Get(sl.id, sel[sl.id]);
                if (o.mat == "none") continue;
                if (System.Array.IndexOf(f.like, o.mat) >= 0) { total += 2.5f; if (likeName == null) likeName = o.name; }
                if (System.Array.IndexOf(f.hate, o.mat) >= 0) { total -= 3.5f; if (hateName == null) hateName = o.name; }
            }
            foreach (var e in evs) total += EvAdj(e);
            r.Total = Mathf.Clamp(Mathf.RoundToInt(total), 0, 100);
            r.Stars = Mathf.Clamp(Mathf.RoundToInt(r.Total / 20f), 1, 5);

            var lines = new List<string>();
            lines.Add(r.Total >= 80 ? f.top : r.Total >= 65 ? f.good : r.Total >= 50 ? f.mid : r.Total >= 35 ? f.low : f.bad);
            // 一番重視する項目の一言
            int ci = f.wd >= f.ws && f.wd >= f.wv ? 0 : (f.ws >= f.wv ? 1 : 2);
            int cv = ci == 0 ? r.D : (ci == 1 ? r.S : r.V);
            lines.Add(cv >= 55 ? f.catHi[ci] : f.catLo[ci]);
            if (likeName != null) lines.Add(string.Format(f.likeFmt, likeName));
            else if (hateName != null) lines.Add(string.Format(f.hateFmt, hateName));
            // 事件コメント（悪い出来事を優先して最大2つ）
            int added = 0;
            foreach (int pass in new[] { 0, 1 })
            {
                foreach (var e in evs)
                {
                    if (added >= 2) break;
                    bool bad = EvAdj(e) < 0;
                    if ((pass == 0) != bad) continue;
                    string txt;
                    if (e.key == "crow") continue;
                    if (fev[f.id].TryGetValue(e.key, out txt)) { lines.Add(txt.Replace("{item}", e.item ?? "")); added++; }
                }
            }
            foreach (var e in evs) if (e.key == "crow") { lines.Add(f.crow[Mathf.Clamp(e.fled, 0, 3)]); break; }
            r.Comment = string.Join("\n", lines);
            return r;
        }

        public static string RankLetter(int avg) { return avg >= 85 ? "S" : avg >= 72 ? "A" : avg >= 58 ? "B" : avg >= 42 ? "C" : "D"; }
        public static string RankTitle(string r)
        {
            switch (r)
            {
                case "S": return "田の神が降臨した案山子";
                case "A": return "集落の語り草になる案山子";
                case "B": return "畑を守る立派な案山子";
                case "C": return "もうひと工夫ほしい案山子";
            }
            return "ただの落とし物";
        }

        static void BuildFarmers()
        {
            Farmers.Add(new Farmer
            {
                id = "gen", name = "源蔵じい", age = 82, role = "米づくり六十年・集落の長老", kanji = "源", color = Hex("#c9b27a"),
                wd = .5f, ws = .15f, wv = .35f, bd = 6, bs = -6, bv = -4, like = new[] { "straw", "wood" }, hate = new[] { "wire" },
                top = "こりゃたまげた！ 田の神様がお降りになったかと思うたわい。", good = "うむ、ええ案山子じゃ。これなら安心して昼寝ができるのう。",
                mid = "ふむ…悪うはないが、もうひと手間ほしいところじゃな。", low = "こりゃ心もとないのう。カラスが弁当を食いに来るぞい。", bad = "…案山子というより、ただの落とし物じゃな。",
                catHi = new[] { "守りの固さは申し分ない。", "品もある。", "遠くからでも目に入るのう。" },
                catLo = new[] { "守りが弱い。風が吹いたら倒れそうじゃ。", "見た目も、もう少し整えなされ。", "地味すぎて鳥に気づかれんぞ。" },
                likeFmt = "「{0}」を使うとるのが分かっとる。昔ながらでええのう。", hateFmt = "じゃが「{0}」は雨で錆びて困るんじゃ。",
                crow = new[] { "カラスは一羽も逃げんかった…。", "一羽だけ逃げたのう。", "二羽も追い払ったか、見事じゃ。", "三羽とも逃げた！ お見事！" }
            });
            Farmers.Add(new Farmer
            {
                id = "hana", name = "花さん", age = 46, role = "直売所の人気トマト農家", kanji = "花", color = Hex("#f29bb9"),
                wd = .15f, ws = .55f, wv = .3f, bd = -4, bs = 4, bv = 0, like = new[] { "cloth", "cotton", "grass" }, hate = new[] { "straw" },
                top = "やだ、最高！ 直売所の看板にしたいくらいよ！", good = "いいじゃない。畑がぱっと明るくなったわ。",
                mid = "悪くないけど、もうひと味ほしいわね。", low = "うーん、ちょっと地味ね。お客さんも素通りよ。", bad = "ごめんね、これは畑の景観に響くわ…。",
                catHi = new[] { "守りもしっかりしてるわね。", "配色が素敵。私の直売所に飾りたい！", "ひと目で目を引くわね。" },
                catLo = new[] { "おしゃれでも風で飛んだら意味ないわよ。", "色のまとまりがほしいわ。", "もう少し存在感を出して。" },
                likeFmt = "「{0}」のセンス、わかってるじゃない。", hateFmt = "「{0}」はちょっと野暮ったいかしら。",
                crow = new[] { "カラス、全然平気な顔してたわね。", "一羽は逃げたけど…。", "二羽も逃げたわ、やるじゃない。", "カラスが全員逃げたわ、すごい！" }
            });
            Farmers.Add(new Farmer
            {
                id = "ren", name = "蓮くん", age = 24, role = "SNSで人気の若手農家", kanji = "蓮", color = Hex("#6fb7e8"),
                wd = .15f, ws = .3f, wv = .55f, bd = -2, bs = 0, bv = 6, like = new[] { "wire", "cloth" }, hate = new[] { "grass" },
                top = "うおーバズる！！ これ絶対伸びるやつっす！！", good = "いいっすね！ 写真映えするっす！",
                mid = "うーん、悪くないけどあと一押しほしいっす。", low = "ちょっとインパクト足りないっす…。", bad = "これはスルーされるやつっす…ごめんなさい！",
                catHi = new[] { "守りも固いって最強っす。", "おしゃれ度高いっす！", "インパクト抜群っす！" },
                catLo = new[] { "ふわふわすぎて守れなそうっす。", "もう少し洗練されるといいっす。", "目立たないと埋もれるっす。" },
                likeFmt = "「{0}」、映えるっすね！", hateFmt = "「{0}」は地味でスワイプされるっす。",
                crow = new[] { "カラスに無視されたっす…", "一羽逃げたっす！", "二羽も逃げた！ やばっ！", "カラス全員逃げたっす、神っす！" }
            });
        }

        static void BuildCulture()
        {
            Culture.Add(new[] { "名前の由来は「嗅がし」", "かかしは「嗅がし」から来たという説があります。焼いた獣の肉や髪を竿に吊るし、その臭いで鳥獣を寄せつけなかったとされます。" });
            Culture.Add(new[] { "田の神の依代", "春に山の神が田の神として里に降り、秋に山へ帰ると考える土地があります。かかしを、その田の神が宿る依代として拝む地域も各地にあります。" });
            Culture.Add(new[] { "古事記の「久延毘古」", "古事記には、山田のそほど（かかし）の神・久延毘古が登場します。歩けないのに、天下のことをすべて知っている神とされます。" });
            Culture.Add(new[] { "十日夜（とおかんや）", "旧暦10月10日、主に東日本で行われる収穫の行事です。かかしに餅を供えて感謝し、子どもたちが藁鉄砲で地面を叩く風習が知られています。" });
            Culture.Add(new[] { "実は「慣れ」との戦い", "鳥は同じ見た目にすぐ慣れます。農家は服を替えたり、立てる位置を動かしたり、キラキラしたものを足したりして、新鮮さを保ちます。" });
            Culture.Add(new[] { "今は地域の名物", "各地で「かかし祭り」や「かかしコンテスト」が開かれています。かかしは、畑の守り手であり、集落の顔でもあります。" });
        }

        static Dictionary<string, string> D(params string[] kv)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }

        static void BuildFev()
        {
            fev["gen"] = D(
                "monkey_ok", "猿めを寄せつけんとは見事じゃ。", "monkey_bad", "猿に{item}を持っていかれたか。猿は賢いからのう。",
                "boar_scare", "猪が鼻を鳴らして引き返したわい。", "boar_hold", "猪の突進に耐えたか。根性があるのう。", "boar_bad", "猪に倒されては畑が荒らされるぞい。",
                "storm_ok", "台風を耐え抜いたか。頼もしい。", "storm_bad", "台風で倒れては案山子の名折れじゃ。", "storm_hat", "帽子が飛んだのは惜しいのう。",
                "crow_hit", "カラスに{item}をつつかれたか。光り物やお供えは狙われるぞい。",
                "deer_scare", "鹿も寄りつかんかったのう。", "deer_hold", "鹿の角突きにも耐えたか。", "deer_bad", "鹿に倒されるとは。草の匂いに釣られたかのう。");
            fev["hana"] = D(
                "monkey_ok", "猿も寄ってこなかったわ、すごい。", "monkey_bad", "{item}を猿に取られたのはショックだわ…。",
                "boar_scare", "猪が逃げていった！ トマトが守られたわ。", "boar_hold", "猪にぶつかられても立ってた。かっこいい。", "boar_bad", "猪に倒されるなんて、畑が心配になるわ。",
                "storm_ok", "あの台風で無事なんて、頼りになるわね。", "storm_bad", "台風で倒れちゃったのは残念。", "storm_hat", "帽子が飛んじゃったのは惜しいわね。",
                "crow_hit", "{item}をカラスにつつかれちゃったのね。",
                "deer_scare", "鹿も近寄らなかったわ、すごい。", "deer_hold", "鹿に角で突かれても立ってたわ。", "deer_bad", "鹿に倒されちゃったのは悲しいわ。");
            fev["ren"] = D(
                "monkey_ok", "猿が近づかなかったの、強いっす！", "monkey_bad", "{item}を猿に持っていかれたのはもったいないっす。",
                "boar_scare", "猪が引き返したの、神っす！", "boar_hold", "猪の突進に耐えたの、動画に撮りたいっす。", "boar_bad", "猪に倒されたの、バズらないやつっす…。",
                "storm_ok", "台風でも立ってるのは映えるっす！", "storm_bad", "台風で倒れたのは悔しいっす。", "storm_hat", "帽子が飛ぶ瞬間、撮れ高あったっす。",
                "crow_hit", "{item}がカラスの標的だったっすね…。",
                "deer_scare", "鹿もスルーしてたっす！", "deer_hold", "鹿の角突きに耐えたの、撮れ高あったっす。", "deer_bad", "鹿に倒されたのは悔しいっす。");
        }
    }
}
