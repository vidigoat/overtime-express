using System.Collections.Generic;
using Overtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Overtime
{
    /// <summary>
    /// The courier: a skinned, animated character. Clips are blended through a Playables mixer driven by the sim state,
    /// with one-shot overlays (landing, lane dodge, crash) and procedural body orientation for wall-runs, slides and flips.
    /// </summary>
    public class RunnerView
    {
        public Transform root, model, chest;
        public Material packGlow, bodyGlow;      // kept so the rest of the view can tint the parcel light
        public float flip, squash, extraZ, faceYaw;
        float cheerT;
        public string character = "Courier";

        PlayableGraph graph; AnimationMixerPlayable mixer;
        readonly List<AnimationClipPlayable> plays = new List<AnimationClipPlayable>();
        readonly Dictionary<string, int> idx = new Dictionary<string, int>();
        float[] weight, target;
        string oneShot; float oneShotT, oneShotLen, oneShotW;
        float prevX, rollV, wallBlend, pitch, yaw, hipY;
        int wallSide, lastLane;
        Transform parcel, pack;

        static readonly string[] Clips = { "Sprint_Loop", "Jump_Loop", "Jump_Start", "Jump_Land", "Slide_Loop", "NinjaJump_Idle_Loop", "Hit_Chest", "Dance_Loop", "Idle_Loop", "Driving_Loop", "Crouch_Idle_Loop", "Roll", "Interact" };

        public RunnerView(Transform parent)
        {
            root = new GameObject("Runner").transform; root.SetParent(parent, false);
            bodyGlow = new Material(Shader.Find("Overtime/Glow")); packGlow = new Material(Shader.Find("Overtime/Glow"));
            Build();
        }

        CharBody body;

        public void Build()
        {
            if (model) Object.Destroy(model.gameObject);
            if (graph.IsValid()) graph.Destroy();
            plays.Clear(); idx.Clear();
            var art = ArtLibrary.I;
            // Kip: a slightly short, big-headed delivery rider in company colours
            body = CharBody.BuildCourier(root, Variant, Scale, bodyGlow, packGlow, out pack);
            model = body.model; chest = body.chest;
            var anim = model.GetComponent<Animator>() ?? model.gameObject.AddComponent<Animator>();
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            graph = PlayableGraph.Create("Runner"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, Clips.Length);
            for (int i = 0; i < Clips.Length; i++)
            {
                var p = AnimationClipPlayable.Create(graph, art.Clip(Clips[i]));
                p.SetApplyFootIK(false);
                graph.Connect(p, 0, mixer, i); mixer.SetInputWeight(i, 0);
                plays.Add(p); idx[Clips[i]] = i;
            }
            weight = new float[Clips.Length]; target = new float[Clips.Length];
            weight[idx["Sprint_Loop"]] = 1;
            var output = AnimationPlayableOutput.Create(graph, "out", anim); output.SetSourcePlayable(mixer);
            graph.Play();

        }

        public static string Variant = "Worker";
        const float Scale = 0.88f;
        // the designed characters animate facing backwards relative to their bind pose, so turn them round
        static readonly Quaternion FaceForward = Quaternion.Euler(0, 180, 0);

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = Find(c, name); if (r) return r; }
            return null;
        }

        /// <summary>The job's item travels inside the sealed delivery pack, so there is nothing extra to show on his back.</summary>
        public void SetParcel(string modelName, float scale) { }

        /// <summary>Play a clip once on top of the current pose (landing, dodge, crash, cheer).</summary>
        public void OneShot(string clip, float w = 1f, float speed = 1f)
        {
            if (!idx.TryGetValue(clip, out int i)) return;
            oneShot = clip; oneShotT = 0; oneShotW = w;
            oneShotLen = (float)plays[i].GetAnimationClip().length / speed;
            plays[i].SetTime(0); plays[i].SetSpeed(speed);
        }

        void Loop(string clip, float speed) { var p = plays[idx[clip]]; p.SetSpeed(speed); }
        void Hold(string clip, float t) { var p = plays[idx[clip]]; p.SetSpeed(0); p.SetTime(t); }

        public void Pose(Sim sim, float dt, bool dashing, bool cheering = false, bool riding = false)
        {
            for (int i = 0; i < target.Length; i++) target[i] = 0;
            float tPitch = 0, tYaw = 0, tHip = 0;
            float runRate = Mathf.Clamp(sim.speed / 14.5f, 0.6f, 1.6f) * 1.15f;
            if (!cheering) cheerT = 0;
            if (cheering)
            {
                // hand the parcel over first, then celebrate
                cheerT += dt;
                if (cheerT < 1.25f) { target[idx["Interact"]] = 1; Loop("Interact", 1); if (cheerT - dt <= 0) plays[idx["Interact"]].SetTime(0); }
                else { target[idx["Dance_Loop"]] = 1; Loop("Dance_Loop", 1); }
            }
            else if (riding) { target[idx["Driving_Loop"]] = 1; Loop("Driving_Loop", 1); tHip = 0.58f; tPitch = 8; }
            else switch (sim.state)
            {
                case RunState.Slide: target[idx["Slide_Loop"]] = 1; Loop("Slide_Loop", 1); break;
                case RunState.Grind: target[idx["Crouch_Idle_Loop"]] = 1; Loop("Crouch_Idle_Loop", 1); tYaw = 68; break;
                case RunState.Air:
                    if (dashing) { target[idx["NinjaJump_Idle_Loop"]] = 1; Loop("NinjaJump_Idle_Loop", 1.4f); tPitch = 18; }
                    else { target[idx["Jump_Loop"]] = 1; Loop("Jump_Loop", 1); tPitch = Mathf.Clamp(-sim.vy * 0.9f, -12, 14); }
                    break;
                default: target[idx["Sprint_Loop"]] = 1; Loop("Sprint_Loop", runRate); tPitch = 4; break;
            }
            lastLane = sim.lane;

            float ow = 0;
            if (oneShot != null)
            {
                oneShotT += dt;
                float k = oneShotT / Mathf.Max(0.01f, oneShotLen);
                if (k >= 1) oneShot = null;
                else { ow = oneShotW * Mathf.Clamp01(Mathf.Min(k * 8, (1 - k) * 4)); }
            }
            float blend = Mathf.Min(1, dt * 14), sum = 0;
            for (int i = 0; i < weight.Length; i++)
            {
                float t = target[i] * (1 - ow);
                if (oneShot != null && idx[oneShot] == i) t = Mathf.Max(t, ow);
                weight[i] = Mathf.Lerp(weight[i], t, blend); sum += weight[i];
            }
            for (int i = 0; i < weight.Length; i++) mixer.SetInputWeight(i, sum > 0 ? weight[i] / sum : 0);
            graph.Evaluate(dt);
            body.LateFix();

            // wall-run: plant the feet on the wall and lay the body out sideways from it
            bool onWall = sim.state == RunState.Wall;
            if (onWall) wallSide = sim.wall.side;
            wallBlend = Mathf.MoveTowards(wallBlend, onWall ? 1 : 0, dt / 0.14f);
            float wb = wallBlend * wallBlend * (3 - 2 * wallBlend);

            float vx = (sim.x - prevX) / Mathf.Max(dt, 1e-4f); prevX = sim.x;
            rollV = Mathf.Lerp(rollV, Mathf.Clamp(-vx * 2.2f, -24, 24) * (1 - wb), Mathf.Min(1, dt * 14));
            if (flip > 0) flip = Mathf.Max(0, flip - dt / 0.55f);
            float flipA = flip > 0 ? (1 - flip) * 360f : 0;
            squash = Mathf.Lerp(squash, 0, Mathf.Min(1, dt * 9));
            float k2 = Mathf.Min(1, dt * 12);
            pitch = Mathf.Lerp(pitch, tPitch, k2); yaw = Mathf.Lerp(yaw, tYaw, k2); hipY = Mathf.Lerp(hipY, tHip, k2);

            root.position = new Vector3(sim.x + wallSide * wb * 0.74f, sim.y + wb * 0.1f, sim.s + extraZ);
            root.rotation = Quaternion.AngleAxis(faceYaw, Vector3.up) * Quaternion.AngleAxis(wallSide * wb * 62f, Vector3.forward);
            // pivot flips and leans around the body's centre, not the feet
            var pivot = new Vector3(0, 0.85f, 0);
            var rot = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch + flipA, Vector3.right) * Quaternion.AngleAxis(rollV, Vector3.forward);
            model.localRotation = rot * FaceForward;
            model.localPosition = pivot - rot * pivot + new Vector3(0, hipY - body.GroundDrop, 0);
            float sc = Scale;
            model.localScale = new Vector3(sc * (1 + squash * 0.2f), sc * (1 - squash * 0.26f), sc * (1 + squash * 0.2f));
        }

        public void SetVisible(bool v) { if (root.gameObject.activeSelf != v) root.gameObject.SetActive(v); }
        public void Dispose() { if (graph.IsValid()) graph.Destroy(); }
    }
}
