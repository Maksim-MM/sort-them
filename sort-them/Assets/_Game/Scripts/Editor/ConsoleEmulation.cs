using UnityEditor;

namespace SortThem.Editor
{
    public static class ConsoleEmulation
    {
        const string Menu = "SortThem/Console/Emulate Console Look (Play)";

        [MenuItem(Menu)]
        static void Toggle() => EditorPrefs.SetBool(Platform.EmulateConsolePref, !EditorPrefs.GetBool(Platform.EmulateConsolePref, false));

        [MenuItem(Menu, true)]
        static bool Validate()
        {
            UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(Platform.EmulateConsolePref, false));
            return true;
        }
    }
}
