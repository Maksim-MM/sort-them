using UnityEditor;

namespace SortThem.Editor
{
    [InitializeOnLoad]
    public static class BackgroundPlayTicker
    {
        const string PrefKey = "SortThem.BackgroundPlayTicker";
        const string MenuPath = "SortThem/Dev/Tick Play Mode In Background";

        static BackgroundPlayTicker()
        {
            EditorApplication.update += OnUpdate;
        }

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void OnUpdate()
        {
            if (!Enabled || !EditorApplication.isPlaying) return;
            if (UnityEditorInternal.InternalEditorUtility.isApplicationActive) return;
            EditorApplication.Step();
        }
    }
}
