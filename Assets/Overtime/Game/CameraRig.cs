using Overtime.Core;
using UnityEngine;

namespace Overtime
{
    /// <summary>Chase camera: sprung follow, landing dip, lean into lane changes, roll with wall-runs, speed FOV.</summary>
    public class CameraRig
    {
        public Camera cam;
        public float shake, fovKick, dip;
        /// <summary>Menu screens: a low three-quarter shot that frames the courier between the header and the route map.</summary>
        public bool hero; float heroK;
        float camY, camX, roll;

        public CameraRig(Camera c) { cam = c; }

        /// <summary>Ease the camera toward a staged shot (hand-off, ride-off). k is 0..1 blend.</summary>
        public void Stage(Vector3 pos, Vector3 look, float k, float fov)
        {
            cam.transform.position = Vector3.Lerp(cam.transform.position, pos, k);
            var q = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, q, k);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, k);
        }

        public void Reset(Sim sim) { camY = sim.y + 2.95f; camX = sim.x; shake = fovKick = dip = roll = 0; }

        public void Update(Sim sim, float dt, float rdt, float over, float time)
        {
            bool portrait = cam.aspect < 1;
            float ty = sim.y + 2.95f;
            camY += (ty - camY) * Mathf.Min(1, dt * (ty < camY ? 5.5f : 3.4f));
            camX += (sim.x * 0.62f - camX) * Mathf.Min(1, dt * 9);
            shake = Mathf.Max(0, shake - rdt * 2.2f);
            dip += (0 - dip) * Mathf.Min(1, rdt * 9);
            fovKick += (0 - fovKick) * Mathf.Min(1, rdt * 4);
            float sh = shake * shake, back = portrait ? 5.3f : 5.2f;
            var pos = new Vector3(camX + (Random.value - 0.5f) * sh, camY - dip + (Random.value - 0.5f) * sh + (sim.state == RunState.Slide ? -0.55f : 0), sim.s - back);
            var look = new Vector3(camX * 1.25f, camY - 1.8f - dip * 0.5f, sim.s + 10);
            heroK = Mathf.MoveTowards(heroK, hero ? 1 : 0, rdt * 2.5f);
            if (heroK > 0)
            {
                float e = Mathf.SmoothStep(0, 1, heroK);
                pos = Vector3.Lerp(pos, new Vector3(camX + 0.95f, camY - 1.25f, sim.s - 5.2f), e);
                look = Vector3.Lerp(look, new Vector3(camX + 0.15f, camY - 3.2f, sim.s + 2f), e);
            }
            int side = sim.wall != null ? sim.wall.side : 0;
            float targetRoll = (sim.x * 0.62f - camX) * -2f + (sim.state == RunState.Wall ? side * 9f : 0) + (sim.overtime ? Mathf.Sin(time * 31) * 0.3f : 0);
            roll += (targetRoll - roll) * Mathf.Min(1, rdt * 8);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.forward);
            float fov = Mathf.Lerp(portrait ? 72 : 58, 60, Mathf.SmoothStep(0, 1, heroK)) + (sim.speed - 14.5f) * 1.6f + fovKick + over * 5 - sim.slowAmt * 9;
            cam.fieldOfView += (fov - cam.fieldOfView) * Mathf.Min(1, rdt * 8);
        }
    }
}
