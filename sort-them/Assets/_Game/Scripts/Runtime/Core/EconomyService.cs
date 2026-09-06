using System;
using UnityEngine;

namespace SortThem
{
    public class EconomyService
    {
        public EconomyConfig Config { get; }
        public float Balance { get; private set; }

        public event Action<float> Changed;

        public EconomyService(EconomyConfig config)
        {
            Config = config;
        }

        public void Add(float delta)
        {
            Balance += delta;
            if (Config != null && !Config.AllowNegativeBalance && Balance < 0f) Balance = 0f;
            Changed?.Invoke(Balance);
        }

        public void SetBalance(float value)
        {
            Balance = value;
            Changed?.Invoke(Balance);
        }

        public bool CanAfford(int cost) => Balance >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Add(-cost);
            return true;
        }
    }
}
