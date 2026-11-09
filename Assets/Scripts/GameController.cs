using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kakashi
{
    /// <summary>ゲーム全体の進行：つくる → さす → 評価 → 結果。シーンに置くだけで動く。</summary>
    public partial class GameController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameController>() == null)
                new GameObject("KakashiGame").AddComponent<GameController>();
        }

        // ---- ゲーム状態 ----
        Dictionary<string, string> sel;
        Scarecrow scarecrow;
        OrbitCam cam;
        Score score;
        List<Ev> evs;
        List<Result> results = new List<Result>();
        List<GameObject> farmerObjs = new List<GameObject>();      // 審査役の3人
        List<GameObject> extraObjs = new List<GameObject>();       // 見物の村人
        Vector3[] judgeSlots = new Vector3[3];
        Vector3[] extraSlots = new Vector3[4];
        Coroutine judgeCo;
        bool stormOn;
        List<Animal> animals = new List<Animal>();
        int slotIdx;
        int judgeIdx;
        bool testing;

        // ---- UI ----
        RectTransform root;
        GameObject introPanel, buildPanel, judgePanel, resultPanel, culturePanel, bannerGo, skipGo, hudGo;
        Text bannerText, synText, ptText, judgeName, judgeRole, judgeStars, judgeComment, resultText;
        UIK.Meter mD, mS, mV, jD, jS, jV;
        RectTransform ptFill, optContent;
        RectTransform[] tabRows;
        List<Image> tabImgs = new List<Image>();
        List<KeyValuePair<Opt, Image>> optImgs = new List<KeyValuePair<Opt, Image>>();
        Text nextLabel;
        Button nextBtn;

        static readonly Color INK = new Color(0.07f, 0.1f, 0.21f, 0.95f);
        static readonly Color STRAWC = new Color(0.93f, 0.73f, 0.29f, 1f);

        // =====================================================================
        void Start()
        {
            Application.targetFrameRate = 60;
            gameObject.AddComponent<KakashiAudio>();
            cam = Env.MakeCamera();
            Env.BuildField();

            sel = new Dictionary<string, string>
            {
                {"pole","branch"},{"head","straw"},{"body","straw"},{"arms","woodbar"},{"cloth","none"},
                {"hat","none"},{"face","none"},{"deco","none"},{"acc","none"}
            };
            scarecrow = new GameObject("Scarecrow").AddComponent<Scarecrow>();

            // 農家の輪（かかしを囲む定位置）：審査役3人＋見物の村人4人。集合の演出までは隠しておく
            float[] judgeAng = { -70f, 70f, 180f };
            float[] extraAng = { -115f, 115f, -150f, 150f };
            for (int i = 0; i < Data.Farmers.Count; i++)
            {
                judgeSlots[i] = RingPos(judgeAng[i]);
                var fo = Env.MakeFarmer(Data.Farmers[i]);
                SetupFarmer(fo, judgeSlots[i], i);
                farmerObjs.Add(fo);
            }
            string[] extraIds = { "hana", "gen", "ren", "hana" };
            string[] extraCols = { "#8fb86a", "#b98a5a", "#d98c4a", "#7a8fc4" };
            for (int i = 0; i < extraAng.Length; i++)
            {
                extraSlots[i] = RingPos(extraAng[i]);
                var fo = Env.MakeFarmer(new Farmer { id = extraIds[i], color = Data.Hex(extraCols[i]) });
                SetupFarmer(fo, extraSlots[i], i + 10);
                extraObjs.Add(fo);
            }

            BuildUI();
            ShowIntro();
        }

        // =====================================================================
        //  UI 組み立て
        // =====================================================================
        void BuildUI()
        {
            var cgo = new GameObject("UI");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var cs = cgo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            root = UIK.Rect(cgo.transform, "SafeArea");
            root.gameObject.AddComponent<SafeArea>();
            EnsureEventSystem();

            // HUD
            hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(root, false);
            UIK.Anchor(hudGo.GetComponent<RectTransform>(), 0, 0, 1, 1);
            var title = UIK.Txt(hudGo.transform, "title", "カカシ工房\n<size=26><color=#ecb94b>農家爆受けのカカシを作れ！</color></size>", 44, Data.Hex("#f2e8d0"), TextAnchor.UpperLeft, true);
            UIK.Anchor(title.rectTransform, 0, 0.9f, 0.62f, 1f, 30, 0, 0, 40);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var cul = UIK.Btn(hudGo.transform, "cultureBtn", "かかし文化", 30, new Color(0.07f, 0.1f, 0.21f, 0.85f), Data.Hex("#f2e8d0"), ShowCulture);
            UIK.Anchor(cul.GetComponent<RectTransform>(), 0.66f, 0.945f, 0.97f, 0.995f);
            var skip = UIK.Btn(hudGo.transform, "skipBtn", "早送り", 30, new Color(0.07f, 0.1f, 0.21f, 0.85f), Data.Hex("#f2e8d0"), ToggleSpeed);
            UIK.Anchor(skip.GetComponent<RectTransform>(), 0.66f, 0.875f, 0.97f, 0.925f);
            skipGo = skip.gameObject;
            skipGo.SetActive(false);

            Button audio = null;
            audio = UIK.Btn(hudGo.transform, "audioBtn", "音：ON", 26, INK, Data.Hex("#f2e8d0"), () =>
                audio.GetComponentInChildren<Text>().text = KakashiAudio.Toggle() ? "音：ON" : "音：OFF");
            UIK.Anchor(audio.GetComponent<RectTransform>(), .03f, .76f, .25f, .82f);

            // バナー
            var bimg = UIK.Img(root, "banner", new Color(0.07f, 0.1f, 0.21f, 0.88f));
            UIK.Anchor(bimg.rectTransform, 0.04f, 0.68f, 0.96f, 0.745f);
            bannerGo = bimg.gameObject;
            bannerText = UIK.Txt(bimg.transform, "t", "", 34, Data.Hex("#f2e8d0"), TextAnchor.MiddleCenter, true);
            UIK.Anchor(bannerText.rectTransform, 0, 0, 1, 1, 16, 6, 16, 6);
            bannerGo.SetActive(false);

            BuildBuildPanel();
            BuildShapePanel();
            BuildJudgePanel();
            BuildResultPanel();
            BuildCulturePanel();
            BuildIntroPanel();
        }

        void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var m = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            m.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        RectTransform Panel(string name, float height, Color c)
        {
            var img = UIK.Img(root, name, c);
            var r = img.rectTransform;
            r.anchorMin = new Vector2(0, 0);
            r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(0.5f, 0);
            r.sizeDelta = new Vector2(0, height);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        RectTransform Overlay(string name)
        {
            var img = UIK.Img(root, name, new Color(0.03f, 0.05f, 0.12f, 0.85f));
            UIK.Anchor(img.rectTransform, 0, 0, 1, 1);
            return img.rectTransform;
        }

        RectTransform Card(RectTransform parent, float x0, float y0, float x1, float y1)
        {
            var img = UIK.Img(parent, "card", new Color(0.11f, 0.15f, 0.28f, 1f));
            UIK.Anchor(img.rectTransform, x0, y0, x1, y1);
            return img.rectTransform;
        }

        void BuildBuildPanel()
        {
            var p = Panel("BuildPanel", 900, INK);
            buildPanel = p.gameObject;
            mD = UIK.MakeMeter(p, "防御力", Data.Hex("#6fb7e8"), 0f, 0.3333f, 16);
            mS = UIK.MakeMeter(p, "お洒落度", Data.Hex("#f29bb9"), 0.3333f, 0.6666f, 16);
            mV = UIK.MakeMeter(p, "目立ち度", Data.Hex("#f2c94c"), 0.6666f, 1f, 16);

            synText = UIK.ScrollText(p, "synergies", 26, Data.Hex("#ecb94b"));
            UIK.Band(synText.transform.parent.parent.parent as RectTransform, 118, 78, 24);

            // タブは2段（5つ＋4つ）、素材カードは2列で縦にスクロール
            tabRows = UIK.Rows(p, "tabs", 2, 8, 16);
            UIK.Band(tabRows[0].parent as RectTransform, 204, 156, 0);
            optContent = UIK.ScrollGrid(p, "opts", 2, 14, 16, 190);
            UIK.Band(optContent.parent.parent as RectTransform, 368, 344, 0);

            // タブ
            tabImgs.Clear();
            for (int i = 0; i < Data.Slots.Count; i++)
            {
                int idx = i;
                var b = UIK.Btn(tabRows[i < 5 ? 0 : 1], "tab" + i, Data.Slots[i].name, 32, Data.Hex("#1c2748"), Data.Hex("#f2e8d0"), () => { slotIdx = idx; ShowOptions(); });
                var le = b.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1; le.preferredWidth = 0;
                tabImgs.Add(b.GetComponent<Image>());
            }

            // ポイント
            ptText = UIK.Txt(p, "pt", "", 32, Data.Hex("#f2e8d0"), TextAnchor.MiddleLeft, true);
            UIK.Band(ptText.rectTransform, 722, 54, 28);
            var bar = UIK.Img(p, "ptBar", Data.Hex("#2a376a"));
            UIK.Band(bar.rectTransform, 780, 14, 28);
            var fill = UIK.Img(bar.transform, "fill", Data.Hex("#e2603b"));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            ptFill = fill.rectTransform;

            var rb = UIK.Btn(p, "rand", "おまかせ", 32, Data.Hex("#2a376a"), Data.Hex("#f2e8d0"), RandomBuild);
            UIK.Anchor(rb.GetComponent<RectTransform>(), 0.03f, 0, 0.25f, 0, 0, 28, 0, -100);
            var shape = UIK.Btn(p, "shape", "形を整える", 32, Data.Hex("#2a376a"), Data.Hex("#f2e8d0"), OpenShape);
            UIK.Anchor(shape.GetComponent<RectTransform>(), 0.27f, 0, 0.56f, 0, 0, 28, 0, -100);
            var go = UIK.Btn(p, "go", "畑にさす", 44, STRAWC, Data.Hex("#121a36"), StartTest);
            UIK.Anchor(go.GetComponent<RectTransform>(), 0.58f, 0, 0.97f, 0, 0, 28, 0, -100);
            buildPanel.SetActive(false);
        }

        void BuildJudgePanel()
        {
            var p = Panel("JudgePanel", 980, INK);
            judgePanel = p.gameObject;
            judgeName = UIK.Txt(p, "name", "", 52, Data.Hex("#f2e8d0"), TextAnchor.MiddleLeft, true);
            UIK.Band(judgeName.rectTransform, 20, 70, 30);
            judgeStars = UIK.Txt(p, "stars", "", 50, Data.Hex("#ecb94b"), TextAnchor.MiddleRight, true);
            UIK.Band(judgeStars.rectTransform, 20, 70, 30);
            judgeRole = UIK.Txt(p, "role", "", 28, Data.Hex("#a3acc8"), TextAnchor.UpperLeft);
            UIK.Band(judgeRole.rectTransform, 94, 44, 30);
            jD = UIK.MakeMeter(p, "防御力", Data.Hex("#6fb7e8"), 0f, 0.3333f, 150);
            jS = UIK.MakeMeter(p, "お洒落度", Data.Hex("#f29bb9"), 0.3333f, 0.6666f, 150);
            jV = UIK.MakeMeter(p, "目立ち度", Data.Hex("#f2c94c"), 0.6666f, 1f, 150);
            var box = UIK.Img(p, "box", new Color(0.11f, 0.15f, 0.28f, 1f));
            UIK.Band(box.rectTransform, 270, 560, 24);
            judgeComment = UIK.ScrollText(box.transform, "comment", 36, Data.Hex("#f2e8d0"));
            UIK.Anchor(judgeComment.transform.parent.parent.parent as RectTransform, 0, 0, 1, 1);
            nextBtn = UIK.Btn(p, "next", "つぎへ", 44, STRAWC, Data.Hex("#121a36"), NextJudge);
            UIK.Anchor(nextBtn.GetComponent<RectTransform>(), 0.3f, 0, 0.97f, 0, 0, 28, 0, -100);
            nextLabel = nextBtn.GetComponentInChildren<Text>();
            judgePanel.SetActive(false);
        }

        void BuildResultPanel()
        {
            var ov = Overlay("ResultPanel");
            resultPanel = ov.gameObject;
            var card = Card(ov, 0.04f, 0.05f, 0.96f, 0.95f);
            resultText = UIK.ScrollText(card, "scroll", 36, Data.Hex("#f2e8d0"));
            UIK.Anchor(resultText.transform.parent.parent.parent as RectTransform, 0, 0.14f, 1, 1, 8, 0, 8, 8);
            var cp = UIK.Btn(card, "copy", "結果をコピー", 34, Data.Hex("#2a376a"), Data.Hex("#f2e8d0"), CopyResult);
            UIK.Anchor(cp.GetComponent<RectTransform>(), 0.04f, 0.02f, 0.46f, 0.12f);
            var ag = UIK.Btn(card, "again", "もう一度つくる", 38, STRAWC, Data.Hex("#121a36"), BackToBuild);
            UIK.Anchor(ag.GetComponent<RectTransform>(), 0.5f, 0.02f, 0.96f, 0.12f);
            resultPanel.SetActive(false);
        }

        void BuildCulturePanel()
        {
            var ov = Overlay("CulturePanel");
            culturePanel = ov.gameObject;
            var card = Card(ov, 0.04f, 0.06f, 0.96f, 0.94f);
            var t = UIK.ScrollText(card, "scroll", 34, Data.Hex("#f2e8d0"));
            UIK.Anchor(t.transform.parent.parent.parent as RectTransform, 0, 0.12f, 1, 1, 8, 0, 8, 8);
            string s = "<size=46><b>かかしの役割と文化</b></size>\n\n";
            foreach (var c in Data.Culture)
                s += "<color=#ecb94b><b>" + c[0] + "</b></color>\n" + c[1] + "\n\n";
            t.text = s;
            var cl = UIK.Btn(card, "close", "とじる", 40, STRAWC, Data.Hex("#121a36"), () => culturePanel.SetActive(false));
            UIK.Anchor(cl.GetComponent<RectTransform>(), 0.2f, 0.02f, 0.8f, 0.1f);
            culturePanel.SetActive(false);
        }

        void BuildIntroPanel()
        {
            var ov = Overlay("IntroPanel");
            introPanel = ov.gameObject;
            var card = Card(ov, 0.07f, 0.10f, 0.93f, 0.90f);
            var title = UIK.Txt(card, "title", "<b>農家爆受けの\n<color=#ecb94b>カカシ</color>を作れ！</b>", 64, Data.Hex("#f2e8d0"), TextAnchor.UpperLeft, true);
            UIK.Anchor(title.rectTransform, 0, 0.60f, 1, 0.97f, 48, 0, 48, 0);
            string[] steps = {
                "木・わら・わた・草・キレ・針金で、3Dのカカシを組み立てる",
                "畑にさすと、カラス・猿・猪・鹿と台風が襲ってくる",
                "個性の違う農家3人が、守・粋・目で評価する" };
            for (int i = 0; i < 3; i++)
            {
                float top = 0.57f - i * 0.12f;
                var row = UIK.Txt(card, "step" + (i + 1), "<color=#e2603b><b>" + (i + 1) + "</b></color>   " + steps[i], 36, Data.Hex("#f2e8d0"), TextAnchor.MiddleLeft);
                UIK.Anchor(row.rectTransform, 0, top - 0.11f, 1, top, 48, 0, 48, 0);
            }
            var note = UIK.Txt(card, "note", "素材ポイントには上限があるよ。ドラッグで回して、細部まで見てね。", 28, Data.Hex("#a3acc8"), TextAnchor.MiddleLeft);
            UIK.Anchor(note.rectTransform, 0, 0.17f, 1, 0.25f, 48, 0, 48, 0);
            var st = UIK.Btn(card, "start", "工房に入る", 46, STRAWC, Data.Hex("#121a36"), BackToBuild);
            UIK.Anchor(st.GetComponent<RectTransform>(), 0.08f, 0.04f, 0.92f, 0.15f);
        }

        // =====================================================================
        //  画面遷移
        // =====================================================================
        void HideAllPanels()
        {
            introPanel.SetActive(false); buildPanel.SetActive(false); judgePanel.SetActive(false);
            resultPanel.SetActive(false); culturePanel.SetActive(false);
            if (shapePanel != null) shapePanel.SetActive(false);
            shapeMode = false;
        }

        void ShowIntro()
        {
            HideAllPanels();
            hudGo.SetActive(false);   // 説明の間は、後ろのタイトルやボタンを隠す
            introPanel.SetActive(true);
            scarecrow.Build(sel);
            cam.userControl = false;
            cam.tYaw = 25; cam.tPitch = 10; cam.tDist = 6.5f; cam.tFov = 45f; cam.autoYaw = 0f;
        }

        void BackToBuild()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            Wind.Strength = 0f;
            cam.shake = 0f;
            testing = false;
            hudGo.SetActive(true);
            foreach (var a in animals) if (a != null) Destroy(a.gameObject);
            animals.Clear();
            ResetFarmers();
            scarecrow.transform.rotation = Quaternion.identity;
            HideAllPanels();
            skipGo.SetActive(false);
            bannerGo.SetActive(false);
            buildPanel.SetActive(true);
            cam.userControl = true;
            cam.tTarget = new Vector3(0, 1.25f, 0);
            cam.tYaw = 15; cam.tPitch = 12; cam.tDist = 5.4f; cam.tFov = 45f;
            scarecrow.Build(sel);
            ShowOptions();
            Refresh();
        }

        void ShowCulture() { culturePanel.SetActive(true); }

        // =====================================================================
        //  つくる
        // =====================================================================
        static string Sg(int v) { return v >= 0 ? "+" + v : v.ToString(); }

        string OptText(Opt o, string slot)
        {
            string s = "<b>" + o.name + "</b>\n";
            if (o.id != "none")
                s += "<color=#6fb7e8>守" + Sg(o.d) + "</color> <color=#f29bb9>粋" + Sg(o.s) + "</color> <color=#f2c94c>目" + Sg(o.v) + "</color>  ●" + o.cost + "\n";
            s += "<color=#a3acc8>" + o.note + "</color>";
            var an = Data.TrigAnimals(slot, o.id);
            if (an.Count > 0) s += "\n<color=#f3a68f>" + string.Join("・", an) + "が反応</color>";
            return s;
        }

        void ShowOptions()
        {
            for (int i = optContent.childCount - 1; i >= 0; i--) Destroy(optContent.GetChild(i).gameObject);
            optImgs.Clear();
            optContent.anchoredPosition = Vector2.zero;
            var slot = Data.Slots[slotIdx];
            foreach (var o in slot.opts)
            {
                var opt = o;
                var b = UIK.Btn(optContent, "opt_" + o.id, "", 28, Data.Hex("#1c2748"), Data.Hex("#f2e8d0"), () => Pick(slot.id, opt));
                var t = b.GetComponentInChildren<Text>();
                t.text = OptText(o, slot.id);
                t.alignment = TextAnchor.UpperLeft;
                t.fontStyle = FontStyle.Normal;
                UIK.Anchor(t.rectTransform, 0, 0, 1, 1, 18, 12, 18, 12);
                optImgs.Add(new KeyValuePair<Opt, Image>(o, b.GetComponent<Image>()));
            }
            RefreshHighlight();
        }

        void Pick(string slot, Opt o)
        {
            int newCost = score.Cost - Data.Get(slot, sel[slot]).cost + o.cost;
            if (newCost > Data.Budget)
            {
                StartCoroutine(Flash("素材ポイントが足りないよ！"));
                return;
            }
            if (sel[slot] == o.id) return;
            sel[slot] = o.id;
            scarecrow.Build(sel);
            Refresh();
        }

        IEnumerator Flash(string s)
        {
            ShowBanner(s);
            yield return new WaitForSeconds(1.4f);
            if (!testing) bannerGo.SetActive(false);
        }

        void Refresh()
        {
            score = Data.Calc(sel);
            score.Stab = Mathf.Clamp(score.Stab - scarecrow.ShapePenalty, 0, 100);
            mD.Set(score.D); mS.Set(score.S); mV.Set(score.V);
            string s = "";
            foreach (var x in score.Syn) s += "<color=#ecb94b>✦ " + x + "</color>  ";
            foreach (var w in score.Warn) s += "\n<color=#f3a68f>⚠ " + w + "</color>";
            synText.text = s;
            ptText.text = "素材ポイント  <size=44>" + score.Cost + "</size> / " + Data.Budget + "    安定度 " + score.Stab;
            ptFill.anchorMax = new Vector2(Mathf.Clamp01(score.Cost / (float)Data.Budget), 1);
            RefreshHighlight();
        }

        void RefreshHighlight()
        {
            if (score == null) return;
            for (int i = 0; i < tabImgs.Count; i++)
                tabImgs[i].color = i == slotIdx ? STRAWC : Data.Hex("#1c2748");
            for (int i = 0; i < tabImgs.Count; i++)
            {
                var lt = tabImgs[i].GetComponentInChildren<Text>();
                lt.color = i == slotIdx ? Data.Hex("#121a36") : Data.Hex("#f2e8d0");
            }
            var slot = Data.Slots[slotIdx];
            foreach (var kv in optImgs)
            {
                bool on = sel[slot.id] == kv.Key.id;
                int cost = score.Cost - Data.Get(slot.id, sel[slot.id]).cost + kv.Key.cost;
                bool ok = cost <= Data.Budget;
                kv.Value.color = on ? Data.Hex("#4a5fb0") : (ok ? Data.Hex("#1c2748") : Data.Hex("#12172c"));
            }
        }

        void RandomBuild()
        {
            scarecrow.ResetShapes();
            var order = new List<Slot>(Data.Slots);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var t = order[i]; order[i] = order[j]; order[j] = t;
            }
            foreach (var sl in Data.Slots) sel[sl.id] = sl.id == "pole" || sl.id == "head" || sl.id == "body" || sl.id == "arms" ? sl.opts[0].id : "none";
            int cost = 0;
            foreach (var sl in Data.Slots) cost += Data.Get(sl.id, sel[sl.id]).cost;
            foreach (var sl in order)
            {
                var cand = new List<Opt>();
                foreach (var o in sl.opts)
                    if (cost - Data.Get(sl.id, sel[sl.id]).cost + o.cost <= Data.Budget) cand.Add(o);
                if (cand.Count == 0) continue;
                var pick = cand[Random.Range(0, cand.Count)];
                cost += pick.cost - Data.Get(sl.id, sel[sl.id]).cost;
                sel[sl.id] = pick.id;
            }
            scarecrow.Build(sel);
            Refresh();
        }

        // =====================================================================
        //  さす（テスト）
        // =====================================================================
        void ToggleSpeed()
        {
            Time.timeScale = Time.timeScale > 1.5f ? 1f : 3f;
            skipGo.GetComponentInChildren<Text>().text = Time.timeScale > 1.5f ? "通常速度へ" : "早送り";
        }

        void ShowBanner(string s)
        {
            bannerText.text = s;
            bannerGo.SetActive(true);
        }

        void StartTest()
        {
            if (testing) return;
            StartCoroutine(TestRoutine());
        }

        IEnumerator TestRoutine()
        {
            testing = true;
            HideAllPanels();
            skipGo.SetActive(true);
            scarecrow.transform.rotation = Quaternion.identity;
            cam.userControl = false;
            ShotBase();

            ShowBanner("畑にさした！ 夕暮れ、動物たちが集まってくる…");
            yield return new WaitForSeconds(2.2f);

            evs = Data.Simulate(sel, score);
            var storm = new List<Ev>();
            foreach (var e in evs)
            {
                ShotBase();
                if (e.animal == "crow") yield return StartCoroutine(CrowScene(e));
                else if (e.animal == "monkey") yield return StartCoroutine(MonkeyScene(e));
                else if (e.animal == "boar") yield return StartCoroutine(ChargeScene(e, "boar", new Vector3(8, 0, 0.2f), new Vector3(1, 0, 0), Vector3.forward, 80f, "猪が突進してくる！"));
                else if (e.animal == "deer") yield return StartCoroutine(ChargeScene(e, "deer", new Vector3(0, 0, -8), new Vector3(0, 0, -1), Vector3.right, 80f, "鹿が角を構えて近づく…"));
                else storm.Add(e);
            }
            ShotBase();
            yield return StartCoroutine(StormScene(storm));

            // 農家の人たちが、かかしの周りへぞろぞろ集まってくる
            yield return StartCoroutine(GatherScene());

            Time.timeScale = 1f;
            skipGo.SetActive(false);
            ShowBanner("さあ、ひとりずつ見てもらおう！");
            yield return new WaitForSeconds(1.2f);
            bannerGo.SetActive(false);

            results.Clear();
            foreach (var f in Data.Farmers) results.Add(Data.Judge(f, score, sel, evs));
            judgeIdx = 0;
            ShowJudge();
        }

        // =====================================================================
        //  カメラ・農家まわりの共通部品
        // =====================================================================
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static Vector3 Ring(float deg, float r)
        {
            return new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(deg * Mathf.Deg2Rad) * r);
        }
        static Vector3 RingPos(float deg) { return Ring(deg, 3.3f); }

        void SetupFarmer(GameObject fo, Vector3 slot, int seed)
        {
            fo.transform.position = slot;
            fo.transform.rotation = Quaternion.LookRotation(new Vector3(-slot.x, 0, -slot.z));
            var rig = fo.GetComponent<FarmerRig>();
            rig.lookAt = scarecrow.transform;
            rig.seed = seed * 1.7f;
            fo.SetActive(false);
        }

        void ResetFarmer(GameObject fo, Vector3 slot)
        {
            fo.SetActive(false);
            fo.transform.position = slot;
            fo.transform.rotation = Quaternion.LookRotation(new Vector3(-slot.x, 0, -slot.z));
            var rig = fo.GetComponent<FarmerRig>();
            rig.mode = FarmerRig.Mode.Idle; rig.speaking = false; rig.mood = 0.5f;
        }

        void ResetFarmers()
        {
            for (int i = 0; i < farmerObjs.Count; i++) ResetFarmer(farmerObjs[i], judgeSlots[i]);
            for (int i = 0; i < extraObjs.Count; i++) ResetFarmer(extraObjs[i], extraSlots[i]);
            stormOn = false;
            cam.autoYaw = 0f; cam.tFov = 45f;
            if (Weather.I != null) Weather.I.ResetNow();
        }

        /// <summary>画面の幅（縦画面の携帯は狭い）から、必要な半幅[m]が入るカメラ距離を出す。</summary>
        float FitDist(float baseDist, float needHalf)
        {
            var c = cam.GetComponent<Camera>();
            float th = Mathf.Tan(cam.tFov * 0.5f * Mathf.Deg2Rad);
            float d = needHalf / (th * Mathf.Max(0.3f, c.aspect));
            return Mathf.Clamp(Mathf.Max(baseDist, d), baseDist, 16f);
        }

        /// <summary>襲来テストの基本のカメラ。縦画面なら画角を広げて引く。</summary>
        void ShotBase()
        {
            var c = cam.GetComponent<Camera>();
            cam.userControl = false;
            cam.autoYaw = 0f;
            cam.tFov = c.aspect < 1f ? 60f : 45f;
            cam.tYaw = 38f; cam.tPitch = 13f;
            cam.tTarget = new Vector3(0, 1.2f, 0.6f);
            cam.tDist = FitDist(8f, 2.4f);
        }

        /// <summary>動物とカカシの両方が画面に入るよう、カメラの狙いと距離を毎フレーム合わせる（縦画面の携帯でも見切れない）。</summary>
        IEnumerator Frame(Animal a, float minDist)
        {
            var c = cam.GetComponent<Camera>();
            while (a != null && a.gameObject.activeSelf)
            {
                Vector3 s = new Vector3(0f, 1.2f, 0.2f);
                Vector3 p = a.transform.position + Vector3.up * 0.6f;
                float yr = cam.tYaw * Mathf.Deg2Rad;
                Vector3 right = new Vector3(Mathf.Cos(yr), 0f, -Mathf.Sin(yr));
                float sep = Mathf.Abs(Vector3.Dot(p - s, right));
                float th = Mathf.Tan(cam.tFov * 0.5f * Mathf.Deg2Rad);
                float need = sep * 0.5f + 1.5f;
                Vector3 mid = (s + p) * 0.5f;
                cam.tTarget = new Vector3(mid.x, Mathf.Clamp(mid.y, 1.0f, 1.6f), mid.z);
                cam.tDist = Mathf.Clamp(need / (th * Mathf.Max(0.3f, c.aspect)), minDist, 15f);
                yield return null;
            }
        }

        IEnumerator Face(Transform tr, Vector3 lookPoint)
        {
            Vector3 d = lookPoint - tr.position; d.y = 0f;
            if (d.sqrMagnitude < 0.001f) yield break;
            Quaternion from = tr.rotation, to = Quaternion.LookRotation(d);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                tr.rotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t));
                yield return null;
            }
        }

        /// <summary>かかし（原点）をつっきらないよう、横を回りこんで歩く。</summary>
        IEnumerator WalkAround(Transform tr, Vector3 to, float speed)
        {
            Vector3 a = Flat(tr.position), b = Flat(to);
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len > 0.1f)
            {
                float tt = Mathf.Clamp01(-Vector3.Dot(a, d) / (len * len));
                Vector3 closest = a + d * tt;
                if (closest.magnitude < 1.9f)
                {
                    Vector3 n = closest.magnitude > 0.05f ? closest.normalized : new Vector3(-d.z, 0f, d.x).normalized;
                    if (closest.magnitude <= 0.05f && n.x < 0f) n = -n;
                    Vector3 via = closest + n * (3.4f - closest.magnitude);
                    yield return StartCoroutine(MoveTo(tr, via, speed, null));
                }
            }
            yield return StartCoroutine(MoveTo(tr, to, speed, null));
        }

        // =====================================================================
        //  農家の集合：「集まってきたよ」→ 小屋からぞろぞろ歩いてきて、かかしを囲む
        // =====================================================================
        IEnumerator GatherScene()
        {
            ShowBanner("農家の人たちが集まってきたよ！");
            var c = cam.GetComponent<Camera>();
            cam.userControl = false;
            cam.tFov = c.aspect < 1f ? 60f : 45f;
            cam.tYaw = 25f; cam.tPitch = 17f;
            cam.tTarget = new Vector3(-1f, 1f, -0.5f);
            cam.tDist = FitDist(11f, 4.6f);
            cam.autoYaw = 5f;
            yield return new WaitForSeconds(1.0f);

            // 見物の村人と審査役を交互に並べて、順番に出発させる
            var order = new List<KeyValuePair<GameObject, Vector3>>();
            var angs = new List<float>();
            float[] judgeAng = { -70f, 70f, 180f };
            float[] extraAng = { -115f, 115f, -150f, 150f };
            for (int i = 0; i < 4; i++)
            {
                order.Add(new KeyValuePair<GameObject, Vector3>(extraObjs[i], extraSlots[i])); angs.Add(extraAng[i]);
                if (i < 3) { order.Add(new KeyValuePair<GameObject, Vector3>(farmerObjs[i], judgeSlots[i])); angs.Add(judgeAng[i]); }
            }
            var jobs = new List<IEnumerator>();
            for (int i = 0; i < order.Count; i++)
                jobs.Add(FarmerArrive(order[i].Key, angs[i], order[i].Value, i * 0.4f));
            yield return StartCoroutine(All(jobs.ToArray()));
            cam.autoYaw = 0f;
            yield return new WaitForSeconds(0.6f);
            bannerGo.SetActive(false);
        }

        IEnumerator FarmerArrive(GameObject fo, float slotAng, Vector3 slot, float delay)
        {
            var rig = fo.GetComponent<FarmerRig>();
            yield return new WaitForSeconds(delay);
            Vector3 start = new Vector3(-13f + Random.Range(-0.5f, 0.5f), 0f, -9.3f + Random.Range(0f, 1.2f));   // 小屋の戸口
            float a0 = -125f + Random.Range(-6f, 6f);
            Vector3 first = Ring(a0, 7f);
            fo.transform.position = start;
            fo.transform.rotation = Quaternion.LookRotation(first - start);
            fo.SetActive(true);
            rig.mode = FarmerRig.Mode.Walk;
            const float speed = 4.2f;
            yield return StartCoroutine(MoveTo(fo.transform, first, speed, null));
            // 外周をぐるっと回って、自分の定位置へ（かかしの手前はつっきらない）
            float delta = Mathf.DeltaAngle(a0, slotAng);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(delta) / 25f));
            for (int s = 1; s <= steps; s++)
            {
                float k = s / (float)steps;
                yield return StartCoroutine(MoveTo(fo.transform, Ring(a0 + delta * k, Mathf.Lerp(7f, 3.3f, k)), speed, null));
            }
            rig.mode = FarmerRig.Mode.Idle;
            yield return StartCoroutine(Face(fo.transform, Vector3.zero));
        }

        // ---- 動き ----
        IEnumerator MoveTo(Transform tr, Vector3 to, float speed, Animal an)
        {
            if (an != null) an.moving = true;
            while ((tr.position - to).sqrMagnitude > 0.01f)
            {
                Vector3 d = to - tr.position;
                Vector3 flat = new Vector3(d.x, 0, d.z);
                if (flat.sqrMagnitude > 0.0001f)
                    tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(flat), 10f * Time.deltaTime);
                tr.position = Vector3.MoveTowards(tr.position, to, speed * Time.deltaTime);
                yield return null;
            }
            if (an != null) an.moving = false;
        }

        IEnumerator Run(IEnumerator r, System.Action done)
        {
            yield return StartCoroutine(r);
            done();
        }

        IEnumerator All(params IEnumerator[] rs)
        {
            int left = rs.Length;
            foreach (var r in rs) StartCoroutine(Run(r, () => { left--; }));
            while (left > 0) yield return null;
        }

        Animal Spawn(string kind, Vector3 p, Vector3 faceTo)
        {
            var a = Env.MakeAnimal(kind);
            a.transform.position = p;
            Vector3 d = faceTo - p; d.y = 0;
            if (d.sqrMagnitude > 0.001f) a.transform.rotation = Quaternion.LookRotation(d);
            animals.Add(a);
            return a;
        }

        IEnumerator Wobble(float sec)
        {
            KakashiAudio.Cue("impact");
            float t = 0;
            while (t < sec)
            {
                t += Time.deltaTime;
                float a = Mathf.Sin(t * 30f) * 6f * (1f - t / sec);
                scarecrow.transform.rotation = Quaternion.Euler(0, 0, a);
                yield return null;
            }
            scarecrow.transform.rotation = Quaternion.identity;
        }

        /// <summary>倒れる時に地面との当たりを調べる対象（支柱は地面に刺さっているので除く）。</summary>
        List<Renderer> FallRenderers()
        {
            var rs = new List<Renderer>();
            Transform pole;
            scarecrow.Parts.TryGetValue("pole", out pole);
            foreach (var r in scarecrow.GetComponentsInChildren<Renderer>())
                if (r != null && r.enabled && r.gameObject.activeInHierarchy && (pole == null || !r.transform.IsChildOf(pole)))
                    rs.Add(r);
            return rs;
        }

        float LowestY(List<Renderer> rs, Quaternion a, Quaternion b, float k)
        {
            scarecrow.transform.rotation = Quaternion.Slerp(a, b, k);
            float m = float.MaxValue;
            foreach (var r in rs) if (r != null) m = Mathf.Min(m, r.bounds.min.y);
            return m;
        }

        /// <summary>地面に当たる手前までの倒れ具合（0..1）。横木や頭が地面にめり込む角度では止める。</summary>
        float FallLimit(List<Renderer> rs, Quaternion a, Quaternion b)
        {
            const float floor = 0.01f;
            float lo = 0f, hi = 1f;
            if (LowestY(rs, a, b, 1f) >= floor) { scarecrow.transform.rotation = a; return 1f; }
            for (int i = 0; i < 12; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (LowestY(rs, a, b, mid) >= floor) lo = mid; else hi = mid;
            }
            scarecrow.transform.rotation = a;
            return lo;
        }

        IEnumerator Fall(Vector3 axis, float deg)
        {
            var rs = FallRenderers();
            Quaternion a = scarecrow.transform.rotation, b = Quaternion.AngleAxis(deg, axis);
            float kMax = FallLimit(rs, a, b);                 // 地面に当たる所まで
            Quaternion end = Quaternion.Slerp(a, b, kMax);
            // ぐらっ…と傾いて、だんだん加速して倒れる
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.9f;
                float k = Mathf.Clamp01(t);
                scarecrow.transform.rotation = Quaternion.Slerp(a, end, k * k * k);
                yield return null;
            }
            KakashiAudio.Cue("impact");
            // 地面で軽く弾んで止まる（めり込まないよう、弾みは起き上がる向きだけ）
            Quaternion up = Quaternion.Slerp(a, b, kMax * 0.92f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f;
                float k = Mathf.Clamp01(t);
                scarecrow.transform.rotation = Quaternion.Slerp(end, up, Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            scarecrow.transform.rotation = end;
        }

        IEnumerator StandUp()
        {
            Quaternion a = scarecrow.transform.rotation;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.1f;
                float k = Mathf.Clamp01(t);
                scarecrow.transform.rotation = Quaternion.Slerp(a, Quaternion.identity, k * k * (3f - 2f * k));
                yield return null;
            }
            scarecrow.transform.rotation = Quaternion.identity;
        }

        // ---- カラス ----
        IEnumerator CrowFly(Animal a, Vector3 perch, bool flee)
        {
            Vector3 hover = perch + new Vector3(0, 0.5f, 1.5f);
            yield return StartCoroutine(MoveTo(a.transform, hover, 7f, a));
            if (flee)
            {
                yield return new WaitForSeconds(0.25f);
                yield return StartCoroutine(MoveTo(a.transform, new Vector3(Random.Range(-6f, 6f), 9f, -14f), 10f, a));
                a.gameObject.SetActive(false);
            }
            else
            {
                yield return StartCoroutine(MoveTo(a.transform, perch, 5f, a));
                a.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
            }
        }

        IEnumerator CrowScene(Ev e)
        {
            KakashiAudio.Cue("crow");
            ShowBanner("カラスの群れが降りてくる…");
            Vector3[] perch = { new Vector3(-0.75f, 1.78f, 0f), new Vector3(0.75f, 1.78f, 0f), new Vector3(0, 2.4f, 0f) };
            var jobs = new List<IEnumerator>();
            var crows = new List<Animal>();
            for (int i = 0; i < 3; i++)
            {
                var c = Spawn("crow", new Vector3(-9 + i * 8, 8 + i, -10), Vector3.zero);
                crows.Add(c);
                bool flee = i < e.fled;
                jobs.Add(CrowFly(c, perch[i], flee));
            }
            yield return StartCoroutine(All(jobs.ToArray()));
            if (e.key == "crow_hit")
            {
                ShowBanner("カラスが「" + e.item + "」をつついた！");
                scarecrow.SetPartVisible(e.slot, false);
            }
            else ShowBanner("カラス " + e.fled + " / 3 羽が逃げた！");
            yield return new WaitForSeconds(1.8f);
            // 居座ったカラスも夕空へ
            var leave = new List<IEnumerator>();
            foreach (var c in crows)
                if (c.gameObject.activeSelf) leave.Add(Leave(c));
            if (leave.Count > 0) yield return StartCoroutine(All(leave.ToArray()));
        }

        IEnumerator Leave(Animal a)
        {
            yield return StartCoroutine(MoveTo(a.transform, new Vector3(Random.Range(-6f, 6f), 9f, -14f), 9f, a));
            a.gameObject.SetActive(false);
        }

        // ---- 猿 ----
        /// <summary>放物線のジャンプ。腕を raiseTo 度まで伸ばしながら、体を少し反らせる。</summary>
        IEnumerator Arc(Animal a, Vector3 to, float height, float sec, float raiseTo)
        {
            Transform tr = a.transform;
            Vector3 from = tr.position;
            Vector3 d = to - from; d.y = 0f;
            Quaternion face = d.sqrMagnitude > 0.001f ? Quaternion.LookRotation(d) : tr.rotation;
            float raise0 = a.armRaise;
            a.moving = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / sec;
                float k = Mathf.Clamp01(t);
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * height;
                tr.position = p;
                tr.rotation = face * Quaternion.Euler(-Mathf.Sin(k * Mathf.PI) * 25f, 0f, 0f);
                a.armRaise = Mathf.Lerp(raise0, raiseTo, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            tr.position = to;
            tr.rotation = face;
            a.armRaise = raiseTo;
        }

        Bounds PartBounds(string slot)
        {
            Bounds b = new Bounds(new Vector3(0f, 1.4f, 0f), Vector3.one * 0.5f);
            Transform t;
            if (scarecrow.Parts.TryGetValue(slot, out t) && t != null)
            {
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                }
            }
            return b;
        }

        /// <summary>奪う物の複製を、猿の手に持たせる（元のパーツは呼び出し側で隠す）。</summary>
        void Steal(string slot, Animal m)
        {
            Transform part;
            if (!scarecrow.Parts.TryGetValue(slot, out part) || part == null || m.hand == null) return;
            var holder = new GameObject("loot").transform;
            holder.SetParent(m.hand, false);
            holder.localPosition = Vector3.zero;
            var clone = Instantiate(part.gameObject, part.position, part.rotation);
            clone.SetActive(true);
            foreach (var sw in clone.GetComponentsInChildren<Swayer>()) Destroy(sw);
            var rs = clone.GetComponentsInChildren<Renderer>();
            float s = 1f;
            Vector3 center = part.position;
            if (rs.Length > 0)
            {
                Bounds bb = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) bb.Encapsulate(rs[i].bounds);
                center = bb.center;
                float maxE = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
                s = Mathf.Min(1f, 0.45f / Mathf.Max(0.05f, maxE));
            }
            clone.transform.SetParent(holder, true);
            clone.transform.position += holder.position - center;
            holder.localScale = Vector3.one * s;
        }

        IEnumerator MonkeyScene(Ev e)
        {
            ShowBanner("畑の端から猿がやってきた…");
            var m = Spawn("monkey", new Vector3(-8, 0, 3), Vector3.zero);
            StartCoroutine(Frame(m, 5.5f));
            KakashiAudio.Cue("monkey");
            yield return StartCoroutine(MoveTo(m.transform, new Vector3(-2.8f, 0, 1.7f), 4.5f, m));
            yield return new WaitForSeconds(0.5f);
            if (e.key == "monkey_bad")
            {
                // 奪う物のすぐ手前まで、ジャンプで飛びかかる
                Bounds b = PartBounds(e.slot);
                Vector3 dir = new Vector3(-0.75f, 0f, 0.66f).normalized;
                float reach = 0.55f + Mathf.Min(0.6f, Mathf.Max(b.extents.x, b.extents.z));
                Vector3 hang = new Vector3(b.center.x, Mathf.Clamp(b.center.y - 0.85f, 0f, 2.2f), b.center.z) + dir * reach;
                ShowBanner("猿が飛びかかった！");
                KakashiAudio.Cue("monkey");
                m.transform.rotation = Quaternion.LookRotation(Flat(b.center - m.transform.position));
                yield return new WaitForSeconds(0.25f);                                   // ため
                yield return StartCoroutine(Arc(m, hang, 1.0f, 0.5f, 165f));              // ジャンプ！腕を上へ
                Steal(e.slot, m);
                scarecrow.SetPartVisible(e.slot, false);
                ShowBanner("猿が「" + e.item + "」を奪った！");
                StartCoroutine(Wobble(0.35f));
                yield return new WaitForSeconds(0.45f);
                // 戦利品を抱えて飛び降り、逃走
                Vector3 land = new Vector3(hang.x - 0.8f, 0f, hang.z + 0.5f);
                yield return StartCoroutine(Arc(m, land, 0.6f, 0.4f, 150f));
                yield return StartCoroutine(MoveTo(m.transform, new Vector3(-10, 0, -6), 8f, m));
            }
            else
            {
                ShowBanner("猿は飛びかかろうとして…ためらった");
                m.transform.rotation = Quaternion.LookRotation(Flat(new Vector3(0f, 0f, 0f) - m.transform.position));
                yield return StartCoroutine(Arc(m, new Vector3(-1.9f, 0f, 1.4f), 0.4f, 0.35f, 40f));
                yield return new WaitForSeconds(0.5f);
                ShowBanner("猿は様子を見て、去っていった");
                m.armRaise = 0f;
                yield return StartCoroutine(MoveTo(m.transform, new Vector3(-10, 0, -4), 6f, m));
            }
            m.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.6f);
        }

        // ---- 猪・鹿（突進系）----
        IEnumerator ChargeScene(Ev e, string kind, Vector3 start, Vector3 dir, Vector3 fallAxis, float fallDeg, string intro)
        {
            ShowBanner(intro);
            var a = Spawn(kind, start, Vector3.zero);
            StartCoroutine(Frame(a, 6.5f));   // 縦画面でも、動物がカカシと一緒に画面へ入る
            // dir は「カカシ側から見た動物のいる向き」
            Vector3 near = dir * 3.2f, hit = dir * 0.55f;
            yield return StartCoroutine(MoveTo(a.transform, near, 4f, a));
            Vector3 away = dir * 12f + new Vector3(0, 0, -5f);
            bool fell = false;
            if (e.key.EndsWith("_scare"))
            {
                ShowBanner(kind == "boar" ? "猪は引き返した！" : "鹿は寄りつかなかった！");
                yield return new WaitForSeconds(0.6f);
                yield return StartCoroutine(MoveTo(a.transform, away, 6f, a));
            }
            else
            {
                yield return StartCoroutine(MoveTo(a.transform, hit, 9f, a));
                if (e.key.EndsWith("_bad"))
                {
                    ShowBanner(kind == "boar" ? "猪に倒された…！" : "鹿に倒された…！");
                    // 動物のいる側と反対にたおれる
                    Vector3 ax = fallAxis;
                    float deg = fallDeg;
                    if (kind == "boar") deg = fallDeg;           // +x側から → -x へ（Z軸+）
                    else deg = fallDeg;                          // -z側から → +z へ（X軸+）
                    yield return StartCoroutine(Fall(ax, deg));
                    fell = true;
                }
                else
                {
                    ShowBanner(kind == "boar" ? "猪の突進に耐えた！" : "鹿の角突きに耐えた！");
                    yield return StartCoroutine(Wobble(0.6f));
                }
                yield return StartCoroutine(MoveTo(a.transform, away, 8f, a));
            }
            a.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            if (fell)
            {
                ShowBanner("かかしを立て直した");
                yield return StartCoroutine(StandUp());
            }
            yield return new WaitForSeconds(0.5f);
        }

        // ---- 台風 ----
        IEnumerator HatFly()
        {
            Transform h;
            if (!scarecrow.Parts.TryGetValue("hat", out h) || h == null || !h.gameObject.activeSelf) yield break;
            h.SetParent(null, true);
            Vector3 v = new Vector3(3f, 5f, -3f);
            float t = 0;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                h.position += v * Time.deltaTime;
                v.y -= 2f * Time.deltaTime;
                h.Rotate(0, 300f * Time.deltaTime, 200f * Time.deltaTime);
                yield return null;
            }
            Destroy(h.gameObject);
        }

        IEnumerator Thunderstorm()
        {
            while (stormOn)
            {
                yield return new WaitForSeconds(Random.Range(1.1f, 2.4f));
                if (!stormOn) break;
                if (Weather.I != null) Weather.I.Strike();
            }
        }

        IEnumerator StormScene(List<Ev> storm)
        {
            Ev main = storm[0];
            Ev hat = storm.Count > 1 ? storm[1] : null;
            var w = Weather.I;
            ShowBanner("日が暮れて…夜の台風が来た！！ 大雨だ！！");
            cam.tPitch = 8f;
            cam.tTarget = new Vector3(0, 1.3f, 0.5f);
            cam.tDist = FitDist(7f, 2.6f);
            stormOn = true;
            StartCoroutine(Thunderstorm());
            float t = 0;
            while (t < 3.0f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 3.0f);
                if (w != null) { w.Night = Mathf.SmoothStep(0f, 1f, k); Weather.RainAmount = k; }
                Wind.Strength = Mathf.Lerp(0f, 3f, k);
                cam.shake = 0.035f * k;
                yield return null;
            }
            if (hat != null)
            {
                ShowBanner("「" + hat.item + "」が飛んだ！");
                yield return StartCoroutine(HatFly());
            }
            bool fell = false;
            if (main.key == "storm_bad")
            {
                ShowBanner("かかしが倒れた…！");
                yield return StartCoroutine(Fall(Vector3.forward, -80f));
                fell = true;
            }
            else ShowBanner("踏ん張った！ 台風を耐え抜いた！");
            yield return new WaitForSeconds(1.4f);

            stormOn = false;
            t = 0;
            while (t < 2.5f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 2.5f);
                if (w != null) { w.Night = 1f - Mathf.SmoothStep(0f, 1f, k); Weather.RainAmount = 1f - k; }
                Wind.Strength = Mathf.Lerp(3f, 0f, k);
                cam.shake = Mathf.Lerp(0.035f, 0f, k);
                yield return null;
            }
            Wind.Strength = 0f; cam.shake = 0f;
            if (w != null) w.ResetNow();
            if (fell) yield return StartCoroutine(StandUp());
        }

        // =====================================================================
        //  評価
        // =====================================================================
        string Stars(int n)
        {
            string s = "";
            for (int i = 0; i < 5; i++) s += i < n ? "★" : "☆";
            return s;
        }

        static int VisibleLen(string s)
        {
            int n = 0; bool tag = false;
            foreach (char ch in s)
            {
                if (ch == '<') tag = true;
                else if (ch == '>' && tag) tag = false;
                else if (!tag) n++;
            }
            return n;
        }

        /// <summary>リッチテキストのタグはそのまま通して、見える文字だけ先頭から n 文字出す。</summary>
        static string Reveal(string s, int n)
        {
            var sb = new System.Text.StringBuilder();
            int shown = 0; bool tag = false;
            foreach (char ch in s)
            {
                if (ch == '<') tag = true;
                if (tag) { sb.Append(ch); if (ch == '>') tag = false; continue; }
                if (shown >= n) continue;
                sb.Append(ch); shown++;
            }
            return sb.ToString();
        }

        void ShowJudge()
        {
            HideAllPanels();
            judgePanel.SetActive(true);
            var r = results[judgeIdx];
            judgeName.text = r.f.name + "<size=30>（" + r.f.age + "）</size>";
            judgeRole.text = r.f.role;
            judgeStars.text = "";
            jD.Set(r.D); jS.Set(r.S); jV.Set(r.V);
            judgeComment.text = "";
            nextLabel.text = judgeIdx < results.Count - 1 ? "つぎの農家へ" : "結果を見る";
            if (judgeCo != null) StopCoroutine(judgeCo);
            judgeCo = StartCoroutine(JudgeRoutine(judgeIdx));
        }

        /// <summary>手前に農家（正面）、奥にカカシ。農家が歩いてきて、カカシを見ながら身振り手振りで批評する。</summary>
        IEnumerator JudgeRoutine(int i)
        {
            var fo = farmerObjs[i];
            var rig = fo.GetComponent<FarmerRig>();
            var r = results[i];
            string full = "「" + r.Comment.Replace("\n", "\n\n") + "」\n\n<color=#a3acc8>採点  " + r.Total + " 点</color>";
            rig.speaking = false;
            yield return null;   // 審査パネルが出て、カメラの表示範囲が決まるのを待つ

            var c = cam.GetComponent<Camera>();
            float a = Mathf.Max(0.3f, c.aspect);
            bool tall = a < 1.25f;
            cam.userControl = false; cam.autoYaw = 0f;
            cam.tFov = tall ? 50f : 38f;
            float th = Mathf.Tan(cam.tFov * 0.5f * Mathf.Deg2Rad);
            cam.tYaw = 4f; cam.tPitch = 4f;
            cam.tTarget = new Vector3(0f, 1.2f, 1.0f);
            cam.tDist = Mathf.Clamp((tall ? 1.4f : 2.1f) / (th * a), 4.6f, 12f);

            // 手前の立ち位置まで歩いてくる
            Vector3 front = new Vector3(tall ? -0.4f : -0.9f, 0f, 2.3f);
            rig.mode = FarmerRig.Mode.Walk;
            yield return StartCoroutine(WalkAround(fo.transform, front, 3.6f));
            // 体はカメラ寄り（正面）、頭でカカシをちらちら見ながら話す
            Vector3 toCam = Flat(cam.transform.position - front).normalized;
            Vector3 toScare = Flat(scarecrow.transform.position - front).normalized;
            Vector3 face = Vector3.Slerp(toCam, toScare, 0.18f);
            yield return StartCoroutine(Face(fo.transform, front + face));

            rig.mood = Mathf.Clamp01((r.Stars - 1) / 4f);
            rig.mode = FarmerRig.Mode.Talk;
            rig.speaking = true;
            int n = VisibleLen(full);
            for (int k = 1; k <= n; k++)
            {
                judgeComment.text = Reveal(full, k);
                yield return new WaitForSeconds(0.035f);
            }
            judgeComment.text = full;
            judgeStars.text = Stars(r.Stars);
            rig.speaking = false;
        }

        IEnumerator ReturnToSlot(int i)
        {
            var fo = farmerObjs[i];
            var rig = fo.GetComponent<FarmerRig>();
            rig.speaking = false; rig.mood = 0.5f;
            rig.mode = FarmerRig.Mode.Walk;
            yield return StartCoroutine(WalkAround(fo.transform, judgeSlots[i], 3.6f));
            rig.mode = FarmerRig.Mode.Idle;
            yield return StartCoroutine(Face(fo.transform, Vector3.zero));
        }

        void NextJudge()
        {
            if (judgeCo != null) StopCoroutine(judgeCo);
            StartCoroutine(ReturnToSlot(judgeIdx));   // しゃべり終えた農家は、輪に戻る
            judgeIdx++;
            if (judgeIdx >= results.Count) ShowResult();
            else ShowJudge();
        }

        // =====================================================================
        //  結果
        // =====================================================================
        string resultPlain = "";

        void ShowResult()
        {
            HideAllPanels();
            testing = false;
            bannerGo.SetActive(false);
            resultPanel.SetActive(true);
            int sum = 0;
            foreach (var r in results) sum += r.Total;
            int avg = Mathf.RoundToInt(sum / (float)results.Count);
            string rank = Data.RankLetter(avg);

            string s = "<size=120><color=#ecb94b><b>" + rank + "</b></color></size>  <size=44><b>" + Data.RankTitle(rank) + "</b></size>\n\n";
            s += "爆受け度（農家3人の平均）  <size=64><color=#ecb94b><b>" + avg + "</b></color></size>\n\n";
            s += "<color=#6fb7e8>防御力 " + score.D + "</color>   <color=#f29bb9>お洒落度 " + score.S + "</color>   <color=#f2c94c>目立ち度 " + score.V + "</color>\n\n";
            string plain = "【農家爆受けのカカシ】" + rank + "ランク 爆受け度" + avg + "\n";
            s += "<b>襲来の結果</b>\n";
            foreach (var e in evs)
            {
                s += "・" + Data.EvLabel(e) + "\n";
                plain += "・" + Data.EvLabel(e) + "\n";
            }
            s += "\n<b>農家の評価</b>\n";
            foreach (var r in results)
            {
                s += "<color=#ecb94b>" + r.f.name + "  " + Stars(r.Stars) + "  " + r.Total + "点</color>\n";
                string[] lines = r.Comment.Split('\n');
                s += lines[0] + "\n\n";
                plain += r.f.name + " " + Stars(r.Stars) + " " + r.Total + "点\n";
            }
            var cul = Data.Culture[Random.Range(0, Data.Culture.Count)];
            s += "<b>かかしのひとこと</b>\n<color=#ecb94b>" + cul[0] + "</color>\n" + cul[1] + "\n";
            resultText.text = s;
            resultPlain = plain;
            cam.userControl = true;
            cam.autoYaw = 0f; cam.tFov = 45f;
            cam.tTarget = new Vector3(0, 1.25f, 0);
            cam.tDist = 6.5f;
        }

        void CopyResult()
        {
            GUIUtility.systemCopyBuffer = resultPlain;
            StartCoroutine(Flash("結果をコピーしたよ！"));
        }
    }
}
