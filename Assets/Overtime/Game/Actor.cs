using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Overtime
{
    /// <summary>A bystander character: idles, then cheers when the parcel arrives.</summary>
    public class Actor
    {
        PlayableGraph graph; AnimationMixerPlayable mixer; float w;
        public Transform t;

        readonly CharBody body;

        public Actor(string character, Transform parent, Vector3 pos, float yawDeg, float scale = 0.9f)
        {
            var holder = new GameObject("Customer").transform; holder.SetParent(parent, false);
            holder.SetPositionAndRotation(pos, Quaternion.Euler(0, yawDeg, 0));
            body = new CharBody(character, holder, scale, 1.2f, null, 1.7f, 0.9f);
            t = holder;
            body.model.localRotation = Quaternion.Euler(0, 180, 0);
            var anim = body.model.GetComponent<Animator>() ?? body.model.gameObject.AddComponent<Animator>();
            anim.applyRootMotion = false;
            graph = PlayableGraph.Create("Actor"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var art = ArtLibrary.I;
            graph.Connect(AnimationClipPlayable.Create(graph, art.Clip("Idle_Loop")), 0, mixer, 0);
            graph.Connect(AnimationClipPlayable.Create(graph, art.Clip("Dance_Loop")), 0, mixer, 1);
            AnimationPlayableOutput.Create(graph, "out", anim).SetSourcePlayable(mixer);
            graph.Play();
        }

        public void Tick(float dt, bool cheer)
        {
            if (!graph.IsValid()) return;
            w = Mathf.MoveTowards(w, cheer ? 1 : 0, dt * 6);
            mixer.SetInputWeight(0, 1 - w); mixer.SetInputWeight(1, w);
            graph.Evaluate(dt);
            body.LateFix();
        }

        public void Dispose() { if (graph.IsValid()) graph.Destroy(); }
    }
}
