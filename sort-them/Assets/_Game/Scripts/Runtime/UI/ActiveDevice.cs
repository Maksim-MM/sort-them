using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    public static class ActiveDevice
    {
        public static bool Gamepad { get; private set; }

        const float PadThreshold = 0.25f;
        static Vector2 _lastStickL, _lastStickR;

        public static void Init() { }

        public static void Poll()
        {
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && PadActuated(pad)) { Gamepad = true; return; }
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.isPressed) { Gamepad = false; return; }
            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed || Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)) Gamepad = false;
        }

        static bool PadActuated(UnityEngine.InputSystem.Gamepad pad)
        {
            var l = pad.leftStick.ReadValue();
            var r = pad.rightStick.ReadValue();
            bool moved = (l - _lastStickL).sqrMagnitude > 0.01f && l.magnitude > PadThreshold || (r - _lastStickR).sqrMagnitude > 0.01f && r.magnitude > PadThreshold;
            _lastStickL = l;
            _lastStickR = r;
            if (moved) return true;
            if (pad.leftTrigger.ReadValue() > PadThreshold || pad.rightTrigger.ReadValue() > PadThreshold) return true;
            foreach (var b in new[] { pad.buttonSouth, pad.buttonEast, pad.buttonWest, pad.buttonNorth, pad.leftShoulder, pad.rightShoulder, pad.startButton, pad.selectButton, pad.leftStickButton, pad.rightStickButton, pad.dpad.up, pad.dpad.down, pad.dpad.left, pad.dpad.right })
                if (b.isPressed) return true;
            return false;
        }
    }
}
