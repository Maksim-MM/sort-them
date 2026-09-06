using NUnit.Framework;
using UnityEngine;

namespace SortThem.Tests
{
    public class EconomyTests
    {
        EconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.RewardPerCar = 1f;
            _config.AllowNegativeBalance = true;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Add_ChangesBalance_AndRaisesEvent()
        {
            var eco = new EconomyService(_config);
            float seen = -1f;
            eco.Changed += b => seen = b;
            eco.Add(2.5f);
            Assert.AreEqual(2.5f, eco.Balance);
            Assert.AreEqual(2.5f, seen);
        }

        [Test]
        public void Balance_CanGoNegative_WhenAllowed()
        {
            var eco = new EconomyService(_config);
            eco.Add(-3f);
            Assert.AreEqual(-3f, eco.Balance);
        }

        [Test]
        public void Balance_ClampsToZero_WhenNegativeDisallowed()
        {
            _config.AllowNegativeBalance = false;
            var eco = new EconomyService(_config);
            eco.Add(-3f);
            Assert.AreEqual(0f, eco.Balance);
        }

        [Test]
        public void TrySpend_FailsWithoutFunds()
        {
            var eco = new EconomyService(_config);
            eco.Add(10f);
            Assert.IsFalse(eco.TrySpend(11));
            Assert.IsTrue(eco.TrySpend(10));
            Assert.AreEqual(0f, eco.Balance);
        }
    }

    public class UpgradeTests
    {
        EconomyConfig _config;
        UpgradeData _range;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _range = ScriptableObject.CreateInstance<UpgradeData>();
            _range.Kind = UpgradeKind.Range;
            _range.CostPerLevel = new[] { 20, 30 };
            _range.ValuePerLevel = new[] { 1.25f, 1.5f };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_range);
        }

        [Test]
        public void Buy_LevelsUp_AndDeductsCost()
        {
            var eco = new EconomyService(_config);
            eco.Add(45f);
            var up = new UpgradeService(new[] { _range }, eco);
            Assert.AreEqual(1f, up.Value(UpgradeKind.Range, 1f));
            Assert.IsTrue(up.TryBuy(_range));
            Assert.AreEqual(1, up.Level(_range));
            Assert.AreEqual(25f, eco.Balance);
            Assert.AreEqual(1.25f, up.Value(UpgradeKind.Range, 1f));
            Assert.IsFalse(up.TryBuy(_range), "second level costs 30, only 25 left");
            eco.Add(5f);
            Assert.IsTrue(up.TryBuy(_range));
            Assert.IsTrue(up.IsMaxed(_range));
            Assert.IsFalse(up.CanBuy(_range));
            Assert.AreEqual(1.5f, up.Value(UpgradeKind.Range, 1f));
        }

        [Test]
        public void Value_UsesFallback_WhenNotOwned()
        {
            var up = new UpgradeService(new[] { _range }, new EconomyService(_config));
            Assert.AreEqual(7f, up.Value(UpgradeKind.Inventory, 7f));
            Assert.IsFalse(up.Has(UpgradeKind.Range));
        }
    }
}
