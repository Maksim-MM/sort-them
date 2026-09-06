using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SortThem.Tests
{
    public class ShelfTests
    {
        readonly List<Object> _garbage = new List<Object>();
        CategoryData _catA, _catB;
        CarItemData _carA1, _carA2, _carB1;
        RackController _rack;
        ShelfController _shelf;

        [SetUp]
        public void SetUp()
        {
            _catA = Category("cat_a");
            _catB = Category("cat_b");
            _carA1 = Car("a1", _catA);
            _carA2 = Car("a2", _catA);
            _carB1 = Car("b1", _catB);

            var rackGo = new GameObject("Rack");
            _garbage.Add(rackGo);
            _rack = rackGo.AddComponent<RackController>();
            _rack.Category = _catA;

            var shelfGo = new GameObject("Shelf");
            shelfGo.transform.SetParent(rackGo.transform);
            _shelf = shelfGo.AddComponent<ShelfController>();
            _shelf.Rack = _rack;
            var points = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var p = new GameObject("Slot" + i).transform;
                p.SetParent(shelfGo.transform);
                p.localPosition = new Vector3(i, 0f, 0f);
                points[i] = p;
            }
            _shelf.SlotPoints = points;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _garbage) if (o != null) Object.DestroyImmediate(o);
            _garbage.Clear();
        }

        CategoryData Category(string id)
        {
            var c = ScriptableObject.CreateInstance<CategoryData>();
            c.CategoryID = id;
            _garbage.Add(c);
            return c;
        }

        CarItemData Car(string id, CategoryData cat)
        {
            var c = ScriptableObject.CreateInstance<CarItemData>();
            c.CarID = id;
            c.Category = cat;
            _garbage.Add(c);
            return c;
        }

        CarInstance Instance(CarItemData data)
        {
            var go = new GameObject("Car_" + data.CarID);
            _garbage.Add(go);
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            go.AddComponent<Rigidbody>();
            var ci = go.AddComponent<CarInstance>();
            ci.Data = data;
            return ci;
        }

        [Test]
        public void EmptyShelf_AcceptsAnyCategory()
        {
            Assert.IsTrue(_shelf.Accepts(_carA1));
            Assert.IsTrue(_shelf.Accepts(_carB1));
        }

        [Test]
        public void FirstCar_AssignsTarget_AndBlocksOtherModels()
        {
            var a1 = Instance(_carA1);
            Assert.IsTrue(_shelf.TryPlace(a1, 0, false));
            Assert.AreEqual(_carA1, _shelf.TargetCar);
            Assert.IsTrue(_shelf.Accepts(_carA1));
            Assert.IsFalse(_shelf.Accepts(_carA2), "same category, other model must be rejected");
            Assert.IsFalse(_shelf.Accepts(_carB1));
            Assert.IsFalse(_shelf.TryPlace(Instance(_carA2), 1, false));
        }

        [Test]
        public void Validity_IsShelfLevel_ByRackCategory()
        {
            _shelf.TryPlace(Instance(_carA1), 0, false);
            Assert.IsTrue(_shelf.IsValid);
            Assert.IsFalse(_shelf.IsClosed);

            var other = new GameObject("Shelf2");
            _garbage.Add(other);
            var shelf2 = other.AddComponent<ShelfController>();
            shelf2.Rack = _rack;
            shelf2.SlotPoints = _shelf.SlotPoints;
            shelf2.TryPlace(Instance(_carB1), 0, false);
            Assert.IsFalse(shelf2.IsValid, "cat_b car on cat_a rack is invalid");
        }

        [Test]
        public void Placed_Car_IsKinematic_OnStaticLayer_AtSlot()
        {
            var a1 = Instance(_carA1);
            _shelf.TryPlace(a1, 2, false);
            Assert.AreEqual(CarState.Placed, a1.State);
            Assert.IsTrue(a1.Body.isKinematic);
            Assert.AreEqual(2, a1.SlotIndex);
            Assert.AreEqual(_shelf.SlotPoints[2].position + Vector3.up * 0.1f, a1.transform.position);
        }

        [Test]
        public void OccupiedSlot_IsRejected_FirstFreeSlotFillsInOrder()
        {
            Assert.AreEqual(0, _shelf.FirstFreeSlot());
            _shelf.TryPlace(Instance(_carA1), 0, false);
            Assert.AreEqual(1, _shelf.FirstFreeSlot());
            Assert.IsFalse(_shelf.TryPlace(Instance(_carA1), 0, false));
            var middle = Instance(_carA1);
            _shelf.TryPlace(middle, 1, false);
            _shelf.TryPlace(Instance(_carA1), 2, false);
            _shelf.Remove(middle);
            Assert.AreEqual(1, _shelf.FirstFreeSlot(), "gap in the middle is filled first");
        }

        [Test]
        public void FullShelf_IsComplete_AndRejectsMore()
        {
            for (int i = 0; i < 3; i++) Assert.IsTrue(_shelf.TryPlace(Instance(_carA1), i, false));
            Assert.IsTrue(_shelf.IsComplete);
            Assert.IsTrue(_shelf.IsClosed);
            Assert.IsFalse(_shelf.Accepts(_carA1));
            Assert.AreEqual(-1, _shelf.FirstFreeSlot());
        }

        [Test]
        public void RemovingAll_ResetsToEmpty()
        {
            var a1 = Instance(_carA1);
            var a1b = Instance(_carA1);
            _shelf.TryPlace(a1, 0, false);
            _shelf.TryPlace(a1b, 1, false);
            _shelf.Remove(a1);
            Assert.AreEqual(1, _shelf.Count);
            Assert.AreEqual(_carA1, _shelf.TargetCar);
            Assert.IsNull(a1.Shelf);
            _shelf.Remove(a1b);
            Assert.IsTrue(_shelf.IsEmpty);
            Assert.IsTrue(_shelf.Accepts(_carB1), "reset shelf accepts anything again");
        }

        [Test]
        public void Remove_IgnoresCarFromOtherShelf()
        {
            var a1 = Instance(_carA1);
            _shelf.TryPlace(a1, 0, false);
            var stray = Instance(_carA1);
            _shelf.Remove(stray);
            Assert.AreEqual(1, _shelf.Count);
        }
    }
}
