using System.Collections.Generic;
using Overtime.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Overtime
{
    /// <summary>
    /// Owns the whole game: builds the scene in code, runs the simulation, routes its events to the view,
    /// and carries the story loop: briefing -> run -> hand-off -> wages -> gear shop.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public enum Mode { Intro, Menu, Route, Brief, Count, Play, Results, Shop, Epilogue }
        public static GameRoot I;
        /// <summary>Probes for automated checks: metres into the current wall-run / grind (-1 when not in one), distance run.</summary>
        public static float WallProbe = -1, GrindProbe = -1, DistProbe, EndProbe, RushProbe;
        public Mode mode = Mode.Menu;
        public Sim sim;
        public int levelIdx;
        public bool botMode;
        Bot bot;
        World world; RunnerView runner; CameraRig rig; Fx fx; Hud hud; SwipeInput input; Sfx sfx; Mood mood = new Mood();
        Light key, runnerLight; GameObject bike; Transform bubble;
        ChromaticAberration chroma; Bloom bloom; ColorAdjustments grade;
        float time, countT, endT, dashT, hitT, shownScore, epiT, perfT;
        int perfFrames, perfStage;
        Transform[] traffic; float[] trafficT, trafficV;
        int countN, loaded = -1, briefIdx;

        // ───────── persistence: wallet, gear, progress ─────────
        public static readonly (string id, string name, string desc, int price)[] GearList =
        {
            ("gloves", "GRIP GLOVES", "Forgiving ledge jumps", 250), ("soles", "SPRING SOLES", "Jump higher", 400), ("magnet", "TOCK MAGNET", "Pulls Tocks toward you", 600),
            ("flask", "THERMO FLASK", "Slow-mo burns no extra time", 700), ("wrap", "BUBBLE WRAP", "First crash each run is free", 900),
            ("board", "SKATEBOARD", "Faster, and grinds pay double", 1300), ("bike", "THE BIKE", "Wages x1.5. The dream.", 3000),
        };
        public static int Wallet { get => PlayerPrefs.GetInt("ot.wallet", 0); set { PlayerPrefs.SetInt("ot.wallet", value); PlayerPrefs.Save(); } }
        public static bool Owns(string id) => PlayerPrefs.GetInt("ot.gear." + id, 0) == 1;
        static int Best(string id) => PlayerPrefs.GetInt("ot.best." + id, 0);
        static int Stars(string id) => PlayerPrefs.GetInt("ot.stars." + id, 0);
        bool Unlocked(int i) => i == 0 || botMode || Stars(Levels.All[i - 1].id) >= 1;
        public static readonly (int stars, string name)[] Ranks = { (0, "SOLO COURIER"), (3, "ROOFTOP ROOKIE"), (6, "CITY RUNNER"), (10, "EXPRESS PRO"), (14, "NIGHT LEGEND"), (18, "OVERTIME TYCOON") };
        static int TotalStars() { int n = 0; foreach (var l in Levels.All) n += Stars(l.id); return n; }
        static int RankOf(int stars) { int r = 0; for (int i = 0; i < Ranks.Length; i++) if (stars >= Ranks[i].stars) r = i; return r; }
        /// <summary>How far through the current rank (0..1), for the HQ bar.</summary>
        static float RankProgress(int stars) { int r = RankOf(stars); if (r >= Ranks.Length - 1) return 1; return (stars - Ranks[r].stars) / (float)(Ranks[r + 1].stars - Ranks[r].stars); }
        int NextJob() { for (int i = 0; i < Levels.All.Length; i++) if (Unlocked(i) && Stars(Levels.All[i].id) == 0) return i; for (int i = 0; i < Levels.All.Length; i++) if (Unlocked(i) && Stars(Levels.All[i].id) < 3) return i; return 0; }
        // the first shift teaches by doing: the world slows and a ghost thumb shows the move until you make it
        string tutMove; float tutWait; bool briefFromRoute;
        static int ChestPay(int i) => 120 + i * 80;
        static bool ChestOpen(int i) => PlayerPrefs.GetInt("ot.chest." + i, 0) == 1;
        static bool TutorialDone => PlayerPrefs.GetInt("ot.tut", 0) == 1;

        static Gear CurrentGear() => new Gear { gloves = Owns("gloves"), soles = Owns("soles"), magnet = Owns("magnet"), wrap = Owns("wrap"), flask = Owns("flask"), board = Owns("board"), bike = Owns("bike") };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (I == null) new GameObject("Overtime").AddComponent<GameRoot>(); }

        void OnDestroy() { runner?.Dispose(); world?.customer?.Dispose(); }

        void Awake()
        {
            I = this;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer, phone = Application.isMobilePlatform;
            // in a browser the page's own frame clock is the smooth one; a fixed target makes it stutter
            Application.targetFrameRate = web ? -1 : 120; QualitySettings.vSyncCount = 0;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.gameObject.SetActive(false);
            foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None)) v.gameObject.SetActive(false);

            var cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera") { tag = "MainCamera" }; cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
            cam.farClipPlane = 2500; cam.nearClipPlane = 0.15f; cam.clearFlags = CameraClearFlags.Skybox; cam.allowHDR = true;
            var cd = cam.GetUniversalAdditionalCameraData(); cd.renderPostProcessing = true; cd.antialiasing = phone ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            rig = new CameraRig(cam);

            var lg = new GameObject("Key Light"); key = lg.AddComponent<Light>(); key.type = LightType.Directional;
            lg.transform.rotation = Quaternion.Euler(50, 205, 0); key.intensity = 1.6f; key.shadows = LightShadows.Soft; key.shadowStrength = 0.7f;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                // phones trade a little sharpness and shadow range for a steady frame rate
                urp.shadowDistance = phone ? 32 : 75;
                urp.renderScale = phone ? 0.8f : 1f;
            }

            RenderSettings.skybox = new Material(Shader.Find("Overtime/Sky"));
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.0062f;
            RenderSettings.ambientMode = AmbientMode.Flat;

            var vg = new GameObject("Post"); var vol = vg.AddComponent<Volume>(); vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = prof;
            bloom = prof.Add<Bloom>(true); bloom.threshold.Override(1f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.62f);
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.34f); vig.smoothness.Override(0.5f);
            chroma = prof.Add<ChromaticAberration>(true); chroma.intensity.Override(0.08f);
            var grain = prof.Add<FilmGrain>(true); grain.intensity.Override(0.18f);
            grade = prof.Add<ColorAdjustments>(true); grade.contrast.Override(10f); grade.saturation.Override(8f);

            runner = new RunnerView(transform);
            var bg = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(bg.GetComponent<Collider>()); bg.name = "Shield";
            bubble = bg.transform; bubble.SetParent(runner.root, false); bubble.localPosition = new Vector3(0, 0.95f, 0); bubble.localScale = Vector3.one * 2.3f;
            bg.GetComponent<MeshRenderer>().sharedMaterial = Mats.Fx(1, new Color(0.5f, 0.9f, 1f) * 0.1f); bg.SetActive(false);
            bike = BuildBike(runner.root);
            fx = new Fx(transform); world = new World(); hud = new Hud(); sfx = new Sfx(transform) { muted = true };   // sound stays off until real audio is added
            // the courier carries his own light: it keeps him readable at night and spills onto whatever he passes
            var rl = new GameObject("Runner Light"); rl.transform.SetParent(runner.root, false); rl.transform.localPosition = new Vector3(0, 2.6f, -1.6f);
            runnerLight = rl.AddComponent<Light>(); runnerLight.type = LightType.Point; runnerLight.range = 7; runnerLight.intensity = 7; runnerLight.shadows = LightShadows.None;
            input = new SwipeInput { onSwipe = s => { if (mode == Mode.Play && bot == null) { TutorialSwipe(s); sim.DoSwipe(s); } } };
            Squish.onPress = () => Haptics.Tap(0);
            hud.onTick = k => Haptics.Tap(k == 2 ? 3 : k);

            for (int i = 0; i < 6; i++) { int k = i; hud.cards[i].onClick.AddListener(() => { if (Unlocked(k)) { briefFromRoute = true; OpenBrief(k); } else hud.Popup("LOCKED", Hud.Text2, 0.6f); }); }
            hud.bStart.onClick.AddListener(() => StartLevel(briefIdx, false));
            hud.bBack.onClick.AddListener(() => { if (briefFromRoute) OpenRoute(); else ToMenu(); });
            hud.routeBtn.onClick.AddListener(OpenRoute);
            hud.routeBack.onClick.AddListener(ToMenu);
            for (int i = 0; i < 5; i++) { int k = i; hud.chestBtn[i].onClick.AddListener(() => ClaimChest(k)); }
            hud.retry.onClick.AddListener(() => StartLevel(levelIdx, botMode));
            hud.next.onClick.AddListener(() => { int n = Mathf.Min(Levels.All.Length - 1, levelIdx + 1); if (botMode) StartLevel(n, true); else OpenBrief(n); });
            hud.toMenu.onClick.AddListener(ToMenu);
            hud.shopBtn.onClick.AddListener(OpenShop);
            hud.playBtn.onClick.AddListener(() => { briefFromRoute = false; OpenBrief(NextJob()); });
            hud.sBack.onClick.AddListener(ToMenu);
            for (int i = 0; i < GearList.Length; i++) { int k = i; hud.buy[i].onClick.AddListener(() => Buy(k)); }
            hud.introGo.onClick.AddListener(() => { PlayerPrefs.SetInt("ot.intro", 1); PlayerPrefs.Save(); ToMenu(); });

            ToMenu();
            if (PlayerPrefs.GetInt("ot.intro", 0) == 0)
            {
                hud.introText.text = "Name on the door: OVERTIME EXPRESS.\n\nStaff: you.\nVehicles: none. You run the rooftops.\nBoss: also you.\n\nEvery second on the clock is money. Deliver, get paid, buy better gear, and one day buy the bike in the shop window.";
                SetMode(Mode.Intro);
            }
        }

        /// <summary>A little delivery bike from primitives: the thing you are saving for.</summary>
        static GameObject BuildBike(Transform parent)
        {
            var g = new GameObject("Bike"); g.transform.SetParent(parent, false);
            void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 rot, Color c)
            {
                var p = GameObject.CreatePrimitive(type); Destroy(p.GetComponent<Collider>()); p.transform.SetParent(g.transform, false);
                p.transform.localPosition = pos; p.transform.localScale = scale; p.transform.localRotation = Quaternion.Euler(rot);
                p.GetComponent<MeshRenderer>().sharedMaterial = Mats.Lit(c, 0.3f);
            }
            Color red = Mats.Hex(0xff3b4e), dark = Mats.Hex(0x23264a), white = Mats.Hex(0xf4f6ff), yellow = Mats.Hex(0xffd21e);
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.36f, 0.72f), new Vector3(0.72f, 0.07f, 0.72f), new Vector3(0, 0, 90), dark);
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.36f, -0.72f), new Vector3(0.72f, 0.07f, 0.72f), new Vector3(0, 0, 90), dark);
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.36f, 0.72f), new Vector3(0.3f, 0.09f, 0.3f), new Vector3(0, 0, 90), yellow);
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.36f, -0.72f), new Vector3(0.3f, 0.09f, 0.3f), new Vector3(0, 0, 90), yellow);
            Part(PrimitiveType.Cube, new Vector3(0, 0.62f, 0), new Vector3(0.16f, 0.14f, 1.3f), Vector3.zero, red);
            Part(PrimitiveType.Cube, new Vector3(0, 0.82f, -0.28f), new Vector3(0.3f, 0.1f, 0.5f), Vector3.zero, dark);
            Part(PrimitiveType.Cube, new Vector3(0, 0.92f, 0.62f), new Vector3(0.1f, 0.7f, 0.1f), new Vector3(-18, 0, 0), white);
            Part(PrimitiveType.Cube, new Vector3(0, 1.26f, 0.52f), new Vector3(0.74f, 0.07f, 0.07f), Vector3.zero, dark);
            Part(PrimitiveType.Cube, new Vector3(0, 0.98f, -0.82f), new Vector3(0.5f, 0.42f, 0.44f), Vector3.zero, yellow);
            g.SetActive(false);
            return g;
        }

        // ───────── flow ─────────
        void Prepare(int i)
        {
            levelIdx = i;
            var def = Levels.All[i];
            if (loaded != i) { world.Build(def); loaded = i; }
            sim = new Sim(def, CurrentGear());
            mood.district = def.delivery.district;
            rig.Reset(sim); fx.Clear(new Vector3(0, sim.y + 1.3f, 0));
            runner.flip = 0; runner.SetVisible(true); runner.SetParcel(def.delivery.itemModel, def.delivery.itemScale);
            dashT = hitT = endT = 0; shownScore = 0;
        }

        void SetMode(Mode m)
        {
            mode = m;
            hud.Show(hud.intro, m == Mode.Intro); hud.Show(hud.menu, m == Mode.Menu); hud.Show(hud.brief, m == Mode.Brief); hud.Show(hud.results, m == Mode.Results);
            hud.Show(hud.shop, m == Mode.Shop); hud.Show(hud.routeScreen, m == Mode.Route); hud.play.SetActive(m == Mode.Play || m == Mode.Count);
            if (m != Mode.Play) { tutMove = null; hud.Tutorial(null, Vector2.zero); }
        }
        bool Backdrop => mode == Mode.Menu || mode == Mode.Route || mode == Mode.Intro || mode == Mode.Brief || mode == Mode.Shop || mode == Mode.Epilogue;

        public void StartLevel(int i, bool asBot)
        {
            botMode = asBot;
            Prepare(i);
            bot = asBot ? new Bot(1f, 3) : null;
            countT = 0; countN = 3;
            SetMode(Mode.Count);
            hud.Popup("3", Color.white, 0.55f); sfx.Play("beep");
        }

        public void ToMenu()
        {
            hud.wallet.text = Wallet.ToString("N0");
            hud.company.text = Owns("bike") ? "YOUR COMPANY · NOW WITH WHEELS" : "YOUR COMPANY";
            int total = TotalStars(), rank = RankOf(total);
            hud.rankName.text = $"RANK {rank + 1}  ·  {Ranks[rank].name}";
            hud.rankFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.03f, RankProgress(total)), 1);
            hud.rankSub.text = rank < Ranks.Length - 1 ? $"{total} / {Ranks[rank + 1].stars} stars to {Ranks[rank + 1].name}" : "Top of the city. Every rooftop knows your name.";
            int nx = NextJob();
            hud.pulsePin = nx;
            hud.playSub.text = $"JOB {nx + 1}  ·  {Levels.All[nx].delivery.item}";
            SetMode(Mode.Menu);
            Prepare(0);
            bot = new Bot(0.985f, Random.Range(1, 99999));   // the backdrop is a bot running job 1
        }

        /// <summary>The Duolingo-style path: gold stops you've cleared, the next one breathing, chests that pay out between districts.</summary>
        void OpenRoute()
        {
            hud.routeWallet.text = Wallet.ToString("N0");
            int nx = NextJob(); hud.pulsePin = nx;
            for (int i = 0; i < 6; i++)
            {
                var l = Levels.All[i]; bool open = Unlocked(i); int st = Stars(l.id); bool done = st > 0;
                var c = Hud.DistrictColors[i];
                hud.routeNode[i].color = done ? Hud.Yellow : open ? c : new Color(0.3f, 0.3f, 0.45f);
                var icon = UIKit.Icon(l.delivery.itemModel);
                hud.routeIcon[i].sprite = icon; hud.routeIcon[i].color = open ? Color.white : new Color(0.12f, 0.1f, 0.25f, 0.85f);
                hud.routeBannerIcon[i].sprite = icon; hud.routeBannerIcon[i].color = open ? Color.white : new Color(0.1f, 0.08f, 0.2f, 0.6f);
                hud.routeBan[i].color = open ? c : new Color(0.32f, 0.32f, 0.48f);
                hud.cardText[i].text = icon == null ? (open ? (i + 1).ToString() : "?") : "";
                hud.routeTick[i].gameObject.SetActive(done);
                hud.routeLock[i].gameObject.SetActive(!open);
                Hud.SetPips(hud.cardPips[i], st, true);
                hud.routeItem[i].text = open ? l.delivery.item : "Locked";
                hud.routeBanner[i].color = open ? Color.white : new Color(0.6f, 0.6f, 0.75f);
            }
            for (int i = 0; i < 5; i++)
            {
                bool earned = Stars(Levels.All[i].id) > 0, got = ChestOpen(i);
                hud.chestReady[i] = earned && !got;
                hud.routeChest[i].color = got ? new Color(0.35f, 0.32f, 0.45f) : earned ? new Color(1f, 0.72f, 0.2f) : new Color(0.42f, 0.42f, 0.58f);
                hud.chestIcon[i].sprite = UIKit.Icon("Coin_C", 20); hud.chestIcon[i].color = got ? new Color(1, 1, 1, 0.35f) : earned ? Color.white : new Color(0.15f, 0.13f, 0.3f, 0.8f);
                hud.chestText[i].text = got ? "PAID OUT" : earned ? $"TAP  +{ChestPay(i)}" : $"+{ChestPay(i)}";
                hud.chestText[i].color = got ? Hud.Text2 : Hud.Yellow;
            }
            foreach (var (dot, seg) in hud.routeDots) dot.color = seg < nx * 2 ? new Color(1f, 0.82f, 0.12f, 0.85f) : new Color(1, 1, 1, 0.14f);
            SetMode(Mode.Route);
            // scroll so the next stop sits a third of the way down
            Canvas.ForceUpdateCanvases();
            float content = hud.routeContent.rect.height, view = ((RectTransform)hud.routeScroll.transform).rect.height;
            float target = -hud.routeNode[nx].rectTransform.anchoredPosition.y - view * 0.33f;
            hud.routeScroll.verticalNormalizedPosition = content > view ? 1 - Mathf.Clamp01(target / (content - view)) : 1;
        }

        void ClaimChest(int i)
        {
            if (ChestOpen(i)) { hud.Popup("ALREADY PAID", Hud.Text2, 0.6f); return; }
            if (Stars(Levels.All[i].id) == 0) { hud.Popup($"FINISH JOB {i + 1}", Hud.Text2, 0.7f); return; }
            PlayerPrefs.SetInt("ot.chest." + i, 1); Wallet += ChestPay(i); PlayerPrefs.Save();
            Haptics.Tap(3);
            OpenRoute();
            hud.Popup($"+{ChestPay(i)} TOCKS!", Hud.Yellow, 1f);
        }

        void OpenBrief(int i)
        {
            briefIdx = i;
            var l = Levels.All[i]; var d = l.delivery;
            hud.bNum.text = $"JOB {i + 1}  /  {l.name}";
            hud.bItem.text = d.item.ToUpper();
            hud.bFor.text = "for " + d.customer;
            hud.bRule.text = d.rule;
            hud.bRuleBg.color = d.kind switch { ParcelKind.Heavy => new Color(1f, 0.72f, 0.3f), ParcelKind.Fragile => new Color(0.75f, 0.9f, 1f), ParcelKind.Hot => new Color(1f, 0.55f, 0.45f), ParcelKind.Live => new Color(0.7f, 1f, 0.7f), ParcelKind.Ticking => new Color(1f, 0.6f, 0.85f), _ => new Color(0.86f, 0.9f, 1f) };
            hud.bDot.text = d.briefing;
            hud.bPay.text = $"<size=19><color=#ffd21e>{d.pay}</color></size>  base pay  + time + pickups";
            var dc = Hud.DistrictColors[Mathf.Clamp(d.district, 0, 5)];
            hud.bStage.color = dc; hud.bGlow.color = new Color(1, 1, 1, 0.22f); hud.bGlow.color = new Color(dc.r, dc.g, dc.b, 0.55f);
            hud.bIcon.sprite = UIKit.Icon(d.itemModel, 30);
            int have = Stars(l.id);
            for (int k = 0; k < 3; k++)
            {
                hud.bGoalText[k].text = l.stars[k].ToString("N0");
                hud.bGoalPip[k].color = have > k ? Hud.Yellow : new Color(1, 1, 1, 0.22f);
                hud.bGoalTile[k].color = have > k ? new Color(0.36f, 0.28f, 0.1f) : new Color(0.2f, 0.18f, 0.42f);
            }
            SetMode(Mode.Brief);
        }

        void OpenShop()
        {
            hud.sWallet.text = Wallet.ToString("N0");
            for (int i = 0; i < GearList.Length; i++)
            {
                var g = GearList[i]; bool own = Owns(g.id);
                hud.sName[i].text = $"{g.name}\n<size=13><color=#a8bcff>{g.desc}</color></size>";
                hud.sPrice[i].text = own ? "OWNED" : g.price.ToString("N0");
                hud.buy[i].image.color = own ? new Color(0.3f, 0.92f, 0.55f) : Wallet >= g.price ? Hud.Yellow : new Color(0.55f, 0.58f, 0.72f);
                hud.buy[i].interactable = !own;
            }
            SetMode(Mode.Shop);
        }

        void Buy(int i)
        {
            var g = GearList[i];
            if (Owns(g.id) || Wallet < g.price) { hud.sNote.text = Owns(g.id) ? "You already own that." : $"You need {g.price - Wallet:N0} more Tocks. Go deliver something."; return; }
            Wallet -= g.price; PlayerPrefs.SetInt("ot.gear." + g.id, 1); PlayerPrefs.Save();
            sfx.Play("buy");
            hud.sNote.text = $"{g.name} equipped.";
            if (g.id == "bike") { StartEpilogue(); return; }
            OpenShop();
        }

        /// <summary>
        /// Keep it smooth on slower phones: if the frame rate sags, give up sharpness first, then shadows, then bloom.
        /// Smooth motion matters more to a runner than any of them.
        /// </summary>
        void WatchFrameRate()
        {
            if (Application.isEditor || perfStage >= 3) return;
            perfT += Time.unscaledDeltaTime; perfFrames++;
            if (perfT < 2.5f) return;
            float fps = perfFrames / perfT; perfT = 0; perfFrames = 0;
            if (fps > 44 || time < 4) return;
            perfStage++;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (perfStage == 1) { if (urp) urp.renderScale = 0.68f; }
            else if (perfStage == 2) { key.shadows = LightShadows.None; runnerLight.enabled = false; }
            else { bloom.active = false; chroma.active = false; if (urp) urp.renderScale = 0.58f; }
        }

        public void StartEpilogue()
        {
            Prepare(0);
            bot = new Bot(1f, 5);
            epiT = 0;
            SetMode(Mode.Epilogue);
        }

        /// <summary>Streams of air traffic crossing the sky ahead: the city has places to be.</summary>
        void TickTraffic(float rdt)
        {
            if (traffic == null)
            {
                const int n = 48; traffic = new Transform[n]; trafficT = new float[n]; trafficV = new float[n];
                var holder = new GameObject("Traffic").transform; holder.SetParent(transform, false);
                for (int i = 0; i < n; i++)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(holder, false);
                    g.transform.localScale = new Vector3(6f, 0.45f, 1f);
                    var r = g.GetComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off;
                    r.sharedMaterial = Mats.Glow(i % 6 < 3 ? Mats.Hex(0xff5ad1) : Mats.Hex(0x9fe8ff), 2.2f);
                    traffic[i] = g.transform; trafficT[i] = Random.value; trafficV[i] = 0.035f + Random.value * 0.03f;
                }
            }
            for (int i = 0; i < traffic.Length; i++)
            {
                trafficT[i] = (trafficT[i] + trafficV[i] * rdt) % 1f;
                int lane = i % 6; float dir = lane % 2 == 1 ? 1 : -1;
                float laneZ = Mathf.Ceil((sim.s + 120 + lane * 95) / 570f) * 570f - lane * 95 + 160;
                traffic[i].position = new Vector3((trafficT[i] - 0.5f) * 900 * dir, sim.y + 26 + lane * 9 + Mathf.Sin(trafficT[i] * 20 + i) * 0.6f, laneZ);
            }
        }

        // ───────── frame ─────────
        void Update()
        {
            float rdt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            time += rdt;
            WatchFrameRate();
            input.Update();
            float simDt = 0;
            bool playing = mode == Mode.Play;

            if (playing || Backdrop)
            {
                sim.slow = playing && bot == null && input.slow;
                // while the tutorial thumb is up the world nearly stops; it lets go by itself after a few seconds
                if (tutMove != null) { tutWait += rdt; if (tutWait > 5f) { tutMove = null; hud.Tutorial(null, Vector2.zero); } }
                float worldK = tutMove != null ? 0.12f : 1f;
                float wdt = rdt * worldK;
                int steps = Mathf.Max(1, Mathf.CeilToInt(wdt * 120f)); float h = wdt / steps;
                for (int i = 0; i < steps && sim.ended == EndReason.None; i++)
                {
                    bot?.Act(sim, h);
                    if (!playing) sim.clock = Mathf.Max(sim.clock, 30);
                    sim.Step(h);
                }
                simDt = wdt * (1 - 0.58f * sim.slowAmt);
                Handle(sim.events, playing);
                if (sim.ended != EndReason.None)
                {
                    if (!playing) { var m = mode; Prepare(0); bot = new Bot(0.985f, Random.Range(1, 99999)); mode = m; }
                    else
                    {
                        endT += rdt; simDt = rdt * (sim.ended == EndReason.Finish ? 1f : 0.25f);
                        if (sim.ended != EndReason.Finish) runner.SetVisible(false);
                        if (endT > (sim.ended == EndReason.Finish ? 3.4f : 1.1f)) ShowResults();
                    }
                }
            }
            else if (mode == Mode.Count)
            {
                countT += rdt;
                int n = 3 - Mathf.FloorToInt(countT / 0.55f);
                if (n != countN) { countN = n; if (n > 0) { hud.Popup(n.ToString(), Color.white, 0.55f); sfx.Play("beep"); } else { hud.Popup("GO!", Hud.Yellow, 0.5f); sfx.Play("capsule"); SetMode(Mode.Play); } }
            }

            WallProbe = sim.state == RunState.Wall ? sim.s - sim.wall.s0 : -1;
            GrindProbe = sim.state == RunState.Grind ? sim.s - sim.rail.s0 : -1;
            DistProbe = mode == Mode.Play ? sim.s : 0;
            EndProbe = mode == Mode.Play ? endT : 0; RushProbe = mode == Mode.Play ? sim.rushT : 0;

            // view
            mood.Update(sim, rdt, key, time);
            dashT = Mathf.Max(0, dashT - simDt); hitT = Mathf.Max(0, hitT - rdt * 2.4f);
            var roof = sim.PeekRoof(sim.s);
            Shader.SetGlobalVector("_OTRunner", new Vector4(sim.x, sim.y, sim.s, roof != null ? sim.y - roof.y : 99));
            bool riding = Backdrop && (Owns("bike") || mode == Mode.Epilogue);
            bike.SetActive(riding);
            runner.Pose(sim, simDt, dashT > 0, sim.ended == EndReason.Finish, riding);
            if (sim.ended == EndReason.None || sim.ended == EndReason.Finish) runner.SetVisible(sim.invuln <= 0 || Mathf.FloorToInt(time * 22) % 2 == 0);
            // power-up dressing on the courier
            bubble.gameObject.SetActive(sim.shield && runner.root.gameObject.activeSelf && sim.ended == EndReason.None && mode == Mode.Play);
            if (sim.shield) bubble.localScale = Vector3.one * (2.3f + Mathf.Sin(time * 6) * 0.08f);
            if (sim.rushT > 0 && Random.value < 0.45f) fx.Burst(new Vector3(sim.x + Random.Range(-0.25f, 0.25f), sim.y + Random.Range(0.1f, 0.5f), sim.s - 0.3f), 1, Mats.Hex(0xff8a1f, 2.4f), 1.6f, 1.2f, sim.speed * 0.55f, 0.26f, 0.32f, -2);
            if (sim.fizzT > 0 && Random.value < 0.5f) fx.Burst(new Vector3(sim.x + Random.Range(-0.4f, 0.4f), sim.y + 1.2f, sim.s), 1, Mats.Hex(0x39e6ff, 2.2f), 1.2f, 2.5f, sim.speed * 0.8f, 0.3f, 0.7f, -3);
            if (sim.magnetT > 0 && Random.value < 0.35f) fx.Burst(new Vector3(sim.x, sim.y + 1f, sim.s), 1, Mats.Hex(0xffd21e, 2.2f), 3f, 0.5f, sim.speed * 0.8f, 0.25f, 0.4f, 0);
            var flow = Mood.Flow(sim.chain);
            runner.bodyGlow.SetColor("_Tint", (sim.overtime ? Mats.Hex(0xff2040) : flow) * 2.0f);
            var clockCol = sim.overtime ? Mats.Hex(0xff1030) : Color.Lerp(Mats.Hex(0x39e6ff), Mats.Hex(0xffa62b), mood.tension);
            runner.packGlow.SetColor("_Tint", clockCol * 2.0f);
            runnerLight.color = Color.Lerp(Color.white, Color.Lerp(mood.accent, clockCol, 0.5f), 0.45f);
            fx.Tick(sim, new Vector3(sim.x, sim.y + (sim.state == RunState.Slide ? 0.55f : 1.25f), sim.s - 0.5f), flow, simDt);
            if (sim.state == RunState.Grind) fx.Burst(new Vector3(sim.x, sim.y + 0.04f, sim.s), 2, Mats.Hex(0xffe14a, 2), 3.5f, 1.2f, sim.speed * 0.5f, 0.3f, 0.5f);
            if (sim.state == RunState.Wall) fx.Burst(new Vector3(sim.x + sim.wall.side * 0.8f, sim.y + 0.1f, sim.s), 2, mood.accent * 2, 2.5f, 0.5f, sim.speed * 0.5f, 0.3f, 0.4f);
            if (sim.state == RunState.Slide) fx.Burst(new Vector3(sim.x, sim.y + 0.05f, sim.s + 0.5f), 1, Mats.Hex(0xffd9a0, 1.5f), 2.5f, 0.5f, sim.speed * 0.4f, 0.28f, 0.4f);
            rig.hero = Backdrop && mode != Mode.Epilogue;
            rig.Update(sim, simDt, rdt, mood.over, time);
            // the hand-off: he jogs the last few steps to the customer, turns to them and passes the parcel over
            bool handoff = mode == Mode.Play && sim.ended == EndReason.Finish;
            float hk = handoff ? Mathf.SmoothStep(0, 1, endT / 0.8f) : 0;
            runner.extraZ = Mathf.Lerp(runner.extraZ, handoff ? 3.9f : 0, handoff ? Mathf.Min(1, rdt * 5) : 1);
            runner.faceYaw = Mathf.Lerp(runner.faceYaw, handoff ? Mathf.Atan2(1.7f - sim.x, 1.6f) * Mathf.Rad2Deg * hk : 0, handoff ? Mathf.Min(1, rdt * 6) : 1);
            if (handoff)
            {
                var mid = new Vector3((sim.x + 1.7f) * 0.5f, sim.y + 1.15f, sim.s + 4.6f);
                // a tall phone screen sees very little sideways, so the two are framed in depth: over Kip's shoulder toward the customer
                rig.Stage(mid + new Vector3(-1.9f, 0.75f, -5.2f), mid + new Vector3(0.1f, -0.1f, 0), hk, 50);
                if (hud.play.activeSelf) hud.play.SetActive(false);
                hud.Cine("DELIVERED", Hud.Yellow, 0.4f);
            }
            else if (mode == Mode.Epilogue)
            {
                // the ride-off: low and close behind the bike, sun dead ahead
                epiT += rdt;
                var p = new Vector3(sim.x, sim.y, sim.s);
                rig.Stage(p + new Vector3(1.5f, 1.35f, -4.4f), p + new Vector3(0, 1.0f, 1.5f), Mathf.SmoothStep(0, 1, epiT / 0.9f), 56);
                string line = epiT < 2.6f ? "THE BIKE." : epiT < 5.4f ? "OVERTIME EXPRESS\nHAS WHEELS" : epiT < 8.2f ? "WAGES x1.5\n<size=20>the city just got smaller</size>" : "";
                if (line != "") hud.Cine(line, Hud.Yellow, 0.4f);
                if (epiT > 9f) ToMenu();
            }
            TickTraffic(rdt);
            world.Tick(sim, time, rdt);
            chroma.intensity.value = 0.08f + (sim.rushT > 0 ? 0.35f : 0) + dashT * 2.5f + mood.over * 0.25f + hitT * 0.6f;
            bloom.intensity.value = Mathf.Lerp(0.95f, 0.55f, mood.day) + mood.over * 0.3f + sim.slowAmt * 0.3f;
            grade.saturation.value = 8f - sim.slowAmt * 45f;
            UpdateHud(rdt);
        }

        void UpdateHud(float rdt)
        {
            if (mode == Mode.Play || mode == Mode.Count)
            {
                float c = sim.clock;
                hud.clock.text = sim.overtime ? "-" + Mathf.Abs(c).ToString("F1") : Mathf.Max(0, c).ToString("F1");
                hud.clock.color = sim.overtime ? Hud.Red : c < 8 ? new Color(1f, 0.8f, 0.3f) : Color.white;
                hud.label.text = sim.overtime ? "OVERTIME! DON'T GET HIT" : "";
                hud.SetClockPulse(sim.overtime || c < 8 ? 1 : 0);
                hud.clockBg.color = sim.overtime ? new Color(0.45f, 0.02f, 0.08f, 0.8f) : new Color(Hud.Ink.r, Hud.Ink.g, Hud.Ink.b, 0.7f);
                hud.label.color = sim.overtime ? Hud.Red : Color.white;
                hud.debt.SetActive(sim.overtime);
                if (sim.overtime) hud.debtFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(1 + c / Sim.OtLimit), 1);
                shownScore = Mathf.Lerp(shownScore, sim.points, Mathf.Min(1, rdt * 10));
                hud.score.text = Mathf.RoundToInt(shownScore).ToString("N0");
                hud.coins.text = sim.coinsGot.ToString();
                hud.progFill.rectTransform.anchorMax = new Vector2(sim.Progress, 1);
                hud.flowRow.SetActive(sim.chain > 0 && !sim.overtime);
                if (sim.chain > 0)
                {
                    var fc = Mood.Flow(sim.chain).gamma;
                    hud.flow.text = "x" + sim.Mult.ToString("F1"); hud.flow.color = fc; hud.flowFill.color = fc;
                    hud.flowFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(sim.chainT / Sim.ChainWindow), 1);
                }
                hud.condFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, sim.condition), 1);
                hud.condFill.color = sim.condition > 0.7f ? Hud.Green : sim.condition > 0.4f ? Hud.Yellow : Hud.Red;
                float[] left = { sim.rushT / Sim.RushTime, sim.fizzT / Sim.FizzTime, sim.shield ? 1f : 0f, sim.magnetT / Sim.MagnetTime };
                int slot = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (left[i] <= 0) continue;
                    var look = World.PowerLook[i];
                    hud.powRow[slot].SetActive(true);
                    hud.powFill[slot].color = look.color.gamma; hud.powFill[slot].fillAmount = Mathf.Clamp01(left[i]);
                    hud.powText[slot].text = look.tag;
                    slot++;
                }
                for (; slot < 4; slot++) hud.powRow[slot].SetActive(false);
            }
            float v = sim.overtime && mode == Mode.Play ? 0.16f + 0.08f * Mathf.Sin(time * 14) : hitT * 0.35f;
            hud.vignette.color = new Color(1, 0, 0.1f, v);
            hud.Tick(rdt);
        }

        void Handle(List<Ev> events, bool playing)
        {
            var p = new Vector3(sim.x, sim.y, sim.s); float f = sim.speed * 0.2f;
            foreach (var e in events)
            {
                if (playing)
                {
                    int hk = e.type switch
                    {
                        EvType.Jump or EvType.Land or EvType.Lane or EvType.Coin or EvType.Slide => 0,
                        EvType.Dash or EvType.Capsule or EvType.Crack or EvType.Wrap => 1,
                        EvType.Hit or EvType.Fall => 2, EvType.Power or EvType.Finish or EvType.OtExit => 3, EvType.OtEnter => 4,
                        EvType.Trick when e.name == "perfect" || e.name == "walljump" || e.name == "launch" => 1, _ => -1,
                    };
                    if (hk >= 0) Haptics.Tap(hk);
                }
                switch (e.type)
                {
                    case EvType.Land: runner.squash = 0.8f; runner.OneShot("Jump_Land", 0.7f, 2.2f); rig.dip = 0.35f; rig.shake = Mathf.Max(rig.shake, 0.1f); fx.Burst(p + Vector3.up * 0.08f, 12, Color.white, 3.5f, 0.4f, f, 0.5f, 0.45f, 3); if (playing) sfx.Play("land"); break;
                    case EvType.Jump: runner.squash = -0.5f; runner.OneShot("Jump_Start", 0.6f, 2.6f); fx.Burst(p + Vector3.up * 0.08f, 6, Color.white, 2.5f, 0.2f, f, 0.4f, 0.35f, 2); if (playing) sfx.Play("jump"); break;
                    case EvType.Slide: if (playing) sfx.Play("slide", 0.7f); break;
                    case EvType.Lane: if (playing) sfx.Play("lane"); break;
                    case EvType.Dash: dashT = 0.32f; rig.fovKick = 13; runner.squash = -0.9f; fx.Burst(p + Vector3.up, 30, Color.white * 1.5f, 7, 0, sim.speed * 0.5f, 0.55f, 0.4f, 0); if (playing) { hud.BumpClock(); sfx.Play("dash"); } break;
                    case EvType.Hit: rig.shake = 0.75f; hitT = 1; runner.OneShot("Hit_Chest", 1f, 1.3f); fx.Burst(p + Vector3.up, 46, Mats.Hex(0xff2040, 2), 10, 2, sim.speed * 0.4f, 0.6f, 0.7f);
                        if (playing) { sfx.Play("hit"); if (sim.ended == EndReason.None) hud.Popup("CRASH!\n<size=18>-4 seconds</size>", Hud.Red); } break;
                    case EvType.Fall: rig.shake = 0.6f; hitT = 0.8f; if (playing) { sfx.Play("hit"); if (sim.ended == EndReason.None) hud.Popup("WHOOPS!\n<size=18>-5 seconds</size>", Hud.Red); } break;
                    case EvType.Wrap: fx.Burst(p + Vector3.up, 40, Color.white * 1.5f, 8, 2, f, 0.5f, 0.6f); if (playing) { sfx.Play("crack"); hud.Popup("BUBBLE WRAP!\n<size=18>crash absorbed</size>", Hud.Cyan); } break;
                    case EvType.Crack: rig.shake = 0.4f; if (playing) { sfx.Play("crack"); hud.Popup("CRACK!\n<size=18>land softer</size>", Hud.Red); } break;
                    case EvType.Bored: if (playing) hud.Popup("IT'S BORED!\n<size=18>-1.5 seconds</size>", Hud.Pink); break;
                    case EvType.Power:
                    {
                        int k = (int)System.Enum.Parse(typeof(PowerKind), e.name); var look = World.PowerLook[k];
                        fx.Burst(p + Vector3.up, 60, look.color * 2.2f, 9, 3, f, 0.6f, 0.8f, 5); rig.fovKick = k == 0 ? 14 : 8; runner.squash = -0.6f;
                        if (playing)
                        {
                            sfx.Play("buy");
                            string sub = k == 0 ? "smash through everything" : k == 1 ? "every move pays double" : k == 2 ? "next crash is free" : "tocks fly to you";
                            hud.Popup($"{look.name}!\n<size=17>{sub}</size>", look.color, 1.2f);
                        }
                        break;
                    }
                    case EvType.Coin: fx.Burst(p + Vector3.up * 0.9f, 5, Mats.Hex(0xffd21e, 2.2f), 3.5f, 1.5f, f, 0.32f, 0.35f, 6); if (playing) { hud.BumpCoins(); sfx.Play("coin", 0.55f); } break;
                    case EvType.Capsule: fx.Burst(p + Vector3.up * 1.1f, 22, Mats.Hex(0x9dffea, 2), 5, 1.5f, f, 0.5f, 0.6f, 4); if (playing) { hud.BumpClock(); sfx.Play("capsule"); } break;
                    case EvType.OtEnter: rig.shake = 0.95f; rig.fovKick = 16; hitT = 1; if (playing) { sfx.Play("ot"); hud.Popup("OVERTIME!\n<size=17>double points · don't get hit</size>", Hud.Red, 1.4f); } break;
                    case EvType.OtExit: rig.fovKick = 10; fx.Burst(p + Vector3.up, 60, Mats.Hex(0x39e6ff, 2), 10, 3, f, 0.6f, 0.9f); if (playing) { sfx.Play("win", 0.7f); hud.Popup("CLOCKED BACK IN!\n<size=17>+3 seconds</size>", Color.white, 1.4f); } break;
                    case EvType.Finish: fx.Burst(p + Vector3.up * 1.5f, 110, Mats.Hex(0xffd21e, 2), 13, 6, f, 0.7f, 1.4f, 7); fx.Burst(p + Vector3.up * 1.5f, 70, Mats.Hex(0xff5cb3, 2), 10, 7, f, 0.7f, 1.5f, 7);
                        if (playing) sfx.Play("win"); break;
                    case EvType.Hint:
                        if (!playing) break;
                        if (levelIdx == 0 && !TutorialDone && TutorialFor(e.text, out var move, out var say, out var dir)) { tutMove = move; tutWait = 0; hud.Tutorial(say, dir); }
                        else hud.Hint(e.text);
                        break;
                    case EvType.Trick:
                        if (e.name == "walljump" || e.name == "launch") runner.flip = 1;
                        if (e.name == "perfect") { fx.Burst(p + Vector3.up * 0.15f, 44, Mats.Hex(0x4dff9a, 2), 8, 2.5f, f, 0.6f, 0.7f, 5); rig.fovKick = 7; }
                        else if (e.name != "dash") fx.Burst(p + Vector3.up, 8 + Mathf.Min(16, e.chain), Mood.Flow(sim.chain) * 1.6f, 4.5f, 1, f, 0.4f, 0.5f, 3);
                        if (playing && e.name != "dash")
                        {
                            sfx.Trick(e.chain);
                            hud.Popup($"{Sim.TrickLabel(e.name)}{(e.name == "perfect" ? "!" : "")}\n<size=18>+{e.gain:F1}s{(e.chain > 1 ? $"  ·  {e.chain} chain" : "")}</size>", Mood.Flow(sim.chain).gamma);
                            hud.BumpClock();
                        }
                        break;
                }
            }
            events.Clear();
        }

        static bool TutorialFor(string hint, out string move, out string say, out Vector2 dir)
        {
            move = null; say = null; dir = Vector2.zero;
            if (hint.StartsWith("SWIPE LEFT")) { move = "side"; say = "SWIPE SIDEWAYS"; dir = Vector2.right; }
            else if (hint.StartsWith("SWIPE UP")) { move = "Up"; say = "SWIPE UP TO JUMP"; dir = Vector2.up; }
            else if (hint.StartsWith("SWIPE DOWN")) { move = "Down"; say = "SWIPE DOWN TO SLIDE"; dir = Vector2.down; }
            else if (hint.StartsWith("SWIPE RIGHT AGAIN")) { move = "Right"; say = "SWIPE RIGHT: WALL RUN"; dir = Vector2.right; }
            return move != null;
        }

        void TutorialSwipe(Swipe s)
        {
            if (tutMove == null) return;
            if (tutMove == s.ToString() || (tutMove == "side" && (s == Swipe.Left || s == Swipe.Right))) { tutMove = null; hud.Tutorial(null, Vector2.zero); hud.Popup("NICE!", Hud.Green, 0.6f); Haptics.Tap(3); }
        }

        void ShowResults()
        {
            var L = sim.level; var d = L.delivery; bool won = sim.ended == EndReason.Finish; int score = sim.finalScore;
            int stars = 0; if (won) foreach (int t in L.stars) if (score >= t) stars++;
            int prevBest = Best(L.id);
            int starsBefore = TotalStars();
            // wages: base pay + a bonus for every second left + the tips you picked up + a tip for the parcel's condition
            int timeBonus = won ? Mathf.FloorToInt(Mathf.Max(0, sim.clock)) * 2 : 0;
            int tips = won ? sim.coinsGot : sim.coinsGot / 2;
            int care = won ? Mathf.RoundToInt(d.pay * 0.5f * sim.condition) : 0;
            int late = won && sim.overtime ? 40 : 0;
            int total = (won ? d.pay : 0) + timeBonus + tips + care + late;
            if (Owns("bike")) total = Mathf.RoundToInt(total * 1.5f);
            if (!botMode)
            {
                Wallet += total;
                PlayerPrefs.SetInt("ot.best." + L.id, Mathf.Max(prevBest, score));
                PlayerPrefs.SetInt("ot.stars." + L.id, Mathf.Max(Stars(L.id), stars));
                PlayerPrefs.Save();
            }
            SetMode(Mode.Results);
            hud.rTitle.text = won ? (sim.overtime ? "DELIVERED LATE!" : "DELIVERED!") : sim.ended == EndReason.Timeout ? "OUT OF TIME" : "PARCEL DOWN";
            hud.rTitle.color = won ? Color.white : Hud.Red;
            hud.rQuote.text = won ? $"{d.customer}:\n{d.thanks}" : sim.ended == EndReason.Timeout
                ? "Dot: \"Overtime ran dry, boss. Chain your moves and it pays itself back.\"" : "Dot: \"One hit in overtime and it's over. Clock in earlier next time.\"";
            var lines = new List<string>(); var amts = new List<int>();
            if (won)
            {
                lines.Add("Base pay"); amts.Add(d.pay); lines.Add("Time bonus"); amts.Add(timeBonus); lines.Add("Tocks picked up"); amts.Add(tips);
                lines.Add($"Care bonus ({Mathf.RoundToInt(sim.condition * 100)}%)"); amts.Add(care);
                if (late > 0) { lines.Add("Overtime rate"); amts.Add(late); }
                if (Owns("bike")) { lines.Add("Bike  x1.5"); amts.Add(-1); }
            }
            else { lines.Add("Tocks you kept"); amts.Add(tips); }
            int starsAfter = botMode ? starsBefore : TotalStars(), rb = RankOf(starsBefore), ra = RankOf(starsAfter);
            string rankText = ra > rb ? $"RANK UP!  {Ranks[ra].name}" : ra < Ranks.Length - 1 ? $"{Ranks[ra].name}  ·  {starsAfter} / {Ranks[ra + 1].stars} stars" : Ranks[ra].name;
            hud.Results(lines.ToArray(), amts.ToArray(), total, stars, ra > rb ? 0 : RankProgress(starsBefore), RankProgress(starsAfter), rankText);
            hud.rRank.color = ra > rb ? Hud.Yellow : Hud.Text2;
            if (won && levelIdx == 0 && !botMode) { PlayerPrefs.SetInt("ot.tut", 1); PlayerPrefs.Save(); }
            bool canNext = won && levelIdx + 1 < Levels.All.Length && (stars >= 1 || Unlocked(levelIdx + 1));
            hud.next.gameObject.SetActive(canNext);
            hud.next.transform.parent.GetChild(hud.next.transform.GetSiblingIndex() - 1).gameObject.SetActive(canNext);
        }
    }
}
