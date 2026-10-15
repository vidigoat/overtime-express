using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Overtime
{
    /// <summary>
    /// The component kit every screen is built from. Sprites are drawn in greyscale and tinted per use, so one
    /// shape serves every colour: a face with a soft top-to-bottom gradient and a bright top highlight, a chunky lip
    /// underneath in a darker shade of the same colour, and a dark outline. Pressing sinks the face into its lip.
    /// Icons are photographed from the real 3D delivery items, so the UI shows the burger you are carrying.
    /// </summary>
    public static class UIKit
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>A lipped, glossy rounded panel, 9-sliced. Tint with Image.color.</summary>
        public static Sprite Panel(int radius = 22, int lip = 8) => cache.TryGetValue($"p{radius}_{lip}", out var s) ? s : cache[$"p{radius}_{lip}"] = MakePanel(radius, lip);

        /// <summary>A Duolingo-style coin node: a round face sitting on a darker extruded edge.</summary>
        public static Sprite Coin => cache.TryGetValue("coin", out var s) ? s : cache["coin"] = MakeCoin(192, 18);

        /// <summary>A full-screen vertical gradient, tinted per screen.</summary>
        public static Sprite Gradient => cache.TryGetValue("grad", out var s) ? s : cache["grad"] = MakeGradient();

        static float Sat(float v) => Mathf.Clamp01(v);

        static Sprite MakePanel(int r, int lip)
        {
            const int ol = 3; int w = (r + lip) * 2 + 8, h = (r + lip) * 2 + 8 + lip;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            // rounded-rect distance (negative inside)
            float RR(float x, float y, float x0, float y0, float x1, float y1, float rad)
            {
                float cx = Mathf.Clamp(x, x0 + rad, x1 - rad), cy = Mathf.Clamp(y, y0 + rad, y1 - rad);
                return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - rad;
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float face = RR(fx, fy, 0, lip, w, h, r);          // the face sits on top
                    float body = RR(fx, fy, 0, 0, w, h - lip, r);      // the lip is the same shape, dropped
                    float outer = Mathf.Min(face, body);
                    float a = Sat(0.5f - outer);
                    if (a <= 0) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    float lum;
                    if (outer > -ol) lum = 0.28f;                                      // outline
                    else if (face > -ol - 0.5f) lum = 0.58f;                           // lip
                    else
                    {
                        float t = (fy - lip) / (h - lip);                              // 0 bottom .. 1 top of face
                        lum = Mathf.Lerp(0.82f, 0.97f, t);
                        float fromTop = (h - fy) - ol;
                        if (fromTop > 2 && fromTop < 6 && Mathf.Abs(fx - w / 2f) < w / 2f - r * 0.7f) lum = 1f;   // glossy top highlight
                        if (face > -ol - 2.5f && face < -ol) lum = Mathf.Lerp(lum, 1f, 0.35f);                // inner rim
                    }
                    byte l = (byte)(Sat(lum) * 255);
                    px[y * w + x] = new Color32(l, l, l, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            int b = r + lip + 2;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b + lip, b, b));
        }

        static Sprite MakeCoin(int size, int lip)
        {
            int w = size, h = size; float rad = (size - lip) / 2f - 3;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            float cx = w / 2f, cyTop = lip + rad + 2, cyBot = rad + 2;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float dTop = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cyTop) * (fy - cyTop)) - rad;
                    float dBot = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cyBot) * (fy - cyBot)) - rad;
                    // the side of the coin: everything between the two discs
                    float dSide = Mathf.Max(Mathf.Abs(fx - cx) - rad, Mathf.Max(cyBot - fy, fy - cyTop));
                    float outer = Mathf.Min(Mathf.Min(dTop, dBot), dSide);
                    float a = Sat(0.5f - outer);
                    if (a <= 0) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    float lum;
                    if (outer > -4) lum = 0.26f;
                    else if (dTop > -4.5f) lum = 0.55f;
                    else
                    {
                        float t = (fy - (cyTop - rad)) / (2 * rad);
                        lum = Mathf.Lerp(0.8f, 0.98f, t);
                        if (dTop > -11 && dTop < -6) lum = Mathf.Lerp(lum, 0.7f, 0.45f);     // inset ring
                        // glossy crescent near the top left
                        float hx = fx - (cx - rad * 0.28f), hy = fy - (cyTop + rad * 0.42f);
                        if (hx * hx / (rad * rad * 0.16f) + hy * hy / (rad * rad * 0.04f) < 1) lum = Mathf.Lerp(lum, 1f, 0.75f);
                    }
                    byte l = (byte)(Sat(lum) * 255);
                    px[y * w + x] = new Color32(l, l, l, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite MakeGradient()
        {
            var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) { float t = y / 127f; var c = Color.Lerp(new Color(0.55f, 0.55f, 0.6f), Color.white, t * t); for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c); }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f), 100);
        }

        // ───────── icons from the real 3D models ─────────
        const int IconLayer = 31;

        /// <summary>A transparent-background portrait of a 3D model, three-quarter view, lit like a product shot.</summary>
        public static Sprite Icon(string model, float yaw = 35f)
        {
            string key = "icon_" + model + yaw;
            if (cache.TryGetValue(key, out var s)) return s;
            var art = ArtLibrary.I;
            var rig = new GameObject("IconRig"); rig.transform.position = new Vector3(0, -800, 0);
            var t = art != null ? art.Spawn(model, rig.transform, rig.transform.position, Quaternion.Euler(0, yaw, 0), 1f, false) : null;
            if (t == null) { Object.Destroy(rig); return cache[key] = null; }
            foreach (var tr in rig.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = IconLayer;
            var rends = rig.GetComponentsInChildren<Renderer>();
            var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);

            var lg = new GameObject("IconLight"); var light = lg.AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << IconLayer;
            lg.transform.rotation = Quaternion.Euler(38, -28, 0); light.intensity = 1.5f; light.shadows = LightShadows.None;
            var cg = new GameObject("IconCam"); var cam = cg.AddComponent<Camera>(); cam.enabled = false;
            cam.cullingMask = 1 << IconLayer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true; cam.allowHDR = false; cam.allowMSAA = true;
            var dir = Quaternion.Euler(24, 0, 0) * Vector3.forward;
            float ext = b.extents.magnitude;
            cam.transform.position = b.center - dir * (ext * 4 + 2); cam.transform.rotation = Quaternion.LookRotation(dir);
            cam.orthographicSize = Mathf.Max(b.extents.y, b.extents.x, b.extents.z) * 1.12f;
            cam.nearClipPlane = 0.01f; cam.farClipPlane = ext * 10 + 10;
            var cd = cam.GetUniversalAdditionalCameraData(); cd.renderPostProcessing = false; cd.antialiasing = AntialiasingMode.None; cd.renderShadows = false;
            var rt = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            cam.targetTexture = rt; cam.Render();
            RenderSettings.fog = fog;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(rig);
            return cache[key] = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
