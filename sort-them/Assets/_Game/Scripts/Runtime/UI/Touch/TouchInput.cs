using UnityEngine;

namespace SortThem
{
    public enum TouchButton { Jump, Interact, Place, Crouch, Pause, Next, Prev, Ability1, Ability2, Ability3 }

    public static class TouchInput
    {
        public static bool Active;
        public static Vector2 Move;
        public static bool SprintToggled;

        static Vector2 _look;
        static readonly int[] _pressedFrame = new int[10];

        public static void AddLook(Vector2 delta) => _look += delta;

        public static Vector2 ConsumeLook()
        {
            var l = _look;
            _look = Vector2.zero;
            return l;
        }

        public static void Press(TouchButton button) => _pressedFrame[(int)button] = Time.frameCount;

        public static bool Consume(TouchButton button)
        {
            int i = (int)button;
            if (_pressedFrame[i] == 0 || Time.frameCount - _pressedFrame[i] > 1) return false;
            _pressedFrame[i] = 0;
            return true;
        }

        public static void Reset()
        {
            Move = Vector2.zero;
            _look = Vector2.zero;
            SprintToggled = false;
            for (int i = 0; i < _pressedFrame.Length; i++) _pressedFrame[i] = 0;
        }
    }
}
