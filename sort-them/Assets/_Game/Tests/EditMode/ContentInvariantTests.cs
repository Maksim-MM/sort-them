using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SortThem.Tests
{
    public class ContentInvariantTests
    {
        const string LayoutPath = "Assets/_Game/Data/LevelLayout.asset";
        const string ScenePath = "Assets/_Game/Scenes/Main.unity";

        LevelLayoutData _layout;
        CarCatalog _catalog;
        ShelfController[] _shelves;
        bool _openedScene;

        [SetUp]
        public void SetUp()
        {
            _layout = AssetDatabase.LoadAssetAtPath<LevelLayoutData>(LayoutPath);
            Assert.IsNotNull(_layout, "LevelLayout.asset не найден");
            _catalog = _layout.Catalog;
            Assert.IsNotNull(_catalog, "У раскладки нет каталога");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                _openedScene = true;
            }
            _shelves = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ShelfController>(true))
                .ToArray();
            Assert.IsNotEmpty(_shelves, "В сцене Main нет полок");
        }

        [TearDown]
        public void TearDown()
        {
            if (_openedScene)
                EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(ScenePath), true);
        }

        [Test]
        public void EveryInstanceReferencesCatalogCar()
        {
            foreach (var e in _layout.Instances)
                Assert.That(e.CarIndex, Is.InRange(0, _catalog.Cars.Length - 1));
        }

        [Test]
        public void EveryCarCountEqualsShelfCapacity()
        {
            var counts = new int[_catalog.Cars.Length];
            foreach (var e in _layout.Instances) counts[e.CarIndex]++;

            var failures = new List<string>();
            for (int i = 0; i < _catalog.Cars.Length; i++)
            {
                var car = _catalog.Cars[i];
                var shelf = _shelves.FirstOrDefault(s => s.Rack != null && s.Rack.Category == car.Category);
                if (shelf == null) { failures.Add($"{car.DevName}: нет стеллажа категории {car.Category?.CategoryID}"); continue; }
                if (counts[i] != shelf.Capacity) failures.Add($"{car.DevName}: {counts[i]} на уровне, полка на {shelf.Capacity}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void EveryCategoryHasOneShelfPerModel()
        {
            var failures = new List<string>();
            foreach (var cat in _catalog.Categories)
            {
                int models = _catalog.Cars.Count(c => c.Category == cat);
                int shelves = _shelves.Count(s => s.Rack != null && s.Rack.Category == cat);
                if (models != shelves) failures.Add($"{cat.CategoryID}: моделей {models}, полок {shelves}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void TotalInstancesEqualTotalSlots()
        {
            int slots = _shelves.Sum(s => s.Capacity);
            Assert.AreEqual(slots, _layout.Instances.Length, "Машинок на уровне не столько, сколько слотов");
        }
    }
}
