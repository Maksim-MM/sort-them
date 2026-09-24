using UnityEngine;

namespace SortThem
{
    public static class ControlHints
    {
        public static HintDevice Device => GameInput.Device;

        public static Sprite Glyph(GameAction action, int index = 0) => action?.Glyph(Device, index);

        public static string Fallback(GameAction action) => action != null ? action.Fallback(Device) : "";
    }
}
