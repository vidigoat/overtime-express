using System;
using Overtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overtime
{
    /// <summary>
    /// One-thumb controls. A swipe fires the moment the finger has travelled far enough, and one swipe is exactly
    /// one move: a drag that keeps going can chain into the other axis (right, then up) but never repeats itself.
    /// A tap is a jump; a finger held still is slow-mo. Keyboard mirrors it for desktop.
    /// </summary>
    public class SwipeInput
    {
        public Action<Swipe> onSwipe = _ => { };
        public bool slow;
        bool down, fired, lastHoriz, touchSlow;
        Vector2 start; float downT;

        public void Update()
        {
            var kb = Keyboard.current;
            bool keySlow = false;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) onSwipe(Swipe.Left);
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) onSwipe(Swipe.Right);
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) onSwipe(Swipe.Up);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) onSwipe(Swipe.Down);
                keySlow = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }
            var ptr = Pointer.current;
            if (ptr != null)
            {
                Vector2 pos = ptr.position.ReadValue();
                // thresholds in physical size so the gesture feels the same on every phone
                // sized from the screen, not reported DPI: browsers on phones report DPI unreliably
                float th = Mathf.Max(14f, Mathf.Min(Screen.width, Screen.height) * 0.045f);
                if (ptr.press.wasPressedThisFrame) { down = true; fired = false; touchSlow = false; start = pos; downT = Time.unscaledTime; }
                else if (down && ptr.press.isPressed)
                {
                    Vector2 d = pos - start; bool horiz = Mathf.Abs(d.x) > Mathf.Abs(d.y);
                    bool go = false;
                    if (fired)
                    {
                        if (horiz == lastHoriz) { if (horiz) start.x = pos.x; else start.y = pos.y; }
                        else if (Mathf.Abs(horiz ? d.x : d.y) >= th * 2) go = true;
                    }
                    else if (Mathf.Abs(d.x) >= th || Mathf.Abs(d.y) >= th) go = true;
                    else if (Time.unscaledTime - downT > 0.17f) touchSlow = true;
                    if (go)
                    {
                        start = pos; fired = true; lastHoriz = horiz; touchSlow = false;
                        onSwipe(horiz ? (d.x > 0 ? Swipe.Right : Swipe.Left) : d.y > 0 ? Swipe.Up : Swipe.Down);
                    }
                }
                if (down && ptr.press.wasReleasedThisFrame)
                {
                    if (!fired && !touchSlow)
                    {
                        // a short quick flick that never crossed the threshold still counts; a still tap is a jump (or a dash in the air)
                        Vector2 d = pos - start;
                        if (d.magnitude >= th * 0.45f) onSwipe(Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? Swipe.Right : Swipe.Left) : d.y > 0 ? Swipe.Up : Swipe.Down);
                        else onSwipe(Swipe.Up);
                    }
                    down = false; touchSlow = false;
                }
            }
            slow = keySlow || touchSlow;
        }
    }
}
