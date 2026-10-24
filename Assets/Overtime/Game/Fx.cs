using Overtime.Core;
using UnityEngine;

namespace Overtime
{
    /// <summary>Particles and the energy ribbon that streams from the delivery pack.</summary>
    public class Fx
    {
        readonly ParticleSystem ps;
        readonly TrailRenderer trail;
        readonly Transform trailT;

        public Fx(Transform parent)
        {
            var g = new GameObject("Particles"); g.transform.SetParent(parent, false);
            ps = g.AddComponent<ParticleSystem>();
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600; main.playOnAwake = false; main.loop = false;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.3f));
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Mats.Fx(0, Color.white); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var tg = new GameObject("Trail"); trailT = tg.transform; trailT.SetParent(parent, false);
            trail = tg.AddComponent<TrailRenderer>();
            trail.time = 0.16f; trail.minVertexDistance = 0.15f; trail.numCapVertices = 2; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0.15f);
            trail.sharedMaterial = Mats.Fx(1, Color.white);
            trail.alignment = LineAlignment.View; trail.textureMode = LineTextureMode.Stretch;
            // the ribbon read as a hazy wedge behind the courier at phone size; his lit soles and sparks carry the motion instead
            trail.enabled = false;
            var tgGrad = new Gradient(); tgGrad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = tgGrad;
        }

        public void Clear(Vector3 p) { ps.Clear(); trailT.position = p; trail.Clear(); }

        public void Burst(Vector3 p, int n, Color c, float speed = 5, float up = 2, float fwd = 0, float size = 0.3f, float life = 0.6f, float grav = 14)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2, e = Random.value;
                ep.position = p + Random.insideUnitSphere * 0.05f;
                ep.velocity = new Vector3(Mathf.Cos(a) * speed * e, Random.value * speed * 0.8f + up, Mathf.Sin(a) * speed * e + fwd);
                ep.startColor = c; ep.startSize = size * (0.6f + Random.value * 0.8f) * 0.5f; ep.startLifetime = life * (0.5f + Random.value * 0.7f);
                ps.Emit(ep, 1);
            }
            gravity = grav;
        }
        float gravity = 14;

        public void Tick(Sim sim, Vector3 packPos, Color flow, float dt)
        {
            trailT.position = packPos;
            trail.startWidth = trail.endWidth = 1;
            trail.widthMultiplier = 0.07f + Mathf.Min(0.12f, sim.chain * 0.008f);
            var c = (sim.overtime ? Mats.Hex(0xff2040) : flow) * (0.3f + Mathf.Min(0.6f, sim.chain * 0.04f)) * Mathf.Lerp(1f, 0.28f, Shader.GetGlobalFloat("_OTDay"));
            trail.sharedMaterial.SetColor("_Tint", c);
            // warm roof dust while running; overtime stays a clean red
            if (sim.Grounded && !sim.overtime && Random.value < 0.25f)
                Burst(new Vector3(packPos.x, packPos.y - 0.9f, packPos.z), 1, new Color(0.75f, 0.62f, 0.42f), 1.4f, 0.6f, -sim.speed * 0.15f, 0.16f, 0.28f, 3f);
            var fo = ps.forceOverLifetime; fo.enabled = true; fo.y = -gravity;
        }
    }
}
