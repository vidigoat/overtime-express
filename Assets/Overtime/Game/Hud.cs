using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Overtime
{
    /// <summary>
    /// The whole interface, built in code. Chunky and bright: one rounded display font, thick dark outlines,
    /// big yellow buttons. Screens: intro, menu, briefing, play HUD, delivery slip, shop.
    /// </summary>
    public class Hud
    {
        public static readonly Color Ink = new Color(0.09f, 0.07f, 0.22f), Yellow = new Color(1f, 0.82f, 0.12f), Cyan = new Color(0.22f, 0.9f, 1f),
            Red = new Color(1f, 0.25f, 0.3f), Green = new Color(0.3f, 0.92f, 0.55f), Card = new Color(0.05f, 0.045f, 0.15f, 0.93f), Text2 = new Color(0.66f, 0.74f, 1f), Panel2 = new Color(0.1f, 0.11f, 0.28f, 1f), Pink = new Color(1f, 0.36f, 0.7f);

        public GameObject intro, menu, brief, play, results, shop, debt, flowRow, condRow;
        // play
        public Text clock, label, score, coins, flow, popup, hint, cond;
        public Image debtFill, progFill, flowFill, vignette, condFill;
        public GameObject[] powRow = new GameObject[4]; public Image[] powFill = new Image[4]; public Text[] powText = new Text[4];
        // menu
        public Text wallet, company;
        public Button[] cards = new Button[6]; public Text[] cardText = new Text[6]; public Image[][] cardPips = new Image[6][]; public Image[] cardBg = new Image[6];
        public Button shopBtn, introGo;
        public Text introText;
        // brief
        public Text bNum, bItem, bFor, bRule, bDot, bPay; public Image bRuleBg; public Button bStart, bBack;
        // results
        public Text rTitle, rQuote, rLines, rAmounts, rTotal; public Image[] rPips; public Button retry, next, toMenu;
        // shop
        public Text sWallet, sNote; public Button[] buy = new Button[7]; public Text[] sName = new Text[7]; public Text[] sPrice = new Text[7]; public Image[] sBg = new Image[7]; public Button sBack;

        readonly Font font, italic;
        readonly Sprite round, pill;
        readonly RectTransform canvasRt;
        float popupT, hintT, clockBump, coinBump;
        Color popupColor = Color.white;

        public Hud()
        {
            font = Resources.Load<Font>("ChakraPetch-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            italic = Resources.Load<Font>("ChakraPetch-BoldItalic") ?? font;
            round = Rounded(64, 12); pill = Rounded(64, 31);
            var cg = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cg.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(390, 844); sc.matchWidthOrHeight = 1f;
            canvasRt = cg.GetComponent<RectTransform>();
            if (Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            vignette = Img(canvasRt, new Color(1, 0, 0.1f, 0), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(vignette.rectTransform); vignette.raycastTarget = false;

            BuildPlay(); BuildMenu(); BuildBrief(); BuildResults(); BuildShop(); BuildIntro();
        }

        // ───────── screens ─────────
        void BuildPlay()
        {
            play = Panel("Play"); var p = play.GetComponent<RectTransform>();
            var progBg = Img(p, new Color(0, 0, 0, 0.28f), pill, TextAnchor.UpperCenter, 0, -12, 0, 10); progBg.rectTransform.anchorMin = new Vector2(0.05f, 1); progBg.rectTransform.anchorMax = new Vector2(0.95f, 1); progBg.rectTransform.sizeDelta = new Vector2(0, 10);
            progFill = Img(progBg.rectTransform, Yellow, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(progFill.rectTransform); progFill.rectTransform.anchorMax = new Vector2(0, 1);
            var coinPill = Img(p, new Color(0, 0, 0, 0.32f), pill, TextAnchor.UpperLeft, 14, -30, 104, 34);
            Img(coinPill.rectTransform, Yellow, pill, TextAnchor.MiddleLeft, 5, 0, 24, 24);
            Img(coinPill.rectTransform, new Color(1f, 0.62f, 0.05f), pill, TextAnchor.MiddleLeft, 10, 0, 14, 14);
            coins = Txt(coinPill.rectTransform, "0", 20, TextAnchor.MiddleLeft, 36, 0, 64, 30, Color.white, true);
            score = Txt(p, "0", 15, TextAnchor.UpperRight, -16, -36, 140, 22, new Color(1, 1, 1, 0.9f), true);
            label = Txt(p, "SECONDS LEFT", 13, TextAnchor.UpperCenter, 0, -66, 300, 18, Color.white, true);
            clock = Txt(p, "30.0", 88, TextAnchor.UpperCenter, 0, -74, 380, 96, Color.white, true, 3); clock.font = italic;
            var debtBg = Img(p, new Color(0, 0, 0, 0.35f), pill, TextAnchor.UpperCenter, 0, -172, 230, 10); debt = debtBg.gameObject;
            debtFill = Img(debtBg.rectTransform, Red, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(debtFill.rectTransform);
            flowRow = Row(p, TextAnchor.UpperCenter, 0, -190, 250, 34);
            var fr = flowRow.GetComponent<RectTransform>();
            flow = Txt(fr, "x1.0", 28, TextAnchor.MiddleLeft, 0, 0, 86, 34, Color.white, true);
            var fb = Img(fr, new Color(0, 0, 0, 0.35f), pill, TextAnchor.MiddleLeft, 90, 0, 100, 10);
            flowFill = Img(fb.rectTransform, Color.white, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(flowFill.rectTransform);
            Txt(fr, "FLOW", 14, TextAnchor.MiddleLeft, 198, 0, 60, 30, Color.white, true);
            condRow = Row(p, TextAnchor.UpperLeft, 14, -72, 130, 26);
            var cr = condRow.GetComponent<RectTransform>();
            cond = Txt(cr, "PARCEL", 12, TextAnchor.MiddleLeft, 2, 0, 56, 22, Color.white, true);
            var cb = Img(cr, new Color(0, 0, 0, 0.35f), pill, TextAnchor.MiddleLeft, 54, 0, 70, 10);
            condFill = Img(cb.rectTransform, Green, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(condFill.rectTransform);
            // active power-ups stack down the right edge, each with its own draining bar
            for (int i = 0; i < 4; i++)
            {
                powRow[i] = Row(p, TextAnchor.UpperRight, -12, -64 - i * 30, 96, 26);
                var pr = powRow[i].GetComponent<RectTransform>();
                var bg = Img(pr, new Color(0, 0, 0, 0.42f), pill, TextAnchor.MiddleCenter, 0, 0, 96, 26);
                powFill[i] = Img(bg.rectTransform, Color.white, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(powFill[i].rectTransform);
                powText[i] = Txt(pr, "", 13, TextAnchor.MiddleCenter, 0, 0, 96, 26, Ink, false);
                powRow[i].SetActive(false);
            }
            popup = Txt(p, "", 40, TextAnchor.MiddleCenter, 0, 150, 380, 100, Color.white, true, 3); Center(popup); popup.font = italic;
            var hintBg = Img(p, new Color(Ink.r, Ink.g, Ink.b, 0.86f), round, TextAnchor.LowerCenter, 0, 34, 344, 58);
            hint = Txt(hintBg.rectTransform, "", 18, TextAnchor.MiddleCenter, 0, 0, 0, 0, Color.white, false); Stretch(hint.rectTransform); hint.rectTransform.offsetMin = new Vector2(12, 4); hint.rectTransform.offsetMax = new Vector2(-12, -4);
        }

        void BuildMenu()
        {
            menu = Panel("Menu"); var m = menu.GetComponent<RectTransform>();
            Shade(m, 0.28f);
            company = Txt(m, "YOUR COMPANY", 14, TextAnchor.UpperCenter, 0, -58, 380, 20, Yellow, true);
            var t1 = Txt(m, "OVER<color=#ff2040>TIME</color>", 76, TextAnchor.UpperCenter, 0, -72, 390, 84, Color.white, true, 3); t1.supportRichText = true; t1.font = italic;
            var tag = Img(m, Red, round, TextAnchor.UpperCenter, 0, -150, 150, 36); tag.rectTransform.localRotation = Quaternion.Euler(0, 0, -4);
            Txt(tag.rectTransform, "EXPRESS", 24, TextAnchor.MiddleCenter, 0, 1, 150, 34, Color.white, true);
            var wp = Img(m, new Color(0, 0, 0, 0.4f), pill, TextAnchor.UpperCenter, 0, -200, 150, 38);
            Img(wp.rectTransform, Yellow, pill, TextAnchor.MiddleLeft, 6, 0, 26, 26);
            Img(wp.rectTransform, new Color(1f, 0.62f, 0.05f), pill, TextAnchor.MiddleLeft, 12, 0, 14, 14);
            wallet = Txt(wp.rectTransform, "0", 22, TextAnchor.MiddleLeft, 40, 0, 104, 32, Color.white, true);
            for (int i = 0; i < 6; i++)
            {
                cardBg[i] = Img(m, Card, round, TextAnchor.UpperCenter, 0, -252 - i * 76, 352, 68);
                cards[i] = cardBg[i].gameObject.AddComponent<Button>();
                cardText[i] = Txt(cardBg[i].rectTransform, "", 18, TextAnchor.MiddleLeft, 16, 0, 250, 64, Color.white, false); cardText[i].supportRichText = true;
                cardPips[i] = Pips(cardBg[i].rectTransform, TextAnchor.MiddleRight, -16, 0, 15);
            }
            shopBtn = Btn(m, "GEAR SHOP", TextAnchor.LowerCenter, 0, 44, 220, 60, Yellow, Ink, 26);
        }

        void BuildBrief()
        {
            brief = Panel("Brief"); var b = brief.GetComponent<RectTransform>();
            Shade(b, 0.5f);
            var card = Img(b, Card, round, TextAnchor.MiddleCenter, 0, 20, 350, 520); var c = card.rectTransform;
            bNum = Txt(c, "DELIVERY 1", 15, TextAnchor.UpperCenter, 0, -20, 320, 22, Cyan, false);
            bItem = Txt(c, "", 32, TextAnchor.UpperCenter, 0, -42, 320, 84, Color.white, false);
            bFor = Txt(c, "", 17, TextAnchor.UpperCenter, 0, -128, 320, 24, Text2, false);
            bRuleBg = Img(c, Cyan, round, TextAnchor.UpperCenter, 0, -164, 316, 74);
            bRule = Txt(bRuleBg.rectTransform, "", 17, TextAnchor.MiddleCenter, 0, 0, 0, 0, Ink, false); Stretch(bRule.rectTransform); bRule.rectTransform.offsetMin = new Vector2(12, 6); bRule.rectTransform.offsetMax = new Vector2(-12, -6);
            var dot = Img(c, Panel2, round, TextAnchor.UpperCenter, 0, -250, 316, 150);
            Txt(dot.rectTransform, "DOT // DISPATCH", 12, TextAnchor.UpperLeft, 14, -10, 200, 16, Cyan, false);
            bDot = Txt(dot.rectTransform, "", 15, TextAnchor.UpperLeft, 14, -30, 290, 116, Color.white, false);
            bPay = Txt(c, "", 18, TextAnchor.LowerCenter, 0, 96, 320, 26, Color.white, false); bPay.supportRichText = true;
            bStart = Btn(c, "START SHIFT", TextAnchor.LowerCenter, 0, 22, 300, 64, Yellow, Ink, 28);
            bBack = Btn(b, "BACK", TextAnchor.LowerCenter, 0, 54, 120, 44, new Color(0, 0, 0, 0.4f), Color.white, 18);
        }

        void BuildResults()
        {
            results = Panel("Results"); var r = results.GetComponent<RectTransform>();
            Shade(r, 0.5f);
            rTitle = Txt(r, "DELIVERED!", 56, TextAnchor.MiddleCenter, 0, 300, 390, 70, Color.white, true, 4); Center(rTitle);
            rPips = Pips(r, TextAnchor.MiddleCenter, 44, 236, 34);
            var card = Img(r, Card, round, TextAnchor.MiddleCenter, 0, 20, 350, 340); var c = card.rectTransform;
            rQuote = Txt(c, "", 15, TextAnchor.UpperCenter, 0, -16, 318, 84, Text2, false);
            rLines = Txt(c, "", 18, TextAnchor.UpperLeft, 24, -108, 200, 150, Color.white, false); rLines.lineSpacing = 1.15f;
            rAmounts = Txt(c, "", 18, TextAnchor.UpperRight, -24, -108, 120, 150, Yellow, false); rAmounts.lineSpacing = 1.15f;
            var tot = Img(c, Yellow, round, TextAnchor.LowerCenter, 0, 16, 318, 56);
            rTotal = Txt(tot.rectTransform, "", 26, TextAnchor.MiddleCenter, 0, 0, 318, 56, Ink, false);
            retry = Btn(r, "RETRY", TextAnchor.MiddleCenter, -90, -210, 168, 62, Color.white, Ink, 26);
            next = Btn(r, "NEXT JOB", TextAnchor.MiddleCenter, 90, -210, 168, 62, Yellow, Ink, 26);
            toMenu = Btn(r, "MENU", TextAnchor.MiddleCenter, 0, -280, 130, 44, new Color(0, 0, 0, 0.4f), Color.white, 18);
        }

        void BuildShop()
        {
            shop = Panel("Shop"); var s = shop.GetComponent<RectTransform>();
            Shade(s, 0.6f);
            Txt(s, "GEAR SHOP", 46, TextAnchor.UpperCenter, 0, -50, 390, 56, Color.white, true, 3);
            sNote = Txt(s, "Spend wages. Every piece changes how you run.", 14, TextAnchor.UpperCenter, 0, -104, 360, 20, Color.white, true);
            var wp = Img(s, new Color(0, 0, 0, 0.4f), pill, TextAnchor.UpperCenter, 0, -130, 150, 38);
            Img(wp.rectTransform, Yellow, pill, TextAnchor.MiddleLeft, 6, 0, 26, 26);
            sWallet = Txt(wp.rectTransform, "0", 22, TextAnchor.MiddleLeft, 40, 0, 104, 32, Color.white, true);
            for (int i = 0; i < 7; i++)
            {
                sBg[i] = Img(s, Card, round, TextAnchor.UpperCenter, 0, -180 - i * 72, 352, 64);
                sName[i] = Txt(sBg[i].rectTransform, "", 16, TextAnchor.MiddleLeft, 14, 0, 232, 60, Color.white, false); sName[i].supportRichText = true;
                buy[i] = Btn(sBg[i].rectTransform, "", TextAnchor.MiddleRight, -10, 0, 92, 44, Yellow, Ink, 18);
                sPrice[i] = buy[i].GetComponentInChildren<Text>();
            }
            sBack = Btn(s, "BACK", TextAnchor.LowerCenter, 0, 40, 150, 52, Color.white, Ink, 22);
        }

        void BuildIntro()
        {
            intro = Panel("Intro"); var r = intro.GetComponent<RectTransform>();
            Shade(r, 0.62f);
            var card = Img(r, Card, round, TextAnchor.MiddleCenter, 0, 10, 350, 430); var c = card.rectTransform;
            Txt(c, "CONGRATULATIONS", 15, TextAnchor.UpperCenter, 0, -22, 320, 22, Cyan, false);
            Txt(c, "YOU OWN A\nDELIVERY COMPANY", 28, TextAnchor.UpperCenter, 0, -46, 320, 84, Color.white, false);
            introText = Txt(c, "", 16, TextAnchor.UpperLeft, 22, -146, 306, 190, Text2, false); introText.lineSpacing = 1.1f;
            introGo = Btn(c, "CLOCK IN", TextAnchor.LowerCenter, 0, 22, 300, 64, Yellow, Ink, 28);
        }

        // ───────── building blocks ─────────
        static Sprite Rounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius))), dy = Mathf.Max(0, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Center(Component c) { var r = c.GetComponent<RectTransform>(); var p = r.anchoredPosition; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = p; }
        static Vector2 Anchor(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => new Vector2(0, 1), TextAnchor.UpperCenter => new Vector2(0.5f, 1), TextAnchor.UpperRight => new Vector2(1, 1),
            TextAnchor.MiddleLeft => new Vector2(0, 0.5f), TextAnchor.MiddleRight => new Vector2(1, 0.5f),
            TextAnchor.LowerCenter => new Vector2(0.5f, 0), TextAnchor.LowerLeft => new Vector2(0, 0), TextAnchor.LowerRight => new Vector2(1, 0), _ => new Vector2(0.5f, 0.5f),
        };
        static void Place(RectTransform r, TextAnchor a, float x, float y, float w, float h) { var v = Anchor(a); r.anchorMin = r.anchorMax = r.pivot = v; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); }
        GameObject Panel(string name) { var g = new GameObject(name, typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(canvasRt, false); Stretch(r); return g; }
        GameObject Row(RectTransform parent, TextAnchor a, float x, float y, float w, float h) { var g = new GameObject("Row", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false); Place(r, a, x, y, w, h); return g; }
        void Shade(RectTransform parent, float a) { var i = Img(parent, new Color(Ink.r, Ink.g, Ink.b, a), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(i.rectTransform); i.rectTransform.SetAsFirstSibling(); }

        Text Txt(RectTransform parent, string s, int size, TextAnchor anchor, float x, float y, float w, float h, Color color, bool outline, int thick = 2)
        {
            var g = new GameObject("Text", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
            Place(r, anchor, x, y, w, h);
            var t = g.AddComponent<Text>(); t.font = font; t.text = s; t.fontSize = size; t.color = color; t.raycastTarget = false;
            t.alignment = anchor == TextAnchor.LowerCenter ? TextAnchor.MiddleCenter : anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline) { var o = g.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(thick, -thick); var sh = g.AddComponent<Shadow>(); sh.effectColor = Ink; sh.effectDistance = new Vector2(0, -thick * 2); }
            return t;
        }
        Image Img(RectTransform parent, Color c, Sprite sprite, TextAnchor anchor, float x, float y, float w, float h)
        {
            // dark cards get a thin neon frame behind them
            if (c == Card && sprite != null)
            {
                var f = Img(parent, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), sprite, anchor, x, y + (Anchor(anchor).y > 0.75f ? 1.5f : Anchor(anchor).y < 0.25f ? -1.5f : 0), w + 3, h + 3);
                f.raycastTarget = false;
            }
            var g = new GameObject("Image", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
            Place(r, anchor, x, y, w, h);
            var i = g.AddComponent<Image>(); i.color = c;
            if (sprite) { i.sprite = sprite; i.type = Image.Type.Sliced; i.pixelsPerUnitMultiplier = 1f; }
            return i;
        }
        Button Btn(RectTransform parent, string s, TextAnchor anchor, float x, float y, float w, float h, Color bg, Color fg, int size)
        {
            // a dark slab underneath gives every button a chunky pressed-plastic lip
            var lip = Img(parent, Ink, round, anchor, x, y - 5, w, h);
            var img = Img(parent, bg, round, anchor, x, y, w, h); var b = img.gameObject.AddComponent<Button>();
            lip.raycastTarget = false;
            var t = Txt(img.rectTransform, s, size, TextAnchor.MiddleCenter, 0, 0, w, h, fg, false);
            var cb = b.colors; cb.pressedColor = new Color(0.82f, 0.82f, 0.82f); cb.fadeDuration = 0.05f; b.colors = cb;
            return b;
        }

        /// <summary>Three diamond pips used as star ratings (drawn, so they never depend on a font glyph).</summary>
        Image[] Pips(RectTransform parent, TextAnchor anchor, float x, float y, float size)
        {
            var arr = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                arr[i] = Img(parent, new Color(1, 1, 1, 0.2f), null, anchor, x - (2 - i) * size * 1.5f, y, size, size);
                arr[i].rectTransform.pivot = new Vector2(0.5f, 0.5f); arr[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45); arr[i].raycastTarget = false;
            }
            return arr;
        }
        public static void SetPips(Image[] pips, int n, bool visible = true)
        {
            for (int i = 0; i < pips.Length; i++) { pips[i].gameObject.SetActive(visible); pips[i].color = i < n ? Yellow : new Color(1, 1, 1, 0.18f); }
        }

        public void Popup(string s, Color c, float dur = 0.75f) { popup.text = s; popupColor = c; popupT = dur; }
        public void Hint(string s) { hint.text = s; hintT = 3.4f; }
        public void BumpClock() { clockBump = 1; }
        public void BumpCoins() { coinBump = 1; }

        public void Tick(float rdt)
        {
            popupT -= rdt; hintT -= rdt; clockBump = Mathf.Max(0, clockBump - rdt * 5); coinBump = Mathf.Max(0, coinBump - rdt * 7);
            var c = popupColor; c.a = Mathf.Clamp01(popupT * 4); popup.color = c;
            popup.rectTransform.localScale = Vector3.one * (1 + Mathf.Clamp01(popupT - 0.6f) * 1.2f);
            hint.transform.parent.gameObject.SetActive(hintT > 0);
            clock.rectTransform.localScale = Vector3.one * (1 + clockBump * 0.12f);
            coins.rectTransform.localScale = Vector3.one * (1 + coinBump * 0.25f);
        }
    }
}
