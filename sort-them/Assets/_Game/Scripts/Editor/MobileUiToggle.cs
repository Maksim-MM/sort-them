using UnityEditor;

namespace SortThem.Editor
{
    public static class MobileUiToggle
    {
        const string Menu = "SortThem/Dev/Emulate Mobile UI";

        [MenuItem(Menu)]
        static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(Platform.EmulateMobilePref, false);
            EditorPrefs.SetBool(Platform.EmulateMobilePref, on);
            UnityEngine.Debug.Log("SortThem: emulate mobile UI " + (on ? "ON" : "OFF") + " (takes effect on next Play)");
        }

        [MenuItem(Menu, true)]
        static bool Validate()
        {
            UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(Platform.EmulateMobilePref, false));
            return true;
        }
    }
}
