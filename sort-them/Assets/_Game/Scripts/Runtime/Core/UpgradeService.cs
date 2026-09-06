using System;
using System.Collections.Generic;

namespace SortThem
{
    public class UpgradeService
    {
        public IReadOnlyList<UpgradeData> All => _all;

        readonly List<UpgradeData> _all;
        readonly Dictionary<UpgradeKind, int> _levels = new Dictionary<UpgradeKind, int>();
        readonly EconomyService _economy;

        public event Action<UpgradeData> Changed;

        public UpgradeService(IEnumerable<UpgradeData> upgrades, EconomyService economy)
        {
            _all = new List<UpgradeData>(upgrades);
            _economy = economy;
        }

        public UpgradeData Find(UpgradeKind kind)
        {
            foreach (var u in _all) if (u.Kind == kind) return u;
            return null;
        }

        public int Level(UpgradeKind kind) => _levels.TryGetValue(kind, out var l) ? l : 0;
        public int Level(UpgradeData data) => data != null ? Level(data.Kind) : 0;
        public bool Has(UpgradeKind kind) => Level(kind) > 0;

        public float Value(UpgradeKind kind, float fallback)
        {
            var data = Find(kind);
            return data != null ? data.ValueAt(Level(kind), fallback) : fallback;
        }

        public bool IsMaxed(UpgradeData data) => data != null && Level(data) >= data.MaxLevel;

        public int NextCost(UpgradeData data) => data != null ? data.CostOf(Level(data)) : 0;

        public bool CanBuy(UpgradeData data)
        {
            return data != null && !IsMaxed(data) && _economy != null && _economy.CanAfford(NextCost(data));
        }

        public bool TryBuy(UpgradeData data)
        {
            if (!CanBuy(data)) return false;
            if (!_economy.TrySpend(NextCost(data))) return false;
            _levels[data.Kind] = Level(data) + 1;
            Changed?.Invoke(data);
            return true;
        }

        public void SetLevel(UpgradeKind kind, int level)
        {
            var data = Find(kind);
            int max = data != null ? data.MaxLevel : level;
            _levels[kind] = Math.Max(0, Math.Min(level, max));
            Changed?.Invoke(data);
        }

        public IEnumerable<KeyValuePair<UpgradeKind, int>> Levels => _levels;
    }
}
