using TMPro;
using UnityEngine.InputSystem;

namespace SortThem
{
    public static class ControlHints
    {
        public const string KeyboardGroup = "KeyboardMouse";
        public const string GamepadGroup = "Gamepad";

        public static TMP_SpriteAsset Glyphs;

        static readonly (string Key, string Short)[] ShortForms =
        {
            ("dpadup", "\u2191"), ("dpaddown", "\u2193"), ("dpadleft", "\u2190"), ("dpadright", "\u2192"),
            ("leftshift", "Shift"), ("leftcontrol", "Ctrl"), ("leftstickpress", "LS"), ("rightstickpress", "RS"),
        };

        public static string Short(InputAction action, string group)
        {
            string label = Label(action, group);
            if (label.StartsWith("<sprite")) return label;
            string key = label.Replace("/", "").Replace(" ", "").Replace("-", "").ToLowerInvariant();
            foreach (var (k, s) in ShortForms) if (key == k) return s;
            return label;
        }

        public static string Label(InputAction action, string group)
        {
            if (action == null) return "";
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite) continue;
                string groups = b.isComposite && i + 1 < bindings.Count ? bindings[i + 1].groups : b.groups;
                if (!InGroup(groups, group)) continue;
                string text = action.GetBindingDisplayString(i, out var deviceLayout, out var controlPath, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                string glyph = Glyph(deviceLayout, controlPath);
                return glyph ?? text;
            }
            return "";
        }

        static string Glyph(string deviceLayout, string controlPath)
        {
            if (Glyphs == null || string.IsNullOrEmpty(controlPath)) return null;
            string name = (deviceLayout + "_" + controlPath).Replace('/', '_').ToLowerInvariant();
            return Glyphs.GetSpriteIndexFromName(name) >= 0 ? "<sprite name=\"" + name + "\">" : null;
        }

        static bool InGroup(string groups, string group)
        {
            if (string.IsNullOrEmpty(groups)) return false;
            foreach (var g in groups.Split(';')) if (g.Trim() == group) return true;
            return false;
        }
    }
}
