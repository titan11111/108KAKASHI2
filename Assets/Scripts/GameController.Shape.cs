using UnityEngine;
using UnityEngine.UI;

namespace Kakashi
{
    public partial class GameController
    {
        GameObject shapePanel;
        Text shapeTitle;
        Slider moveX, moveY, moveZ, tilt, twist, size;
        bool shapeMode, wasPressed, partDragging;
        Plane dragPlane;
        Vector3 dragStart, offsetStart;
        readonly Vector3[] panelCorners = new Vector3[4];

        void LayoutPanel(GameObject panel, float height, bool wide)
        {
            var r = panel.GetComponent<RectTransform>();
            if (wide)
            {
                r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, .93f);
                r.pivot = new Vector2(1, .5f);
                r.sizeDelta = new Vector2(Mathf.Min(root.rect.width * .42f, 850), 0);
            }
            else
            {
                r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(1, 0);
                r.pivot = new Vector2(.5f, 0); r.sizeDelta = new Vector2(0, height);
            }
            r.anchoredPosition = Vector2.zero;
        }

        Slider ShapeSlider(Transform parent, string label, float top, float min, float max)
        {
            var row = UIK.Rect(parent, label);
            UIK.Band(row, top, 64, 30);
            var text = UIK.Txt(row, "label", label, 30, Data.Hex("#f2e8d0"), TextAnchor.MiddleLeft);
            UIK.Anchor(text.rectTransform, 0, 0, .13f, 1);
            var track = UIK.Img(row, "slider", Data.Hex("#2a376a"));
            UIK.Anchor(track.rectTransform, .26f, .15f, .98f, .85f);
            var slider = track.gameObject.AddComponent<Slider>();
            var handle = UIK.Img(track.transform, "handle", STRAWC);
            handle.rectTransform.sizeDelta = new Vector2(44, 0);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            var value = UIK.Txt(row, "value", "", 24, Data.Hex("#ecb94b"), TextAnchor.MiddleRight);
            UIK.Anchor(value.rectTransform, .14f, 0, .24f, 1);
            slider.onValueChanged.AddListener(v => { value.text = v.ToString("0.00"); ChangeShape(); });
            return slider;
        }

        void BuildShapePanel()
        {
            var p = Panel("ShapePanel", 900, INK);
            shapePanel = p.gameObject;
            shapeTitle = UIK.Txt(p, "title", "", 36, STRAWC, TextAnchor.MiddleLeft, true);
            UIK.Band(shapeTitle.rectTransform, 12, 60, 30);
            var help = UIK.Txt(p, "help", "部位をタップして選択、ドラッグで移動。左右ボタンで部位を切り替え。", 25, Data.Hex("#a3acc8"));
            UIK.Band(help.rectTransform, 82, 65, 30);
            moveX = ShapeSlider(p, "左右", 155, -.6f, .6f);
            moveY = ShapeSlider(p, "上下", 231, -.5f, .5f);
            moveZ = ShapeSlider(p, "前後", 307, -.4f, .4f);
            tilt = ShapeSlider(p, "傾き", 383, -45, 45);
            twist = ShapeSlider(p, "ひねり", 459, -90, 90);
            size = ShapeSlider(p, "大きさ", 535, .6f, 1.5f);
            var prev = UIK.Btn(p, "prev", "◀ 前の部位", 30, Data.Hex("#2a376a"), Color.white, () => CyclePart(-1));
            var next = UIK.Btn(p, "next", "次の部位 ▶", 30, Data.Hex("#2a376a"), Color.white, () => CyclePart(1));
            UIK.Anchor(prev.GetComponent<RectTransform>(), .03f, .21f, .48f, .3f);
            UIK.Anchor(next.GetComponent<RectTransform>(), .52f, .21f, .97f, .3f);
            var reset = UIK.Btn(p, "reset", "この部位を戻す", 32, Data.Hex("#2a376a"), Color.white, () =>
            {
                string slot = Data.Slots[slotIdx].id;
                scarecrow.Pose(slot).Reset(); scarecrow.ApplyPose(slot); Refresh(); SyncShape();
            });
            UIK.Anchor(reset.GetComponent<RectTransform>(), .03f, .04f, .48f, .16f);
            var done = UIK.Btn(p, "done", "素材選びに戻る", 36, STRAWC, INK, () =>
            {
                shapeMode = false; partDragging = false;
                shapePanel.SetActive(false); buildPanel.SetActive(true); cam.userControl = true;
                ShowOptions(); Refresh();
            });
            UIK.Anchor(done.GetComponent<RectTransform>(), .52f, .04f, .97f, .16f);
            shapePanel.SetActive(false);
        }

        void OpenShape()
        {
            buildPanel.SetActive(false); shapePanel.SetActive(true);
            shapeMode = true; cam.userControl = false;
            if (sel[Data.Slots[slotIdx].id] == "none") CyclePart(1);
            SyncShape();
        }

        void CyclePart(int direction)
        {
            for (int i = 0; i < Data.Slots.Count; i++)
            {
                slotIdx = (slotIdx + direction + Data.Slots.Count) % Data.Slots.Count;
                if (sel[Data.Slots[slotIdx].id] != "none") break;
            }
            SyncShape();
        }

        void SetSlider(Slider slider, float value)
        {
            slider.SetValueWithoutNotify(value);
            slider.transform.parent.Find("value").GetComponent<Text>().text = value.ToString("0.00");
        }

        void SyncShape()
        {
            var slot = Data.Slots[slotIdx]; var p = scarecrow.Pose(slot.id);
            shapeTitle.text = slot.name + "：" + Data.Get(slot.id, sel[slot.id]).name + "    安定度 " + score.Stab;
            SetSlider(moveX, p.offset.x); SetSlider(moveY, p.offset.y); SetSlider(moveZ, p.offset.z);
            SetSlider(tilt, p.tilt); SetSlider(twist, p.twist); SetSlider(size, p.scale);
        }

        void ChangeShape()
        {
            if (!shapeMode) return;
            string slot = Data.Slots[slotIdx].id; var p = scarecrow.Pose(slot);
            p.offset = new Vector3(moveX.value, moveY.value, moveZ.value);
            p.tilt = tilt.value; p.twist = twist.value; p.scale = size.value;
            scarecrow.ApplyPose(slot); Refresh(); SyncShape();
        }

        void Update()
        {
            if (cam == null || root == null) return;
            bool wide = Screen.width > Screen.height * 1.2f;
            var scaler = root.GetComponentInParent<CanvasScaler>();
            scaler.referenceResolution = wide ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = wide ? 1 : .5f;
            LayoutPanel(buildPanel, 900, wide);
            LayoutPanel(shapePanel, 900, wide);
            LayoutPanel(judgePanel, 980, wide);
            var grid = optContent != null ? optContent.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                float cardW = (optContent.rect.width - grid.padding.left - grid.padding.right - grid.spacing.x) / 2f;
                if (cardW > 50f && Mathf.Abs(grid.cellSize.x - cardW) > 1f) grid.cellSize = new Vector2(cardW, grid.cellSize.y);
            }
            // Render into the space above the controls, keeping the whole model visible.
            GameObject panel = shapeMode ? shapePanel : buildPanel.activeSelf ? buildPanel : judgePanel.activeSelf ? judgePanel : null;
            float bottom = 0;
            if (panel != null)
            {
                panel.GetComponent<RectTransform>().GetWorldCorners(panelCorners);
                bottom = Mathf.Clamp(panelCorners[1].y / Screen.height, 0, .75f);
            }
            var camera = cam.GetComponent<Camera>();
            camera.rect = wide && panel != null
                ? new Rect(0, 0, Mathf.Clamp(panelCorners[0].x / Screen.width, .35f, .8f), 1)
                : new Rect(0, bottom, 1, 1 - bottom);
            bool pressed = Ptr.Pressed;
            if (!shapeMode || culturePanel.activeSelf) { partDragging = false; wasPressed = pressed; return; }
            if (pressed && !wasPressed && !Ptr.OverUI())
            {
                var ray = camera.ScreenPointToRay(Ptr.Pos);
                float nearest = float.MaxValue;
                // Test the visible meshes, rather than giant colliders around thin limbs.
                for (int i = 0; i < Data.Slots.Count; i++)
                {
                    var slot = Data.Slots[i];
                    if (sel[slot.id] == "none") continue;
                    foreach (var r in scarecrow.Parts[slot.id].GetComponentsInChildren<Renderer>())
                        if (r.bounds.IntersectRay(ray, out float distance) && distance < nearest)
                        { nearest = distance; slotIdx = i; }
                }
                if (nearest < float.MaxValue)
                {
                    var part = scarecrow.Parts[Data.Slots[slotIdx].id];
                    dragPlane = new Plane(camera.transform.forward, part.position);
                    if (dragPlane.Raycast(ray, out float distance))
                    {
                        dragStart = ray.GetPoint(distance);
                        offsetStart = scarecrow.Pose(Data.Slots[slotIdx].id).offset;
                        partDragging = true; SyncShape();
                    }
                }
            }
            else if (pressed && partDragging)
            {
                var ray = camera.ScreenPointToRay(Ptr.Pos);
                if (dragPlane.Raycast(ray, out float distance))
                {
                    string slot = Data.Slots[slotIdx].id;
                    scarecrow.Pose(slot).offset = offsetStart + scarecrow.transform.InverseTransformVector(ray.GetPoint(distance) - dragStart);
                    scarecrow.ApplyPose(slot); Refresh(); SyncShape();
                }
            }
            if (!pressed) partDragging = false;
            wasPressed = pressed;
        }
    }
}
