using System.Runtime.InteropServices;
using UnityEngine;

namespace Overtime
{
    /// <summary>
    /// The phone answers every move: a light tick for lanes and Tocks, a firm knock for landings and dashes,
    /// a heavy thud for crashes, a double tap for wins. Native on iPhone (Taptic Engine); a no-op elsewhere.
    /// </summary>
    public static class Haptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void OTHaptic(int kind);
#else
        static void OTHaptic(int kind) { }
#endif
        static float lastLight;
        public static bool on = true;

        /// <summary>0 selection tick, 1 medium impact, 2 heavy impact, 3 success, 4 error.</summary>
        public static void Tap(int kind)
        {
            if (!on) return;
            // light ticks are rate-limited so a stream of Tocks buzzes rather than drones
            if (kind == 0) { if (Time.unscaledTime - lastLight < 0.045f) return; lastLight = Time.unscaledTime; }
            OTHaptic(kind);
        }
    }
}
