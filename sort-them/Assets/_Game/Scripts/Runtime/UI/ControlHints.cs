using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace SortThem
{
    public static class ControlHints
    {
        public const string KeyboardGroup = "KeyboardMouse";
        public const string GamepadGroup = "Gamepad";

        public static TMP_SpriteAsset Glyphs;

        static readonly (string Key, string Short)[] ShortForms =
        {
            ("leftshift", "Shift"), ("leftcontrol", "Ctrl"),
        };

        public static string Short(InputAction action, string group) => Resolve(action, group, true);

        public static string Label(InputAction action, string group) => Resolve(action, group, false);

        static string Resolve(InputAction action, string group, bool shortForm)
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
                if (group == GamepadGroup)
                {
                    string pad = PadLabel(controlPath, shortForm);
                    if (pad != null) return pad;
                }
                string glyph = Glyph(deviceLayout, controlPath);
                if (glyph != null) return glyph;
                if (!shortForm) return text;
                string key = Normalize(text);
                foreach (var (k, s) in ShortForms) if (key == k) return s;
                return text;
            }
            return "";
        }

        static string Normalize(string s) => s == null ? "" : s.Replace("/", "").Replace(" ", "").Replace("-", "").ToLowerInvariant();

        static bool DualShock => Gamepad.current is DualShockGamepad;

        static string PadLabel(string controlPath, bool shortForm)
        {
            if (string.IsNullOrEmpty(controlPath)) return null;
            string dpad = shortForm ? "" : Loc.Get("pad.dpad", "Крестовина") + " ";
            switch (Normalize(controlPath))
            {
                case "leftstick": return shortForm ? "LS" : Loc.Get("pad.leftstick", "Левый стик");
                case "rightstick": return shortForm ? "RS" : Loc.Get("pad.rightstick", "Правый стик");
                case "leftstickpress": return shortForm ? "LS" : Loc.Get("pad.leftstickpress", "Левый стик (нажать)");
                case "rightstickpress": return shortForm ? "RS" : Loc.Get("pad.rightstickpress", "Правый стик (нажать)");
                case "lefttrigger": return "LT";
                case "righttrigger": return "RT";
                case "leftshoulder": return "LB";
                case "rightshoulder": return "RB";
                case "buttonsouth": return DualShock ? Loc.Get("pad.cross", "Крест") : "A";
                case "buttoneast": return DualShock ? Loc.Get("pad.circle", "Круг") : "B";
                case "buttonwest": return DualShock ? Loc.Get("pad.square", "Квадрат") : "X";
                case "buttonnorth": return DualShock ? Loc.Get("pad.triangle", "Треугольник") : "Y";
                case "start": return DualShock ? "Options" : "Start";
                case "select": return DualShock ? "Share" : "Select";
                case "dpadup": return dpad + "\u2191";
                case "dpaddown": return dpad + "\u2193";
                case "dpadleft": return dpad + "\u2190";
                case "dpadright": return dpad + "\u2192";
                case "dpad": return shortForm ? "\u2190\u2191\u2193\u2192" : Loc.Get("pad.dpad", "Крестовина");
            }
            return null;
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
