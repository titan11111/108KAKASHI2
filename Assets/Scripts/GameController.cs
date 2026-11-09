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
        List<GameObject> farmerObjs = new List<GameObject>();
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

            Vector3[] pos = { new Vector3(-2.7f, 0, 2.5f), new Vector3(0, 0, 3.5f), new Vector3(2.7f, 0, 2.5f) };
            for (int i = 0; i < Data.Farmers.Count; i++)
            {
                var fo = Env.MakeFarmer(Data.Farmers[i]);
                fo.transform.position = pos[i];
                fo.transform.rotation = Quaternion.LookRotation(new Vector3(-pos[i].x, 0, -pos[i].z));
                fo.SetActive(false);
                farmerObjs.Add(fo);
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
            cam.tYaw = 25; cam.tPitch = 10; cam.tDist = 6.5f;
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
            foreach (var f in farmerObjs) f.SetActive(false);
            scarecrow.transform.rotation = Quaternion.identity;
            HideAllPanels();
            skipGo.SetActive(false);
            bannerGo.SetActive(false);
            buildPanel.SetActive(true);
            cam.userControl = true;
            cam.tTarget = new Vector3(0, 1.25f, 0);
            cam.tYaw = 15; cam.tPitch = 12; cam.tDist = 5.4f;
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
            foreach (var f in farmerObjs) f.SetActive(true);
            scarecrow.transform.rotation = Quaternion.identity;
            cam.userControl = false;
            cam.tYaw = 38; cam.tPitch = 13; cam.tDist = 8f; cam.tTarget = new Vector3(0, 1.2f, 0.6f);

            ShowBanner("畑にさした！ 夕暮れ、動物たちが集まってくる…");
            yield return new WaitForSeconds(2.2f);

            evs = Data.Simulate(sel, score);
            var storm = new List<Ev>();
            foreach (var e in evs)
            {
                if (e.animal == "crow") yield return StartCoroutine(CrowScene(e));
                else if (e.animal == "monkey") yield return StartCoroutine(MonkeyScene(e));
                else if (e.animal == "boar") yield return StartCoroutine(ChargeScene(e, "boar", new Vector3(8, 0, 0.2f), new Vector3(1, 0, 0), Vector3.forward, 80f, "猪が突進してくる！"));
                else if (e.animal == "deer") yield return StartCoroutine(ChargeScene(e, "deer", new Vector3(0, 0, -8), new Vector3(0, 0, -1), Vector3.right, 80f, "鹿が角を構えて近づく…"));
                else storm.Add(e);
            }
            yield return StartCoroutine(StormScene(storm));

            Time.timeScale = 1f;
            skipGo.SetActive(false);
            ShowBanner("農家の3人が見に来たよ！");
            yield return new WaitForSeconds(1.6f);
            bannerGo.SetActive(false);

            results.Clear();
            foreach (var f in Data.Farmers) results.Add(Data.Judge(f, score, sel, evs));
            judgeIdx = 0;
            ShowJudge();
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

        IEnumerator Fall(Vector3 axis, float deg)
        {
            KakashiAudio.Cue("impact");
            Quaternion a = scarecrow.transform.rotation, b = Quaternion.AngleAxis(deg, axis);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                scarecrow.transform.rotation = Quaternion.Slerp(a, b, t * t);
                yield return null;
            }
            scarecrow.transform.rotation = b;
        }

        IEnumerator StandUp()
        {
            Quaternion a = scarecrow.transform.rotation;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                scarecrow.transform.rotation = Quaternion.Slerp(a, Quaternion.identity, t);
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
        IEnumerator MonkeyScene(Ev e)
        {
            ShowBanner("畑の端から猿がやってきた…");
            var m = Spawn("monkey", new Vector3(-8, 0, 3), Vector3.zero);
            yield return StartCoroutine(MoveTo(m.transform, new Vector3(-1.7f, 0, 1.2f), 4.5f, m));
            yield return new WaitForSeconds(0.6f);
            if (e.key == "monkey_bad")
            {
                yield return StartCoroutine(MoveTo(m.transform, new Vector3(-0.5f, 0, 0.7f), 6f, m));
                ShowBanner("猿が「" + e.item + "」を奪った！");
                scarecrow.SetPartVisible(e.slot, false);
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(MoveTo(m.transform, new Vector3(-10, 0, -6), 8f, m));
            }
            else
            {
                ShowBanner("猿は様子を見て、去っていった");
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

        IEnumerator StormScene(List<Ev> storm)
        {
            Ev main = storm[0];
            Ev hat = storm.Count > 1 ? storm[1] : null;
            ShowBanner("台風が来た！！ 風が唸りを上げる…");
            Color amb = RenderSettings.ambientLight;
            float t = 0;
            while (t < 2.2f)
            {
                t += Time.deltaTime;
                float k = t / 2.2f;
                Wind.Strength = Mathf.Lerp(0f, 3f, k);
                cam.shake = 0.04f * k;
                RenderSettings.ambientLight = Color.Lerp(amb, amb * 0.55f, k);
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

            t = 0;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                float k = t / 1.5f;
                Wind.Strength = Mathf.Lerp(3f, 0f, k);
                cam.shake = Mathf.Lerp(0.04f, 0f, k);
                RenderSettings.ambientLight = Color.Lerp(amb * 0.55f, amb, k);
                yield return null;
            }
            Wind.Strength = 0f; cam.shake = 0f; RenderSettings.ambientLight = amb;
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

        void ShowJudge()
        {
            HideAllPanels();
            judgePanel.SetActive(true);
            var r = results[judgeIdx];
            judgeName.text = r.f.name + "<size=30>（" + r.f.age + "）</size>";
            judgeRole.text = r.f.role;
            judgeStars.text = Stars(r.Stars);
            jD.Set(r.D); jS.Set(r.S); jV.Set(r.V);
            judgeComment.text = "「" + r.Comment.Replace("\n", "\n\n") + "」\n\n<color=#a3acc8>採点  " + r.Total + " 点</color>";
            nextLabel.text = judgeIdx < results.Count - 1 ? "つぎの農家へ" : "結果を見る";
            cam.tTarget = new Vector3(0, 1.3f, 1.6f);
            cam.tDist = 9f;
            if (judgeIdx < farmerObjs.Count) StartCoroutine(Hop(farmerObjs[judgeIdx].transform));
        }

        IEnumerator Hop(Transform t)
        {
            Vector3 p = t.position;
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime * 3f)
            {
                t.position = p + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f;
                yield return null;
            }
            t.position = p;
        }

        void NextJudge()
        {
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
