using System;
using UnityEngine;

namespace SortThem
{
    public enum DailyKind { Money, Spin, Discount, Mystery }

    public struct DailyReward
    {
        public DailyKind Kind;
        public int Amount;

        public DailyReward(DailyKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    public static class DailyRewards
    {
        const string KeyDay = "daily.day";
        const string KeyIndex = "daily.index";
        const string KeySpins = "daily.spins";
        const string KeyDiscount = "daily.discount";
        const float DiscountValue = 0.5f;

        public static readonly DailyReward[] Rewards =
        {
            new DailyReward(DailyKind.Money, 25),
            new DailyReward(DailyKind.Money, 50),
            new DailyReward(DailyKind.Spin, 1),
            new DailyReward(DailyKind.Money, 75),
            new DailyReward(DailyKind.Discount, 1),
            new DailyReward(DailyKind.Money, 100),
            new DailyReward(DailyKind.Mystery, 200),
        };

        public static int DayOffset;
        public static event Action Changed;

        public static bool Enabled => Platform.IsWeb && SavesReady.Prefs != null;
        public static int DiscountPercent => Mathf.RoundToInt(DiscountValue * 100f);

        static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays + DayOffset;

        public static int Index => Enabled ? Mathf.Abs(SavesReady.Prefs.GetInt(KeyIndex, 0)) % Rewards.Length : 0;
        public static bool CanClaim => Enabled && SavesReady.Prefs.GetInt(KeyDay, -1) != Today;
        public static int FreeSpins => Enabled ? SavesReady.Prefs.GetInt(KeySpins, 0) : 0;
        public static bool Discount => Enabled && SavesReady.Prefs.GetInt(KeyDiscount, 0) > 0;

        public static int DiscountPrice(int cost) => Mathf.Max(0, Mathf.CeilToInt(cost * (1f - DiscountValue)));

        public static bool Claim(GameManager gm, out DailyReward reward)
        {
            reward = default;
            if (gm == null || !CanClaim) return false;
            var prefs = SavesReady.Prefs;
            int index = Index;
            reward = Rewards[index];
            switch (reward.Kind)
            {
                case DailyKind.Money:
                case DailyKind.Mystery:
                    gm.Economy.Add(reward.Amount);
                    break;
                case DailyKind.Spin:
                    prefs.SetInt(KeySpins, prefs.GetInt(KeySpins, 0) + reward.Amount);
                    break;
                case DailyKind.Discount:
                    prefs.SetInt(KeyDiscount, 1);
                    break;
            }
            prefs.SetInt(KeyDay, Today);
            prefs.SetInt(KeyIndex, (index + 1) % Rewards.Length);
            prefs.TrySave();
            Changed?.Invoke();
            return true;
        }

        public static void UseSpin()
        {
            if (FreeSpins <= 0) return;
            SavesReady.Prefs.SetInt(KeySpins, FreeSpins - 1);
            SavesReady.Prefs.TrySave();
            Changed?.Invoke();
        }

        public static void UseDiscount()
        {
            if (!Discount) return;
            SavesReady.Prefs.SetInt(KeyDiscount, 0);
            SavesReady.Prefs.TrySave();
            Changed?.Invoke();
        }
    }
}
