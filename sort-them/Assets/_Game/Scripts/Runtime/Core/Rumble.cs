using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    public static class Rumble
    {
        static float _until;
        static bool _active;

        public static void ShelfComplete() => Pulse(0.35f, 0.6f, 0.25f);
        public static void UiMove() => Pulse(0f, 0.18f, 0.04f);
        public static void UiClick() => Pulse(0.1f, 0.35f, 0.07f);
        public static void AbilityReady() => Pulse(0.2f, 0.5f, 0.15f);

        public static void Pulse(float low, float high, float duration)
        {
            if (!Settings.Vibration) return;
            var pad = Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(low, high);
            _until = Time.unscaledTime + duration;
            _active = true;
        }

        public static void Tick()
        {
            if (!_active || Time.unscaledTime < _until) return;
            Stop();
        }

        public static void Stop()
        {
            _active = false;
            var pad = Gamepad.current;
            if (pad != null) pad.SetMotorSpeeds(0f, 0f);
        }
    }
}
