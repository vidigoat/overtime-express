using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Overtime
{
    /// <summary>
    /// The whole interface, built in code. Chunky and bright: one display font, thick dark outlines, big yellow buttons
    /// that physically press in. Screens: intro, company HQ (the city route), briefing, play HUD, delivery slip, shop.
    /// Everything lives inside the phone's safe area; screens slide in, results count up.
    /// </summary>
    public class Hud
    {
        public static readonly Color Ink = new Color(0.09f, 0.07f, 0.22f), Yellow = new Color(1f, 0.82f, 0.12f), Cyan = new Color(0.22f, 0.9f, 1f),
            Red = new Color(1f, 0.25f, 0.3f), Green = new Color(0.3f, 0.92f, 0.55f), Card = new Color(0.05f, 0.045f, 0.15f, 0.93f), Deep = new Color(0.13f, 0.1f, 0.32f, 0.98f), Text2 = new Color(0.66f, 0.74f, 1f), Panel2 = new Color(0.1f, 0.11f, 0.28f, 1f), Pink = new Color(1f, 0.36f, 0.7f);
        public static readonly string[] DistrictNames = { "MIDTOWN", "THE DOCKS", "NEON MILE", "ANTENNA HILL", "NIGHT MARKET", "CLOCK TOWER" };
        public static readonly Color[] DistrictColors = { new Color(0.62f, 0.45f, 1f), new Color(0.2f, 0.92f, 0.78f), new Color(1f, 0.35f, 0.85f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.65f, 0.2f), new Color(1f, 0.82f, 0.12f) };

        public GameObject intro, menu, brief, play, results, shop, debt, flowRow, condRow;
        // play
        public Text clock, label, score, coins, flow, popup, hint, cond, cine;
        float cineT;
        public Image debtFill, progFill, flowFill, vignette, condFill, clockBg;
        public GameObject[] powRow = new GameObject[4]; public Image[] powFill = new Image[4]; public Text[] powText = new Text[4];
        // tutorial: a ghost thumb that shows the gesture
        public GameObject tut; public Text tutText; Image tutHand; Vector2 tutDir; float tutT;
        // menu
        public Text wallet, company, rankName, rankSub, playSub;
        public Image rankFill;
        public Button[] cards = new Button[6]; public Text[] cardText = new Text[6]; public Image[][] cardPips = new Image[6][]; public Image[] cardBg = new Image[6];
        public Button shopBtn, introGo, playBtn, routeBtn, routeBack, duelBtn;
        // route
        public GameObject routeScreen; public RectTransform routeContent; public ScrollRect routeScroll; public Text routeWallet;
        public Image[] routeNode = new Image[6], routeTick = new Image[6], routeChest = new Image[5], routeIcon = new Image[6], routeBannerIcon = new Image[6], routeLock = new Image[6], routeBan = new Image[6], chestIcon = new Image[5]; public Image routeStart, routeHalo; public Image[] chestTick = new Image[5], chestTag = new Image[5];
        public Text[] routeBanner = new Text[6], routeItem = new Text[6], chestText = new Text[5]; public Button[] chestBtn = new Button[5];
        public readonly List<(Image dot, int seg)> routeDots = new List<(Image, int)>();
        public Text introText;
        public int pulsePin = -1; public bool[] chestReady = new bool[5];
        // brief
        public Text bNum, bItem, bFor, bRule, bDot, bPay, bGoals; public Image bRuleBg, bStage, bGlow, bIcon; public Image[] bGoalTile = new Image[3], bGoalPip = new Image[3]; public Text[] bGoalText = new Text[3]; public Button bStart, bBack;
        // results
        public Text rTitle, rQuote, rLines, rAmounts, rTotal, rRank; public Image[] rPips; public Image rRankFill; public Button retry, next, toMenu;
        public string wageWord = "WAGES";
        // shop
        public Text sWallet, sNote; public Button[] buy = new Button[7]; public Text[] sName = new Text[7]; public Text[] sPrice = new Text[7]; public Image[] sBg = new Image[7]; public Button sBack;

        readonly Font font, italic;
        readonly Sprite round, pill, hex, disc, ring;
        readonly RectTransform canvasRt, safe;
        Rect lastSafe;
        float popupT, hintT, clockBump, coinBump, clockPulse;
        Color popupColor = Color.white;
        readonly Dictionary<GameObject, float> enterT = new Dictionary<GameObject, float>();
        readonly GameObject[] screens;

        // results reveal
        string[] resLines = new string[0]; int[] resAmounts = new int[0]; int resTotal, resStars; float resT = -1, rankFrom, rankTo; string rankLabel = "";
        public System.Action<int> onTick = _ => { };   // haptics hook: 0 light, 1 medium, 2 success

        public Hud()
        {
            font = Resources.Load<Font>("ChakraPetch-Bold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            italic = Resources.Load<Font>("ChakraPetch-BoldItalic") ?? font;
            round = Rounded(64, 12); pill = Rounded(64, 31); hex = HexSprite(64); disc = Disc(96, 0); ring = Disc(96, 0.74f);
            var cg = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cg.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cg.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(390, 844); sc.matchWidthOrHeight = 1f;
            canvasRt = cg.GetComponent<RectTransform>();
            if (Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            vignette = Img(canvasRt, new Color(1, 0, 0.1f, 0), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(vignette.rectTransform); vignette.raycastTarget = false;
            // everything else sits inside the safe area, clear of the notch, the Dynamic Island and the home bar
            var sg = new GameObject("Safe", typeof(RectTransform)); safe = sg.GetComponent<RectTransform>(); safe.SetParent(canvasRt, false); Stretch(safe);
            FitSafeArea();

            // a caption line for staged moments (hand-off, ride-off), shown without the run HUD
            cine = Txt(safe, "", 40, TextAnchor.UpperCenter, 0, -120, 360, 130, Yellow, true, 3); cine.font = italic; cine.supportRichText = true;
            cine.resizeTextForBestFit = true; cine.resizeTextMaxSize = 44; cine.resizeTextMinSize = 18;
            BuildPlay(); BuildMenu(); BuildRoute(); BuildBrief(); BuildResults(); BuildShop(); BuildIntro();
            screens = new[] { intro, menu, routeScreen, brief, results, shop };
        }

        void FitSafeArea()
        {
            var sa = Screen.safeArea;
            if (sa == lastSafe || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafe = sa;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        // ───────── screens ─────────
        void BuildPlay()
        {
            play = Panel("Play"); var p = play.GetComponent<RectTransform>();
            // route progress: a thin line along the very top with the parcel riding it
            var progBg = Img(p, new Color(0, 0, 0, 0.3f), pill, TextAnchor.UpperCenter, 0, -8, 0, 6); progBg.rectTransform.anchorMin = new Vector2(0.06f, 1); progBg.rectTransform.anchorMax = new Vector2(0.94f, 1); progBg.rectTransform.sizeDelta = new Vector2(0, 6);
            progFill = Img(progBg.rectTransform, Yellow, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(progFill.rectTransform); progFill.rectTransform.anchorMax = new Vector2(0, 1);
            // left: wallet, then parcel condition
            var coinPill = Img(p, new Color(0, 0, 0, 0.36f), pill, TextAnchor.UpperLeft, 12, -24, 96, 32);
            Tock(coinPill.rectTransform, 4, 24);
            coins = Txt(coinPill.rectTransform, "0", 19, TextAnchor.MiddleLeft, 33, 0, 60, 30, Color.white, true);
            condRow = Row(p, TextAnchor.UpperLeft, 14, -62, 120, 22);
            var cr = condRow.GetComponent<RectTransform>();
            cond = Txt(cr, "PARCEL", 11, TextAnchor.MiddleLeft, 0, 0, 52, 20, Color.white, true, 1);
            var cb = Img(cr, new Color(0, 0, 0, 0.4f), pill, TextAnchor.MiddleLeft, 50, 0, 58, 8);
            condFill = Img(cb.rectTransform, Green, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(condFill.rectTransform);
            // centre: the clock, in its own capsule so the road stays clear
            clockBg = Img(p, new Color(Ink.r, Ink.g, Ink.b, 0.7f), pill, TextAnchor.UpperCenter, 0, -20, 132, 58);
            clock = Txt(clockBg.rectTransform, "30.0", 46, TextAnchor.MiddleCenter, 0, 2, 140, 60, Color.white, true, 2); clock.font = italic;
            label = Txt(p, "", 11, TextAnchor.UpperCenter, 0, -80, 200, 16, Color.white, true, 1);
            var debtBg = Img(p, new Color(0, 0, 0, 0.4f), pill, TextAnchor.UpperCenter, 0, -98, 132, 8); debt = debtBg.gameObject;
            debtFill = Img(debtBg.rectTransform, Red, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(debtFill.rectTransform);
            flowRow = Row(p, TextAnchor.UpperCenter, 0, -82, 170, 26);
            var fr = flowRow.GetComponent<RectTransform>();
            flow = Txt(fr, "x1.0", 21, TextAnchor.MiddleLeft, 0, 0, 62, 26, Color.white, true);
            var fb = Img(fr, new Color(0, 0, 0, 0.4f), pill, TextAnchor.MiddleLeft, 62, 0, 70, 8);
            flowFill = Img(fb.rectTransform, Color.white, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(flowFill.rectTransform);
            Txt(fr, "FLOW", 11, TextAnchor.MiddleLeft, 136, 0, 40, 24, Color.white, true, 1);
            // right: score, then active power-ups as draining rings
            score = Txt(p, "0", 16, TextAnchor.UpperRight, -14, -30, 140, 22, new Color(1, 1, 1, 0.92f), true);
            for (int i = 0; i < 4; i++)
            {
                powRow[i] = Row(p, TextAnchor.UpperRight, -12, -58 - i * 58, 50, 50);
                var pr = powRow[i].GetComponent<RectTransform>();
                var bg = Img(pr, new Color(Ink.r, Ink.g, Ink.b, 0.78f), null, TextAnchor.MiddleCenter, 0, 0, 50, 50); bg.sprite = disc;
                powFill[i] = Img(pr, Color.white, null, TextAnchor.MiddleCenter, 0, 0, 50, 50); powFill[i].sprite = ring;
                powFill[i].type = Image.Type.Filled; powFill[i].fillMethod = Image.FillMethod.Radial360; powFill[i].fillOrigin = (int)Image.Origin360.Top; powFill[i].fillClockwise = false;
                powText[i] = Txt(pr, "", 11, TextAnchor.MiddleCenter, 0, 0, 50, 50, Color.white, true, 1);
                powRow[i].SetActive(false);
            }
            popup = Txt(safe, "", 40, TextAnchor.MiddleCenter, 0, 190, 380, 100, Color.white, true, 3); Center(popup); popup.font = italic; popup.rectTransform.sizeDelta = new Vector2(370, 110);
            var hintBg = Img(p, new Color(Ink.r, Ink.g, Ink.b, 0.86f), round, TextAnchor.LowerCenter, 0, 30, 344, 54);
            hint = Txt(hintBg.rectTransform, "", 17, TextAnchor.MiddleCenter, 0, 0, 0, 0, Color.white, false); Stretch(hint.rectTransform); hint.rectTransform.offsetMin = new Vector2(12, 4); hint.rectTransform.offsetMax = new Vector2(-12, -4);

            // the tutorial thumb: a glowing fingertip that sweeps the move it wants
            tut = Row(p, TextAnchor.MiddleCenter, 0, -20, 300, 260);
            var tr = tut.GetComponent<RectTransform>();
            tutText = Txt(tr, "", 30, TextAnchor.UpperCenter, 0, 0, 300, 40, Yellow, true, 3); tutText.font = italic;
            var trail = Img(tr, new Color(1, 1, 1, 0.12f), null, TextAnchor.MiddleCenter, 0, -20, 46, 46); trail.sprite = disc; trail.raycastTarget = false;
            tutHand = Img(tr, Color.white, null, TextAnchor.MiddleCenter, 0, -20, 46, 46); tutHand.sprite = disc; tutHand.raycastTarget = false;
            var tin = Img(tutHand.rectTransform, Yellow, null, TextAnchor.MiddleCenter, 0, 0, 26, 26); tin.sprite = disc; tin.raycastTarget = false;
            tut.SetActive(false);
        }

        void BuildMenu()
        {
            menu = Panel("Menu"); var m = menu.GetComponent<RectTransform>();
            Shade(m, 0.12f);
            // top: wallet and the logo
            var wp = Img(m, new Color(0, 0, 0, 0.42f), pill, TextAnchor.UpperRight, -12, -10, 124, 36);
            Tock(wp.rectTransform, 5, 26);
            wallet = Txt(wp.rectTransform, "0", 20, TextAnchor.MiddleLeft, 36, 0, 86, 32, Color.white, true);
            company = Txt(m, "YOUR COMPANY", 12, TextAnchor.UpperLeft, 16, -20, 200, 18, Yellow, true, 1);
            var t1 = Txt(m, "OVER<color=#ff2040>TIME</color>", 64, TextAnchor.UpperCenter, 0, -50, 390, 72, Color.white, true, 3); t1.supportRichText = true; t1.font = italic;
            var tag = Img(m, Red, round, TextAnchor.UpperCenter, 0, -116, 132, 30); tag.rectTransform.localRotation = Quaternion.Euler(0, 0, -4);
            Txt(tag.rectTransform, "EXPRESS", 21, TextAnchor.MiddleCenter, 0, 1, 132, 30, Color.white, true);

            // company rank: the thing you are really growing
            var rk = Img(m, Deep, UIKit.Panel(22, 8), TextAnchor.UpperCenter, 0, -164, 352, 62); var rr = rk.rectTransform;
            var badge = Img(rr, Yellow, null, TextAnchor.MiddleLeft, 12, 0, 44, 44); badge.sprite = hex; badge.raycastTarget = false;
            var bin = Img(badge.rectTransform, Ink, null, TextAnchor.MiddleCenter, 0, 0, 32, 32); bin.sprite = hex; bin.raycastTarget = false;
            Txt(bin.rectTransform, "HQ", 12, TextAnchor.MiddleCenter, 0, 0, 32, 32, Yellow, false);
            rankName = Txt(rr, "SOLO COURIER", 18, TextAnchor.UpperLeft, 66, -9, 270, 22, Color.white, false);
            var rb = Img(rr, new Color(0, 0, 0, 0.45f), pill, TextAnchor.UpperLeft, 66, -34, 270, 8);
            rankFill = Img(rb.rectTransform, Yellow, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(rankFill.rectTransform);
            rankSub = Txt(rr, "", 11, TextAnchor.UpperLeft, 66, -44, 280, 14, Text2, false);

            // deliver, duel a friend on this phone, look at the route, spend wages
            playBtn = Btn(m, "", TextAnchor.LowerCenter, 0, 156, 352, 72, Yellow, Ink, 30);
            var pt = playBtn.GetComponentInChildren<Text>(); pt.text = "DELIVER"; pt.rectTransform.anchoredPosition = new Vector2(0, 9); pt.font = italic;
            playSub = Txt(playBtn.GetComponent<RectTransform>(), "", 13, TextAnchor.MiddleCenter, 0, -18, 330, 16, new Color(Ink.r, Ink.g, Ink.b, 0.8f), false);
            duelBtn = Btn(m, "DUEL  \u00b7  200", TextAnchor.LowerCenter, 0, 90, 352, 50, Pink, Ink, 20);
            routeBtn = Btn(m, "CITY ROUTE", TextAnchor.LowerCenter, -90, 22, 172, 54, Cyan, Ink, 20);
            shopBtn = Btn(m, "GEAR SHOP", TextAnchor.LowerCenter, 90, 22, 172, 54, Color.white, Ink, 20);
        }

        /// <summary>
        /// The city route, Duolingo-style: one winding path of fat 3D coin stops, each showing the actual parcel,
        /// a glossy banner per district, reward chests between them. Cleared stops turn gold with a tick,
        /// the next one sits in a breathing halo under a START bubble, locked ones are dark silhouettes.
        /// </summary>
        void BuildRoute()
        {
            routeScreen = Panel("Route"); var r = routeScreen.GetComponent<RectTransform>();
            var bg = Img(r, new Color(0.17f, 0.1f, 0.4f), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); bg.sprite = UIKit.Gradient; Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(-200, -200); bg.rectTransform.offsetMax = new Vector2(200, 200);
            var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(Image)); var vr = view.GetComponent<RectTransform>(); vr.SetParent(r, false);
            vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = new Vector2(0, 92); vr.offsetMax = new Vector2(0, -66);
            view.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            var content = new GameObject("Content", typeof(RectTransform)); routeContent = content.GetComponent<RectTransform>(); routeContent.SetParent(vr, false);
            routeContent.anchorMin = new Vector2(0, 1); routeContent.anchorMax = new Vector2(1, 1); routeContent.pivot = new Vector2(0.5f, 1);
            routeScroll = view.AddComponent<ScrollRect>(); routeScroll.content = routeContent; routeScroll.horizontal = false; routeScroll.viewport = vr;
            routeScroll.movementType = ScrollRect.MovementType.Elastic; routeScroll.decelerationRate = 0.12f; routeScroll.scrollSensitivity = 30;

            var dotsLayer = Row(routeContent, TextAnchor.UpperCenter, 0, 0, 0, 0);
            routeHalo = Img(routeContent, Color.white, null, TextAnchor.UpperCenter, 0, 0, 136, 136); routeHalo.sprite = ring; routeHalo.raycastTarget = false;
            float y = -26; var spots = new List<Vector2>();
            float[] xs = { 0, -78, 0, 78, 0, -78 };
            for (int i = 0; i < 6; i++)
            {
                var c = DistrictColors[i];
                // district banner: a glossy lipped slab in the district's colour, with the parcel's portrait on the right
                var ban = Img(routeContent, c, UIKit.Panel(18, 7), TextAnchor.UpperCenter, 0, y, 352, 84); ban.raycastTarget = false; routeBan[i] = ban;
                Txt(ban.rectTransform, $"DISTRICT {i + 1}", 12, TextAnchor.UpperLeft, 20, -13, 200, 16, new Color(Ink.r, Ink.g, Ink.b, 0.7f), false);
                routeBanner[i] = Txt(ban.rectTransform, DistrictNames[i], 26, TextAnchor.UpperLeft, 18, -28, 260, 34, Color.white, true, 2); routeBanner[i].font = italic;
                routeBannerIcon[i] = Img(ban.rectTransform, Color.white, null, TextAnchor.MiddleRight, -10, 6, 76, 76); routeBannerIcon[i].raycastTarget = false; routeBannerIcon[i].preserveAspect = true;
                y -= 84 + 44;
                float x = xs[i];
                spots.Add(new Vector2(x, y - 50));
                // the stop: a fat coin with the parcel on its face
                routeNode[i] = Img(routeContent, c, null, TextAnchor.UpperCenter, x, y, 108, 108); routeNode[i].sprite = UIKit.Coin;
                cards[i] = routeNode[i].gameObject.AddComponent<Button>(); cards[i].transition = Selectable.Transition.None;
                routeNode[i].gameObject.AddComponent<Squish>().press = new Vector2(0, -5);
                routeIcon[i] = Img(routeNode[i].rectTransform, Color.white, null, TextAnchor.MiddleCenter, 0, 7, 66, 66); routeIcon[i].raycastTarget = false; routeIcon[i].preserveAspect = true;
                cardText[i] = Txt(routeNode[i].rectTransform, "", 30, TextAnchor.MiddleCenter, 0, 7, 100, 60, Color.white, true, 2); cardText[i].supportRichText = true;
                // badges: a green tick when cleared, a padlock when locked
                var tb = Img(routeNode[i].rectTransform, Green, null, TextAnchor.MiddleCenter, 36, -30, 36, 36); tb.sprite = UIKit.Coin; tb.raycastTarget = false;
                var tk = Tick(tb.rectTransform); tk.rectTransform.localScale = Vector3.one * 0.62f; tk.rectTransform.anchoredPosition = new Vector2(1, 3);
                foreach (var im in tk.GetComponentsInChildren<Image>()) if (im != tk) im.color = Color.white;
                routeTick[i] = tb;
                var lb = Img(routeNode[i].rectTransform, new Color(0.2f, 0.2f, 0.32f), null, TextAnchor.MiddleCenter, 36, -30, 36, 36); lb.sprite = UIKit.Coin; lb.raycastTarget = false;
                var shackle = Img(lb.rectTransform, new Color(0.75f, 0.78f, 0.9f), null, TextAnchor.MiddleCenter, 0, 7, 14, 14); shackle.sprite = ring; shackle.raycastTarget = false;
                var lockBody = Img(lb.rectTransform, new Color(0.75f, 0.78f, 0.9f), round, TextAnchor.MiddleCenter, 0, 0, 18, 12); lockBody.raycastTarget = false;
                routeLock[i] = lb;
                cardPips[i] = new Image[3];
                for (int k = 0; k < 3; k++)
                {
                    var pip = Img(routeContent, new Color(1, 1, 1, 0.2f), null, TextAnchor.UpperCenter, x + (k - 1) * 20, y - 118, 12, 12);
                    pip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); pip.raycastTarget = false; cardPips[i][k] = pip;
                }
                routeItem[i] = Txt(routeContent, "", 13, TextAnchor.UpperCenter, x, y - 132, 220, 18, Text2, false);
                y -= 168;
                if (i < 5)
                {
                    // a pay chest between districts
                    float cx = -xs[i] * 0.4f + (i % 2 == 0 ? 92 : -92);
                    spots.Add(new Vector2(cx, y - 36));
                    // a pay stop: a gold coin carrying a big Tock, the game's own currency
                    routeChest[i] = Img(routeContent, new Color(1f, 0.78f, 0.2f), null, TextAnchor.UpperCenter, cx, y, 86, 86); routeChest[i].sprite = UIKit.Coin;
                    chestBtn[i] = routeChest[i].gameObject.AddComponent<Button>(); chestBtn[i].transition = Selectable.Transition.None; chestBtn[i].targetGraphic = routeChest[i];
                    routeChest[i].gameObject.AddComponent<Squish>().press = new Vector2(0, -4);
                    chestIcon[i] = Tock(routeChest[i].rectTransform, 0, 44, TextAnchor.MiddleCenter); chestIcon[i].rectTransform.anchoredPosition = new Vector2(0, 6);
                    var ct = Img(routeChest[i].rectTransform, Green, null, TextAnchor.MiddleCenter, 30, -26, 30, 30); ct.sprite = UIKit.Coin; ct.raycastTarget = false;
                    var ctk = Tick(ct.rectTransform); ctk.rectTransform.localScale = Vector3.one * 0.5f; ctk.rectTransform.anchoredPosition = new Vector2(1, 3);
                    foreach (var im in ctk.GetComponentsInChildren<Image>()) if (im != ctk) im.color = Color.white;
                    chestTick[i] = ct;
                    var tagBg = Img(routeContent, new Color(Ink.r, Ink.g, Ink.b, 0.85f), pill, TextAnchor.UpperCenter, cx, y - 90, 100, 26); tagBg.raycastTarget = false; chestTag[i] = tagBg;
                    chestText[i] = Txt(tagBg.rectTransform, "", 13, TextAnchor.MiddleCenter, 0, 0, 100, 26, Yellow, false);
                    y -= 128;
                }
            }
            // dotted path between every stop, under everything
            var dl = dotsLayer.GetComponent<RectTransform>(); dl.anchorMin = new Vector2(0.5f, 1); dl.anchorMax = new Vector2(0.5f, 1);
            for (int i = 0; i + 1 < spots.Count; i++)
            {
                Vector2 a = spots[i], b = spots[i + 1]; int n = Mathf.Max(2, Mathf.RoundToInt((a - b).magnitude / 20f));
                for (int k = 1; k < n; k++)
                {
                    var p = Vector2.Lerp(a, b, k / (float)n);
                    var d = Img(dl, new Color(1, 1, 1, 0.16f), null, TextAnchor.MiddleCenter, p.x, p.y, 11, 11); d.sprite = disc; d.raycastTarget = false;
                    routeDots.Add((d, i));
                }
            }
            routeContent.sizeDelta = new Vector2(0, -y + 40);
            // START bubble with a little tail
            routeStart = Img(routeContent, Color.white, UIKit.Panel(14, 5), TextAnchor.UpperCenter, 0, 0, 112, 46); routeStart.raycastTarget = false;
            var tail = Img(routeStart.rectTransform, Color.white, null, TextAnchor.LowerCenter, 0, 3, 16, 16); tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); tail.raycastTarget = false; tail.transform.SetAsFirstSibling();
            Txt(routeStart.rectTransform, "START", 19, TextAnchor.MiddleCenter, 0, 3, 112, 40, Ink, false).font = italic;

            // header and footer
            var head = Img(r, new Color(0.06f, 0.04f, 0.17f, 0.97f), null, TextAnchor.UpperCenter, 0, 0, 0, 66); head.rectTransform.anchorMin = new Vector2(0, 1); head.rectTransform.anchorMax = new Vector2(1, 1);
            head.rectTransform.offsetMin = new Vector2(-200, -66); head.rectTransform.offsetMax = new Vector2(200, 200);
            var line = Img(head.rectTransform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), null, TextAnchor.LowerCenter, 0, 0, 0, 2); line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(1, 0); line.rectTransform.sizeDelta = new Vector2(0, 2);
            Txt(r, "CITY ROUTE", 26, TextAnchor.UpperLeft, 18, -14, 220, 40, Color.white, true).font = italic;
            var wp = Img(r, new Color(0, 0, 0, 0.42f), pill, TextAnchor.UpperRight, -12, -14, 124, 36);
            Tock(wp.rectTransform, 5, 26);
            routeWallet = Txt(wp.rectTransform, "0", 20, TextAnchor.MiddleLeft, 36, 0, 86, 32, Color.white, true);
            var foot = Img(r, new Color(0.06f, 0.04f, 0.17f, 0.97f), null, TextAnchor.LowerCenter, 0, 0, 0, 92); foot.rectTransform.anchorMin = new Vector2(0, 0); foot.rectTransform.anchorMax = new Vector2(1, 0);
            foot.rectTransform.offsetMin = new Vector2(-200, -200); foot.rectTransform.offsetMax = new Vector2(200, 92);
            routeBack = Btn(r, "BACK TO HQ", TextAnchor.LowerCenter, 0, 20, 240, 58, Color.white, Ink, 21);
        }

        Image Tick(RectTransform parent)
        {
            var g = Img(parent, new Color(0, 0, 0, 0), null, TextAnchor.MiddleCenter, 0, 0, 40, 40); g.raycastTarget = false;
            var a = Img(g.rectTransform, Ink, null, TextAnchor.MiddleCenter, -8, -4, 9, 20); a.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); a.raycastTarget = false;
            var b = Img(g.rectTransform, Ink, null, TextAnchor.MiddleCenter, 5, 2, 9, 34); b.rectTransform.localRotation = Quaternion.Euler(0, 0, -38); b.raycastTarget = false;
            return g;
        }

        Image Chest(RectTransform parent, float x, float y)
        {
            var body = Img(parent, new Color(0.55f, 0.32f, 0.12f), round, TextAnchor.UpperCenter, x, y - 18, 64, 42);
            var lid = Img(body.rectTransform, new Color(0.75f, 0.45f, 0.16f), round, TextAnchor.UpperCenter, 0, 16, 70, 24); lid.raycastTarget = false;
            var band = Img(body.rectTransform, Yellow, null, TextAnchor.MiddleCenter, 0, 4, 70, 6); band.raycastTarget = false;
            var latch = Img(body.rectTransform, Yellow, round, TextAnchor.MiddleCenter, 0, 2, 16, 18); latch.raycastTarget = false;
            return body;
        }

        /// <summary>
        /// The delivery ticket. The parcel itself takes the stage, glowing in its district's colour; then who it's for,
        /// the one rule that changes how you run (a coloured chip), Dot's message as a chat bubble, three star tiles, and the pay.
        /// </summary>
        void BuildBrief()
        {
            brief = Panel("Brief"); var b = brief.GetComponent<RectTransform>();
            Shade(b, 0.6f);
            var card = Img(b, new Color(0.13f, 0.1f, 0.32f), UIKit.Panel(24, 9), TextAnchor.MiddleCenter, 0, 26, 358, 668); var c = card.rectTransform;
            // hero stage
            bStage = Img(c, DistrictColors[0], UIKit.Panel(18, 6), TextAnchor.UpperCenter, 0, -12, 334, 206); var st = bStage.rectTransform; bStage.raycastTarget = false;
            var shadeIn = Img(st, new Color(0.05f, 0.03f, 0.15f, 0.55f), round, TextAnchor.MiddleCenter, 0, 3, 314, 180); shadeIn.raycastTarget = false;
            st.gameObject.AddComponent<RectMask2D>().padding = new Vector4(6, 9, 6, 6);
            bGlow = Img(st, Color.white, null, TextAnchor.MiddleCenter, 0, 0, 190, 190); bGlow.sprite = disc; bGlow.raycastTarget = false;
            var floor = Img(st, new Color(0, 0, 0, 0.35f), null, TextAnchor.MiddleCenter, 0, -62, 150, 26); floor.sprite = disc; floor.raycastTarget = false;
            bIcon = Img(st, Color.white, null, TextAnchor.MiddleCenter, 0, 6, 176, 176); bIcon.raycastTarget = false; bIcon.preserveAspect = true;
            var jobTag = Img(st, new Color(0.05f, 0.03f, 0.15f, 0.8f), pill, TextAnchor.UpperLeft, 14, -14, 170, 26); jobTag.raycastTarget = false;
            bNum = Txt(jobTag.rectTransform, "", 12, TextAnchor.MiddleCenter, 0, 0, 170, 24, Cyan, false);
            bItem = Txt(c, "", 30, TextAnchor.UpperCenter, 0, -224, 330, 40, Color.white, true, 2); bItem.font = italic;
            bItem.resizeTextForBestFit = true; bItem.resizeTextMinSize = 18; bItem.resizeTextMaxSize = 30; bItem.horizontalOverflow = HorizontalWrapMode.Wrap; bItem.verticalOverflow = VerticalWrapMode.Truncate;
            bFor = Txt(c, "", 15, TextAnchor.UpperCenter, 0, -264, 330, 20, Text2, false);
            // the rule
            bRuleBg = Img(c, Cyan, UIKit.Panel(14, 5), TextAnchor.UpperCenter, 0, -292, 330, 58);
            bRule = Txt(bRuleBg.rectTransform, "", 14, TextAnchor.MiddleCenter, 0, 0, 0, 0, Ink, false); Stretch(bRule.rectTransform); bRule.rectTransform.offsetMin = new Vector2(14, 8); bRule.rectTransform.offsetMax = new Vector2(-14, -4);
            // Dot, the dispatcher, in a chat bubble
            var av = Img(c, Cyan, null, TextAnchor.UpperLeft, 14, -364, 42, 42); av.sprite = UIKit.Coin; av.raycastTarget = false;
            Txt(av.rectTransform, "DOT", 11, TextAnchor.MiddleCenter, 0, 3, 42, 30, Ink, false);
            var bub = Img(c, new Color(0.2f, 0.18f, 0.42f), UIKit.Panel(14, 4), TextAnchor.UpperLeft, 64, -358, 280, 100); bub.raycastTarget = false;
            Txt(bub.rectTransform, "DOT  ·  DISPATCH", 10, TextAnchor.UpperLeft, 12, -8, 200, 14, Cyan, false);
            bDot = Txt(bub.rectTransform, "", 13, TextAnchor.UpperLeft, 12, -24, 258, 74, Color.white, false); bDot.lineSpacing = 1.02f;
            bDot.resizeTextForBestFit = true; bDot.resizeTextMinSize = 10; bDot.resizeTextMaxSize = 13; bDot.verticalOverflow = VerticalWrapMode.Truncate;
            // star goals as three tiles
            string[] names = { "DELIVER", "GREAT", "PERFECT" };
            for (int i = 0; i < 3; i++)
            {
                var tile = Img(c, new Color(0.2f, 0.18f, 0.42f), UIKit.Panel(12, 4), TextAnchor.UpperCenter, (i - 1) * 110, -472, 102, 68); tile.raycastTarget = false; bGoalTile[i] = tile;
                bGoalPip[i] = Img(tile.rectTransform, Yellow, null, TextAnchor.UpperCenter, 0, -14, 14, 14); bGoalPip[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45); bGoalPip[i].raycastTarget = false;
                bGoalText[i] = Txt(tile.rectTransform, "", 16, TextAnchor.UpperCenter, 0, -26, 100, 20, Color.white, false);
                Txt(tile.rectTransform, names[i], 10, TextAnchor.UpperCenter, 0, -46, 100, 14, Text2, false);
            }
            bGoals = Txt(c, "", 1, TextAnchor.UpperLeft, 0, 0, 1, 1, Color.clear, false);
            // pay
            var payBg = Img(c, new Color(0.05f, 0.03f, 0.15f, 0.7f), pill, TextAnchor.UpperCenter, 0, -552, 260, 34); payBg.raycastTarget = false;
            Tock(payBg.rectTransform, 8, 24);
            bPay = Txt(payBg.rectTransform, "", 14, TextAnchor.MiddleLeft, 40, 0, 216, 30, Color.white, false); bPay.supportRichText = true;
            bStart = Btn(c, "START SHIFT", TextAnchor.LowerCenter, 0, 18, 322, 64, Yellow, Ink, 28);
            bStart.GetComponentInChildren<Text>().font = italic;
            bBack = Btn(b, "BACK", TextAnchor.LowerCenter, 0, 10, 130, 44, new Color(0.3f, 0.3f, 0.5f), Color.white, 17);
        }

        void BuildResults()
        {
            results = Panel("Results"); var r = results.GetComponent<RectTransform>();
            var rbg = Img(r, new Color(0.15f, 0.09f, 0.36f), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); rbg.sprite = UIKit.Gradient; Stretch(rbg.rectTransform);
            rbg.rectTransform.offsetMin = new Vector2(-200, -200); rbg.rectTransform.offsetMax = new Vector2(200, 200);
            rTitle = Txt(r, "DELIVERED!", 54, TextAnchor.MiddleCenter, 0, 300, 390, 70, Color.white, true, 4); Center(rTitle); rTitle.font = italic;
            rPips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var back = Img(r, new Color(1, 1, 1, 0.14f), null, TextAnchor.MiddleCenter, (i - 1) * 54, 236, 34, 34); back.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); back.raycastTarget = false;
                rPips[i] = Img(r, Yellow, null, TextAnchor.MiddleCenter, (i - 1) * 54, 236, 34, 34); rPips[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45); rPips[i].raycastTarget = false;
            }
            var card = Img(r, Deep, UIKit.Panel(22, 8), TextAnchor.MiddleCenter, 0, 20, 350, 350); var c = card.rectTransform;
            rQuote = Txt(c, "", 15, TextAnchor.UpperCenter, 0, -16, 318, 84, Text2, false);
            rLines = Txt(c, "", 18, TextAnchor.UpperLeft, 24, -104, 220, 150, Color.white, false); rLines.lineSpacing = 1.15f;
            rAmounts = Txt(c, "", 18, TextAnchor.UpperRight, -24, -104, 120, 150, Yellow, false); rAmounts.lineSpacing = 1.15f;
            var tot = Img(c, Yellow, UIKit.Panel(14, 5), TextAnchor.LowerCenter, 0, 16, 318, 56);
            rTotal = Txt(tot.rectTransform, "", 26, TextAnchor.MiddleCenter, 0, 0, 318, 56, Ink, false);
            rRank = Txt(r, "", 12, TextAnchor.MiddleCenter, 0, -172, 330, 16, Text2, false);
            var rb = Img(r, new Color(0, 0, 0, 0.45f), pill, TextAnchor.MiddleCenter, 0, -190, 300, 8);
            rRankFill = Img(rb.rectTransform, Yellow, pill, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(rRankFill.rectTransform);
            retry = Btn(r, "RETRY", TextAnchor.MiddleCenter, -90, -240, 168, 62, Color.white, Ink, 26);
            next = Btn(r, "NEXT JOB", TextAnchor.MiddleCenter, 90, -240, 168, 62, Yellow, Ink, 26);
            toMenu = Btn(r, "HQ", TextAnchor.MiddleCenter, 0, -308, 130, 44, new Color(0, 0, 0, 0.45f), Color.white, 18);
        }

        void BuildShop()
        {
            shop = Panel("Shop"); var s = shop.GetComponent<RectTransform>();
            Shade(s, 0.62f);
            Txt(s, "GEAR SHOP", 44, TextAnchor.UpperCenter, 0, -22, 390, 54, Color.white, true, 3).font = italic;
            sNote = Txt(s, "Spend Tocks. Every piece changes how you run.", 13, TextAnchor.UpperCenter, 0, -76, 360, 20, Color.white, true, 1);
            var wp = Img(s, new Color(0, 0, 0, 0.42f), pill, TextAnchor.UpperCenter, 0, -100, 150, 36);
            Tock(wp.rectTransform, 6, 26);
            sWallet = Txt(wp.rectTransform, "0", 21, TextAnchor.MiddleLeft, 40, 0, 104, 32, Color.white, true);
            for (int i = 0; i < 7; i++)
            {
                sBg[i] = Img(s, Deep, UIKit.Panel(22, 8), TextAnchor.UpperCenter, 0, -148 - i * 74, 352, 66);
                sName[i] = Txt(sBg[i].rectTransform, "", 16, TextAnchor.MiddleLeft, 14, 0, 232, 60, Color.white, false); sName[i].supportRichText = true;
                buy[i] = Btn(sBg[i].rectTransform, "", TextAnchor.MiddleRight, -10, 2, 92, 44, Yellow, Ink, 18);
                sPrice[i] = buy[i].GetComponentInChildren<Text>();
            }
            sBack = Btn(s, "BACK TO HQ", TextAnchor.LowerCenter, 0, 22, 200, 54, Color.white, Ink, 22);
        }

        void BuildIntro()
        {
            intro = Panel("Intro"); var r = intro.GetComponent<RectTransform>();
            Shade(r, 0.62f);
            var card = Img(r, Deep, UIKit.Panel(22, 8), TextAnchor.MiddleCenter, 0, 10, 350, 480); var c = card.rectTransform;
            Txt(c, "CONGRATULATIONS", 15, TextAnchor.UpperCenter, 0, -22, 320, 22, Cyan, false);
            Txt(c, "YOU OWN A\nDELIVERY COMPANY", 28, TextAnchor.UpperCenter, 0, -46, 320, 84, Color.white, false).font = italic;
            introText = Txt(c, "", 15, TextAnchor.UpperLeft, 22, -140, 306, 230, Text2, false); introText.lineSpacing = 1.05f;
            introGo = Btn(c, "CLOCK IN", TextAnchor.LowerCenter, 0, 22, 300, 64, Yellow, Ink, 28);
        }

        // ───────── building blocks ─────────
        /// <summary>The Tock icon: a gold hex token with a dark face and clock hands.</summary>
        Image Tock(RectTransform parent, float x, float size, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var o = Img(parent, Yellow, null, anchor, x, 0, size, size); o.sprite = hex; o.raycastTarget = false;
            var i = Img(o.rectTransform, new Color(0.16f, 0.09f, 0.02f), null, TextAnchor.MiddleCenter, 0, 0, size * 0.66f, size * 0.66f); i.sprite = hex; i.raycastTarget = false;
            var h1 = Img(i.rectTransform, Color.white, null, TextAnchor.MiddleCenter, 0, size * 0.11f, size * 0.09f, size * 0.26f); h1.raycastTarget = false;
            var h2 = Img(i.rectTransform, Color.white, null, TextAnchor.MiddleCenter, size * 0.08f, -size * 0.03f, size * 0.09f, size * 0.2f); h2.rectTransform.localRotation = Quaternion.Euler(0, 0, 62); h2.raycastTarget = false;
            return o;
        }

        static Sprite HexSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size]; float c = size / 2f, r = size / 2f - 1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - c), dy = Mathf.Abs(y + 0.5f - c);
                    // pointy-top hexagon distance
                    float d = Mathf.Max(dx, dx * 0.5f + dy * 0.8660254f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r * 0.8660254f - d + 0.5f) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        /// <summary>An anti-aliased disc, or a ring when <paramref name="inner"/> is the hole's share of the radius.</summary>
        static Sprite Disc(int size, float inner)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size]; float c = size / 2f, r = size / 2f - 1, ri = r * inner;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = Mathf.Clamp01(r - d + 0.5f) * (inner > 0 ? Mathf.Clamp01(d - ri + 0.5f) : 1);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

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
        GameObject Panel(string name)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); var r = g.GetComponent<RectTransform>(); r.SetParent(safe, false); Stretch(r);
            g.SetActive(false);
            return g;
        }
        GameObject Row(RectTransform parent, TextAnchor a, float x, float y, float w, float h) { var g = new GameObject("Row", typeof(RectTransform)); var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false); Place(r, a, x, y, w, h); return g; }
        void Shade(RectTransform parent, float a)
        {
            // the dim reaches past the safe area so the notch strip is shaded too
            var i = Img(parent, new Color(Ink.r, Ink.g, Ink.b, a), null, TextAnchor.MiddleCenter, 0, 0, 0, 0); Stretch(i.rectTransform);
            i.rectTransform.offsetMin = new Vector2(-200, -200); i.rectTransform.offsetMax = new Vector2(200, 200); i.rectTransform.SetAsFirstSibling();
        }

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
            // a dark slab underneath gives every button a chunky plastic lip; pressing pushes the face down onto it
            var img = Img(parent, bg, UIKit.Panel(16, 6), anchor, x, y, w, h); var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var t = Txt(img.rectTransform, s, size, TextAnchor.MiddleCenter, 0, 3, w, h, fg, false);
            b.transition = Selectable.Transition.None;
            img.gameObject.AddComponent<Squish>().press = new Vector2(0, -4);
            return b;
        }

        public static void SetPips(Image[] pips, int n, bool visible = true)
        {
            for (int i = 0; i < pips.Length; i++) { pips[i].gameObject.SetActive(visible); pips[i].color = i < n ? Yellow : new Color(1, 1, 1, 0.18f); }
        }

        /// <summary>Screens fade and rise into place instead of cutting.</summary>
        public void Show(GameObject g, bool on)
        {
            if (on && !g.activeSelf) enterT[g] = 0;
            g.SetActive(on);
        }

        public void Popup(string s, Color c, float dur = 0.75f)
        {
            // long headlines step down a size so they never run off a phone screen
            int first = s.IndexOf('\n'); int len = first < 0 ? s.Length : first;
            popup.fontSize = len > 12 ? 24 : len > 9 ? 28 : 36;
            popup.text = s; popupColor = c; popupT = dur;
        }
        public void Label(Button b, string s) { var t = b.GetComponentInChildren<Text>(); if (t) t.text = s; }
        public void Hint(string s) { hint.text = s; hintT = 3.4f; }
        public void Cine(string s, Color c, float dur = 0.5f) { cine.text = s; cine.color = c; cineT = dur; }
        public void BumpClock() { clockBump = 1; }
        public void BumpCoins() { coinBump = 1; }

        /// <summary>Show the tutorial thumb sweeping in a direction (zero = a tap). Null hides it.</summary>
        public void Tutorial(string text, Vector2 dir)
        {
            bool on = text != null;
            if (on && (!tut.activeSelf || tutText.text != text)) tutT = 0;
            tut.SetActive(on);
            if (on) { tutText.text = text; tutDir = dir; }
        }

        /// <summary>The delivery slip fills in line by line, the wage counts up, then the stars land one at a time.</summary>
        public void Results(string[] lines, int[] amounts, int total, int stars, float rankA, float rankB, string rankText)
        {
            resLines = lines; resAmounts = amounts; resTotal = total; resStars = stars; resT = 0; rankFrom = rankA; rankTo = rankB; rankLabel = rankText;
            rLines.text = rAmounts.text = ""; rTotal.text = "WAGES  +0"; rRank.text = rankText;
            rRankFill.rectTransform.anchorMax = new Vector2(rankA, 1);
            foreach (var p in rPips) p.rectTransform.localScale = Vector3.zero;
        }

        void TickResults(float rdt)
        {
            if (resT < 0 || !results.activeSelf) return;
            float prev = resT; resT += rdt;
            const float lineGap = 0.22f, start = 0.25f;
            int n = resLines.Length;
            string l = "", a = "";
            for (int i = 0; i < n; i++)
            {
                float t0 = start + i * lineGap;
                if (resT < t0) break;
                if (prev < t0) onTick(0);
                float k = Mathf.Clamp01((resT - t0) / 0.18f);
                l += (i > 0 ? "\n" : "") + resLines[i];
                a += (i > 0 ? "\n" : "") + (resAmounts[i] < 0 ? "" : Mathf.RoundToInt(resAmounts[i] * k).ToString("N0"));
            }
            rLines.text = l; rAmounts.text = a;
            float tt = start + n * lineGap;
            float kt = Mathf.Clamp01((resT - tt) / 0.6f);
            int shown = Mathf.RoundToInt(Mathf.Abs(resTotal) * (1 - Mathf.Pow(1 - kt, 3)));
            string sign = resTotal < 0 || wageWord == "YOU PAY" ? "" : "+";
            rTotal.text = $"{wageWord}  {sign}{shown} TOCKS";
            if (prev < tt + 0.6f && resT >= tt + 0.6f) onTick(1);
            for (int i = 0; i < 3; i++)
            {
                float t0 = tt + 0.7f + i * 0.28f, k = Mathf.Clamp01((resT - t0) / 0.3f);
                if (i < resStars && prev < t0 && resT >= t0) onTick(2);
                float s = i < resStars ? (k <= 0 ? 0 : 1 + Mathf.Sin(k * Mathf.PI) * 0.45f) : 1;
                rPips[i].rectTransform.localScale = Vector3.one * s;
                rPips[i].color = i < resStars ? Yellow : new Color(1, 1, 1, 0);
            }
            float kr = Mathf.Clamp01((resT - tt - 1.6f) / 0.8f);
            rRankFill.rectTransform.anchorMax = new Vector2(Mathf.Lerp(rankFrom, rankTo, 1 - Mathf.Pow(1 - kr, 2)), 1);
        }

        public void Tick(float rdt)
        {
            FitSafeArea();
            float now = Time.unscaledTime;
            cineT -= rdt; { var cc = cine.color; cc.a = Mathf.Clamp01(cineT * 4); cine.color = cc; }
            popupT -= rdt; hintT -= rdt; clockBump = Mathf.Max(0, clockBump - rdt * 5); coinBump = Mathf.Max(0, coinBump - rdt * 7);
            var c = popupColor; c.a = Mathf.Clamp01(popupT * 4); popup.color = c;
            popup.rectTransform.localScale = Vector3.one * (1 + Mathf.Clamp01((popupT - 0.6f) * 3) * 0.25f);
            hint.transform.parent.gameObject.SetActive(hintT > 0 && !tut.activeSelf);
            clock.rectTransform.localScale = Vector3.one * (1 + clockBump * 0.16f + clockPulse * (0.06f + 0.06f * Mathf.Sin(now * 12)));
            coins.rectTransform.localScale = Vector3.one * (1 + coinBump * 0.25f);
            // screen entrances: fade up and settle
            foreach (var g in screens)
            {
                if (!g.activeSelf || !enterT.TryGetValue(g, out float t)) continue;
                t = Mathf.Min(1, t + rdt / 0.26f); enterT[g] = t;
                float e = 1 - Mathf.Pow(1 - t, 3);
                g.GetComponent<CanvasGroup>().alpha = e;
                g.transform.localPosition = new Vector3(0, -26 * (1 - e), 0);
                g.transform.localScale = Vector3.one * (0.97f + 0.03f * e);
            }
            // the next stop on the route breathes, with its START tag bobbing above it
            if (routeScreen.activeSelf)
            {
                for (int i = 0; i < 6; i++)
                    if (!routeNode[i].GetComponent<Squish>().held)
                        routeNode[i].transform.localScale = Vector3.one * (i == pulsePin ? 1 + 0.045f * (0.5f + 0.5f * Mathf.Sin(now * 3.9f)) : 1);
                if (pulsePin >= 0)
                {
                    var n = routeNode[pulsePin].rectTransform;
                    routeStart.rectTransform.anchoredPosition = n.anchoredPosition + new Vector2(0, 62 + 4 * Mathf.Sin(now * 3.9f));
                    float br = 0.5f + 0.5f * Mathf.Sin(now * 3.9f);
                    routeHalo.rectTransform.anchoredPosition = n.anchoredPosition + new Vector2(0, 14);
                    routeHalo.rectTransform.localScale = Vector3.one * (0.96f + 0.1f * br);
                    var hc = DistrictColors[pulsePin]; routeHalo.color = new Color(hc.r, hc.g, hc.b, 0.25f + 0.3f * (1 - br));
                }
                for (int i = 0; i < 5; i++)
                    if (!routeChest[i].GetComponent<Squish>().held)
                        routeChest[i].transform.localScale = Vector3.one * (chestReady[i] ? 1 + 0.07f * Mathf.Abs(Mathf.Sin(now * 3.2f + i)) : 1);
            }
            if (tut.activeSelf)
            {
                tutT += rdt;
                float k = (tutT % 1.1f) / 0.75f, e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(k));
                bool tap = tutDir == Vector2.zero;
                tutHand.rectTransform.anchoredPosition = new Vector2(0, -40) + tutDir * 110 * e - tutDir * 55;
                tutHand.rectTransform.localScale = Vector3.one * (tap ? 1 - 0.25f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) : 1);
                tutHand.color = new Color(1, 1, 1, k > 1 ? 0 : Mathf.Clamp01(Mathf.Min(k * 6, (1 - k) * 6)));
                tutText.rectTransform.localScale = Vector3.one * (1 + 0.04f * Mathf.Sin(now * 6));
            }
            TickResults(rdt);
        }

        public void SetClockPulse(float k) { clockPulse = k; }
    }

    /// <summary>Buttons push in under the thumb and spring back, like pressing real plastic.</summary>
    public class Squish : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Vector2 press; public bool held;
        public static System.Action onPress = () => { };
        Vector2 home; bool homeSet; float k;
        public void OnPointerDown(PointerEventData e) { held = true; onPress(); }
        public void OnPointerUp(PointerEventData e) { held = false; }
        public void OnPointerExit(PointerEventData e) { held = false; }
        void OnDisable() { held = false; k = 0; if (homeSet) { ((RectTransform)transform).anchoredPosition = home; transform.localScale = Vector3.one; } }
        void Update()
        {
            var r = (RectTransform)transform;
            if (!homeSet) { home = r.anchoredPosition; homeSet = true; }
            k = Mathf.MoveTowards(k, held ? 1 : 0, Time.unscaledDeltaTime * (held ? 18 : 9));
            r.anchoredPosition = home + press * k;
            if (held || k > 0) r.localScale = Vector3.one * (1 - 0.05f * k);
        }
    }
}
