using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class SetupAll
    {
        [MenuItem("SortThem/Setup All (1-4)")]
        public static void Run()
        {
            LocalizationSetup.Setup();
            InputSetup.Create();
            CarModelGenerator.Generate();
            LevelBuilder.Build();
            Debug.Log("SortThem: setup complete, now bake scatter (5)");
        }
    }
}
