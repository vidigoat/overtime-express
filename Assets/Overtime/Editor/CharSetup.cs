using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>Imports the designed characters as Humanoid so the shared animation library plays on all of them.</summary>
    public static class CharSetup
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            foreach (var path in Directory.GetFiles("Assets/Overtime/Art/Chars", "*.fbx"))
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.SaveAndReimport();
                // these rigs park the feet under the root (an IK layout), which Humanoid rejects; map the ankle end-bones as the feet instead.
                // RunnerView then makes the real foot bones follow those ankles every frame.
                var desc = imp.humanDescription;
                var human = new System.Collections.Generic.List<HumanBone>(desc.human);
                human.RemoveAll(h => h.humanName == "LeftFoot" || h.humanName == "RightFoot");
                human.Add(new HumanBone { humanName = "LeftFoot", boneName = "LowerLeg.L_end", limit = new HumanLimit { useDefaultValues = true } });
                human.Add(new HumanBone { humanName = "RightFoot", boneName = "LowerLeg.R_end", limit = new HumanLimit { useDefaultValues = true } });
                desc.human = human.ToArray();
                imp.humanDescription = desc;
                imp.SaveAndReimport();
                sb.Append($"[mapped {desc.human.Length} skel {desc.skeleton.Length}] ");
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var an = go.GetComponent<Animator>();
                var b = new Bounds(); bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                sb.Append($"{Path.GetFileNameWithoutExtension(path)}: human={(an && an.avatar ? an.avatar.isValid && an.avatar.isHuman : false)} size={b.size} parts=");
                foreach (var r in go.GetComponentsInChildren<Renderer>()) { sb.Append(r.name + "["); foreach (var m in r.sharedMaterials) sb.Append((m ? m.name + "#" + ColorUtility.ToHtmlStringRGB(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color) : "null") + " "); sb.Append("] "); }
                sb.Append(" | ");
            }
            return sb.ToString();
        }

        /// <summary>Stand every designed character in a row (bind pose) and photograph their faces, to choose and style a hero.</summary>
        public static string Lineup(string file)
        {
            var root = new GameObject("Lineup"); var origin = new Vector3(0, 300, 0); root.transform.position = origin;
            // the two hero options, fully dressed, front and back
            var flow = new Material(Shader.Find("Overtime/Glow")); flow.SetColor("_Tint", new Color(0.22f, 0.9f, 1f) * 2f);
            var clock = new Material(Shader.Find("Overtime/Glow")); clock.SetColor("_Tint", new Color(0.22f, 0.9f, 1f) * 2f);
            string[] names = { "Worker", "Worker", "Casual", "Casual" };
            for (int i = 0; i < names.Length; i++)
            {
                var h = new GameObject(names[i]).transform; h.SetParent(root.transform, false); h.localPosition = new Vector3((i - 1.5f) * 1.25f, 0, 0);
                h.localRotation = Quaternion.Euler(0, i % 2 == 0 ? 20 : 160, 0);
                var b = CharBody.BuildCourier(h, names[i], 0.88f, flow, clock, out _);
                b.LateFix();
            }
            var lg = new GameObject("L"); lg.transform.SetParent(root.transform, false); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.2f; lg.transform.rotation = Quaternion.Euler(30, 160, 0);
            var cg = new GameObject("Cam"); cg.transform.SetParent(root.transform, false); var cam = cg.AddComponent<Camera>();
            cam.transform.position = origin + new Vector3(0, 1.1f, 4.4f); cam.transform.LookAt(origin + new Vector3(0, 0.95f, 0)); cam.fieldOfView = 38; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.14f, 0.3f);
            var rt = new RenderTexture(1500, 700, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1500, 700, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1500, 700), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            Directory.CreateDirectory("Shots"); File.WriteAllBytes("Shots/" + file + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(root); rt.Release();
            return "Shots/" + file + ".png";
        }

        /// <summary>Render a straight-on portrait of the courier to a PNG (play mode).</summary>
        public static string FaceShot(string file, float dist, float height, float yawDeg)
        {
            var root = GameObject.Find("Runner");
            if (root == null) return "no runner (enter play mode first)";
            var cg = new GameObject("FaceCam"); var cam = cg.AddComponent<Camera>();
            var t = root.transform;
            var dir = Quaternion.Euler(0, yawDeg, 0) * Vector3.forward;
            cam.transform.position = t.position + dir * dist + Vector3.up * height;
            cam.transform.LookAt(t.position + Vector3.up * height * 0.98f);
            cam.fieldOfView = 34; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500; cam.clearFlags = CameraClearFlags.Skybox; cam.allowHDR = true;
            var data = cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderPostProcessing = true;
            var hide = new System.Collections.Generic.List<GameObject>();
            foreach (var n in new[] { "Trail", "Particles" }) { var g = GameObject.Find(n); if (g) { g.SetActive(false); hide.Add(g); } }
            var rt = new RenderTexture(900, 1200, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
            foreach (var g in hide) g.SetActive(true);
            RenderTexture.active = rt; var tex = new Texture2D(900, 1200, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            Directory.CreateDirectory("Shots"); File.WriteAllBytes("Shots/" + file + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(cg); rt.Release();
            return "Shots/" + file + ".png";
        }
    }
}
