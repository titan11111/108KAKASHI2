using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kakashi
{
    /// <summary>コードだけでuGUIを組むための小さな道具箱。</summary>
    public static class UIK
    {
        static Font font;

        /// <summary>
        /// 日本語フォント。Resources/JpFont.otf があれば最優先（WebGLではこれが必須）。
        /// なければOSのフォント（iOS/Mac/Windows）を使う。
        /// </summary>
        public static Font F
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("JpFont");
                    if (font == null)
                        font = Font.CreateDynamicFontFromOSFont(new[]
                        {
                            "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Yu Gothic UI", "Yu Gothic", "Meiryo",
                            "Noto Sans CJK JP", "Noto Sans JP", "Arial"
                        }, 40);
                }
                return font;
            }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        /// <summary>アンカー(0-1)と内側の余白(左・下・右・上)で配置。</summary>
        public static RectTransform Anchor(RectTransform r, float x0, float y0, float x1, float y1, float l = 0, float b = 0, float rr = 0, float t = 0)
        {
            r.anchorMin = new Vector2(x0, y0);
            r.anchorMax = new Vector2(x1, y1);
            r.offsetMin = new Vector2(l, b);
            r.offsetMax = new Vector2(-rr, -t);
            return r;
        }

        /// <summary>親の上端から top px の位置に、高さ height の横いっぱい帯を置く。</summary>
        public static RectTransform Band(RectTransform r, float top, float height, float pad = 0)
        {
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(pad, -(top + height));
            r.offsetMax = new Vector2(-pad, -top);
            return r;
        }

        public static Image Img(Transform parent, string name, Color c)
        {
            var r = Rect(parent, name);
            var i = r.gameObject.AddComponent<Image>();
            i.color = c;
            return i;
        }

        public static Text Txt(Transform parent, string name, string s, int size, Color c, TextAnchor al = TextAnchor.UpperLeft, bool bold = false)
        {
            var r = Rect(parent, name);
            var t = r.gameObject.AddComponent<Text>();
            t.font = F;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = al;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string name, string label, int size, Color bg, Color fg, UnityAction onClick)
        {
            var img = Img(parent, name, bg);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = cb;
            var t = Txt(img.transform, "label", label, size, fg, TextAnchor.MiddleCenter, true);
            Anchor(t.rectTransform, 0, 0, 1, 1, 8, 4, 8, 4);
            b.onClick.AddListener(() => KakashiAudio.Cue("click"));
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>横スクロールの帯。戻り値は中身(content)。</summary>
        public static RectTransform ScrollH(Transform parent, string name, float spacing, float padding)
        {
            var sv = Rect(parent, name);
            var bgImg = sv.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.01f);
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            var vp = Rect(sv, "viewport");
            Anchor(vp, 0, 0, 1, 1);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = Rect(vp, "content");
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var h = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset((int)padding, (int)padding, 4, 4);
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.childControlWidth = true;
            h.childControlHeight = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = vp;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 20f;
            return content;
        }

        /// <summary>2列などのグリッドを縦スクロールで見せる箱。戻り値は中身(content)。</summary>
        public static RectTransform ScrollGrid(Transform parent, string name, int columns, float spacing, float padding, float cellHeight)
        {
            var sv = Rect(parent, name);
            sv.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            var vp = Rect(sv, "viewport");
            Anchor(vp, 0, 0, 1, 1);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = Rect(vp, "content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var g = content.gameObject.AddComponent<GridLayoutGroup>();
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.spacing = new Vector2(spacing, spacing);
            g.padding = new RectOffset((int)padding, (int)padding, 4, 8);
            g.cellSize = new Vector2(400, cellHeight);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = vp;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>ボタンを段ごとに均等割りで並べる箱。戻り値は各段(row)。</summary>
        public static RectTransform[] Rows(Transform parent, string name, int rows, float spacing, float padding)
        {
            var box = Rect(parent, name);
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset((int)padding, (int)padding, 0, 0);
            v.childForceExpandWidth = true; v.childForceExpandHeight = true;
            v.childControlWidth = true; v.childControlHeight = true;
            var result = new RectTransform[rows];
            for (int i = 0; i < rows; i++)
            {
                var row = Rect(box, "row" + i);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = spacing;
                h.childForceExpandWidth = true; h.childForceExpandHeight = true;
                h.childControlWidth = true; h.childControlHeight = true;
                result[i] = row;
            }
            return result;
        }

        /// <summary>縦スクロールで長い文章を表示する箱。戻り値は中のText。</summary>
        public static Text ScrollText(Transform parent, string name, int size, Color c)
        {
            var sv = Rect(parent, name);
            sv.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            var vp = Rect(sv, "viewport");
            Anchor(vp, 0, 0, 1, 1);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = Rect(vp, "content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Txt(content, "text", "", size, c);
            Anchor(t.rectTransform, 0, 0, 1, 1, 10, 10, 10, 10);
            var lay = t.gameObject.AddComponent<LayoutElement>();
            lay.minHeight = 10;
            var cf = content.gameObject.AddComponent<VerticalLayoutGroup>();
            cf.childControlHeight = true;
            cf.childControlWidth = true;
            cf.childForceExpandHeight = false;
            cf.padding = new RectOffset(12, 12, 12, 12);
            // VerticalLayoutGroup が子の位置を管理するので、アンカーを解除
            t.rectTransform.anchorMin = new Vector2(0, 1);
            t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            sr.content = content;
            sr.viewport = vp;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            return t;
        }

        /// <summary>ラベル＋バーのメーター。</summary>
        public class Meter
        {
            public Text value;
            public RectTransform fill;
            public void Set(int v)
            {
                value.text = v.ToString();
                fill.anchorMax = new Vector2(Mathf.Clamp01(v / 100f), 1);
            }
        }

        public static Meter MakeMeter(Transform parent, string label, Color col, float ax0, float ax1, float top)
        {
            var box = Rect(parent, "meter_" + label);
            box.anchorMin = new Vector2(ax0, 1);
            box.anchorMax = new Vector2(ax1, 1);
            box.pivot = new Vector2(0.5f, 1);
            box.offsetMin = new Vector2(10, -(top + 96));
            box.offsetMax = new Vector2(-10, -top);
            var lb = Txt(box, "lb", label, 28, Data.Hex("#a3acc8"), TextAnchor.UpperLeft, true);
            Anchor(lb.rectTransform, 0, 0.45f, 0.6f, 1);
            var val = Txt(box, "val", "0", 44, col, TextAnchor.UpperRight, true);
            Anchor(val.rectTransform, 0.4f, 0.35f, 1, 1);
            var bg = Img(box, "bar", Data.Hex("#2a376a"));
            Anchor(bg.rectTransform, 0, 0, 1, 0, 0, 6, 0, -14);
            var fill = Img(bg.transform, "fill", col);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return new Meter { value = val, fill = fill.rectTransform };
        }
    }
}
