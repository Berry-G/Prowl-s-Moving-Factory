using UnityEngine;
using UnityEngine.UI;

namespace PMF.UI
{
    /// <summary>상단 HUD 바 (G-14). 정보 위계를 만든다.
    ///
    ///   최상위 — 보호대상 체력(바 + 숫자), 자원
    ///   보조   — 배속, 모체까지 거리, 다음 스폰까지
    ///
    /// G-12 의 <see cref="PauseMenu"/> 와 같은 방식으로 <b>자기 UI 를 스스로 만든다.</b>
    /// 표시 로직은 기존 컴포넌트(<see cref="EscorteeHealthLabel"/>·<see cref="ResourceLabel"/>·
    /// <see cref="SpeedLabel"/>·<see cref="MotherInfoLabel"/>)를 그대로 붙여서 재사용한다.
    ///
    /// 우상단 90px 은 일시정지 버튼(G-12) 자리이므로 보조 정보를 그 앞에서 끊는다.</summary>
    public sealed class HudTopBar : MonoBehaviour
    {
        private const float BarHeight = 72f;
        private const float PauseButtonReserve = 96f;

        private Font _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var strip = BuildStrip();
            BuildEscorteeHealth(strip);
            BuildResource(strip);
            BuildSecondary(strip);
        }

        private RectTransform BuildStrip()
        {
            var go = new GameObject("HudTopBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();   // 일시정지 블로커·패널보다 아래에 깔린다

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -BarHeight);
            rt.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 0.88f);
            return rt;
        }

        // ---------- 최상위 위계 ----------

        private void BuildEscorteeHealth(RectTransform strip)
        {
            MakeText(strip, "EscorteeCaption", "호위대상", 15, TextAnchor.LowerLeft,
                     new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 16f), new Vector2(300f, 20f));

            // 바 배경 → 그 안에 좌측 stretch fill. EscorteeHealthLabel 이 fill 의 앵커 폭을 조절한다.
            var track = new GameObject("EscorteeHealthTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(strip, false);
            SetRect((RectTransform)track.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(24f, -10f), new Vector2(300f, 22f), new Vector2(0f, 0.5f));
            track.GetComponent<Image>().color = new Color(0.22f, 0.23f, 0.27f, 1f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var number = MakeText(strip, "EscorteeHealthLabel", "-- / --", 26, TextAnchor.MiddleLeft,
                                  new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                  new Vector2(336f, -6f), new Vector2(240f, 34f));

            var label = number.gameObject.AddComponent<EscorteeHealthLabel>();
            label.BindFill(fill.GetComponent<Image>());
        }

        private void BuildResource(RectTransform strip)
        {
            var text = MakeText(strip, "ResourceLabel", "$ 0", 34, TextAnchor.MiddleCenter,
                                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                Vector2.zero, new Vector2(320f, 44f));
            text.gameObject.AddComponent<ResourceLabel>();
        }

        // ---------- 보조 위계 ----------

        private void BuildSecondary(RectTransform strip)
        {
            var speed = MakeText(strip, "SpeedLabel", "1x", 20, TextAnchor.MiddleRight,
                                 new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                 new Vector2(-PauseButtonReserve, 14f), new Vector2(260f, 24f),
                                 new Vector2(1f, 0.5f));
            speed.gameObject.AddComponent<SpeedLabel>();

            var mother = MakeText(strip, "MotherInfoLabel", "", 18, TextAnchor.MiddleRight,
                                  new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                  new Vector2(-PauseButtonReserve, -12f), new Vector2(420f, 24f),
                                  new Vector2(1f, 0.5f));
            mother.color = new Color(0.78f, 0.80f, 0.86f);   // 보조 위계는 한 단계 낮춘다
            mother.gameObject.AddComponent<MotherInfoLabel>();
        }

        // ---------- 헬퍼 ----------

        private Text MakeText(Transform parent, string name, string content, int fontSize, TextAnchor align,
                              Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
            => MakeText(parent, name, content, fontSize, align, anchorMin, anchorMax, pos, size, new Vector2(0f, 0.5f));

        private Text MakeText(Transform parent, string name, string content, int fontSize, TextAnchor align,
                              Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect((RectTransform)go.transform, anchorMin, anchorMax, pos, size, pivot);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;   // 상단 바 글자가 클릭을 먹지 않게
            return text;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
